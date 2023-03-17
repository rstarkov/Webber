using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using RT.Util.ExtensionMethods;

namespace Webber.Server;

interface IDbService
{
    bool Enabled { get; }
    void Initialise();
    void RegisterMigration(string serviceName, int fromVer, int toVer, Action<SqliteConnection, SqliteTransaction> migration);
    SqliteConnection OpenConnection();
}

class DisabledDbService : IDbService
{
    public bool Enabled => false;
    void IDbService.Initialise() { }
    SqliteConnection IDbService.OpenConnection() => throw new NotSupportedException();
    void IDbService.RegisterMigration(string serviceName, int fromVer, int toVer, Action<SqliteConnection, SqliteTransaction> migration) => throw new NotSupportedException();
}

class DbService : IDbService
{
    private string _dbFilePath;
    private List<MigrationInfo> _migrations = new();
    private bool _initialised = false;

    public DbService(AppConfig config)
    {
        _dbFilePath = config.DbFilePath;
    }

    public bool Enabled => true;

    public SqliteConnection OpenConnection()
    {
        if (!_initialised)
            throw new InvalidOperationException("The database has not been initialised yet.");
        var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbFilePath }.ConnectionString);
        conn.Open();
        return conn;
    }

    public void RegisterMigration(string serviceName, int fromVer, int toVer, Action<SqliteConnection, SqliteTransaction> migrate)
    {
        if (_initialised)
            throw new InvalidOperationException("RegisterMigration cannot be called anymore as the migrations have already been executed.");
        _migrations.Add(new MigrationInfo { ServiceName = serviceName, FromVer = fromVer, ToVer = toVer, Migrate = migrate });
    }

    private class MigrationInfo
    {
        public string ServiceName { get; set; }
        public int FromVer { get; set; }
        public int ToVer { get; set; }
        public Action<SqliteConnection, SqliteTransaction> Migrate { get; set; }
    }

    public void Initialise()
    {
        if (_initialised)
            throw new Exception("DB service has already been initialised.");

        SqlMapperExtensions.TableNameMapper = type => type.Name;

        var connString = new SqliteConnectionStringBuilder { DataSource = _dbFilePath }.ConnectionString;

        if (!File.Exists(_dbFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_dbFilePath)));
            using var conn = new SqliteConnection(connString);
            conn.Open();
            conn.Execute($@"CREATE TABLE {nameof(TbSchema)} (
                    {nameof(TbSchema.ServiceName)} TEXT NOT NULL PRIMARY KEY,
                    {nameof(TbSchema.SchemaVersion)} INT NOT NULL
                )");
        }

        foreach (var serviceMigrations in _migrations.GroupBy(m => m.ServiceName))
        {
            var serviceName = serviceMigrations.Key;
            var maxVersion = serviceMigrations.Max(m => m.ToVer);

            int curVersion;
            using (var conn = new SqliteConnection(connString))
            {
                conn.Open();
                curVersion = conn.Get<TbSchema>(serviceName)?.SchemaVersion ?? 0;
            }

            while (curVersion < maxVersion)
            {
                // this isn't supposed to support any complex scenarios; it will greedy-pick the largest applicable migration
                var migration = serviceMigrations.Where(m => m.FromVer == curVersion).MaxElementOrDefault(m => m.ToVer);
                if (migration == null)
                    throw new Exception($"Unable to find a migration from version {curVersion} while migrating \"{serviceName}\" to the latest version {maxVersion}.");

                using var conn = new SqliteConnection(connString);
                conn.Open();
                using var trn = conn.BeginTransaction();

                // todo: handle exceptions one way or another? Currently it just results in the migration being aborted and the server failing to start
                migration.Migrate(conn, trn);
                var row = new TbSchema { ServiceName = serviceName, SchemaVersion = migration.ToVer };
                if (curVersion == 0)
                    conn.Insert(row, transaction: trn);
                else
                    conn.Update(row, transaction: trn);
                trn.Commit();

                curVersion = migration.ToVer;
            }
        }

        _initialised = true;
    }

    class TbSchema
    {
        [ExplicitKey]
        public string ServiceName { get; set; }
        public int SchemaVersion { get; set; }
    }
}

static class DatabaseExtensions
{
    private static DateTime _unixepoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToDbDateTime(this DateTime dt) => (long)(dt.ToUniversalTime() - _unixepoch).TotalMilliseconds;

    public static DateTime FromDbDateTime(this long dt) => _unixepoch.AddMilliseconds(dt);
}
