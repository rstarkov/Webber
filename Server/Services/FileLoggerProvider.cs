using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using RT.Util.ExtensionMethods;

namespace Webber.Server.Services;

public static class FileLoggerExtensions
{
    public static void AddFile(this ILoggingBuilder builder)
    {
        // as documented in https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider ... NOT
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);
    }
}

public class FileLoggerOptions
{
    public string FileName { get; set; } = null;
}

[ProviderAlias("File")] // as documented in https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider ... NOT
public class FileLoggerProvider : ILoggerProvider
{
    private string _fileName;

    public FileLoggerProvider(FileLoggerOptions options)
    {
        _fileName = options.FileName;
    }

    public FileLoggerProvider(IOptions<FileLoggerOptions> options)
        : this(options.Value)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _fileName);
    }

    public void Dispose()
    {
    }

    private class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _fileName;

        public LogLevel Level { get; set; } = LogLevel.None;

        public FileLogger(string categoryName, string fileName)
        {
            var pos = categoryName.LastIndexOf('.');
            _categoryName = pos < 0 ? categoryName : categoryName.Substring(pos + 1);
            _fileName = fileName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _fileName != null && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            try
            {
                var filename = _fileName.Replace("{date}", $"{DateTime.Now:yyyy-MM-dd}");
                var lines = new List<string> { $"{DateTime.Now:HH:mm:ss.fff} {logLevel.ToString()[0]} [{_categoryName}] {formatter(state, exception)}" };
                foreach (var ex in exception.SelectChain(e => e.InnerException))
                {
                    lines.Add($"{ex.GetType().Name}: {ex.Message}");
                    lines.Add(ex.StackTrace);
                }
                File.AppendAllLines(filename, lines);
            }
            catch { }
        }
    }
}
