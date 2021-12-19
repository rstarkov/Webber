using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using RT.Util.ExtensionMethods;

namespace Webber.Server.Services;

public static class Console2LoggerExtensions
{
    public static void AddConsole2(this ILoggingBuilder builder)
    {
        // as documented in https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider ... NOT
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, Console2LoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<Console2LoggerOptions, Console2LoggerProvider>(builder.Services);
    }
}

public class Console2LoggerOptions
{
    // this is not how loggers are usually enabled, but this is the only way to register a logger that defaults to disabled until explicitly enabled in the configuration
    // (well there is another way: AddFilter, but that approach means that enabling the logger also overrides all the standard logging levels pre-configured by ASP for each category)
    public bool Enabled { get; set; } = false;
}

[ProviderAlias("Console2")] // as documented in https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider ... NOT
public class Console2LoggerProvider : ILoggerProvider
{
    private bool _enabled;

    public Console2LoggerProvider(Console2LoggerOptions options)
    {
        _enabled = options.Enabled;
    }

    public Console2LoggerProvider(IOptions<Console2LoggerOptions> options)
        : this(options.Value)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName, _enabled);
    }

    public void Dispose()
    {
    }

    private class ConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly bool _enabled;

        public LogLevel Level { get; set; } = LogLevel.None;

        public ConsoleLogger(string categoryName, bool enabled)
        {
            var pos = categoryName.LastIndexOf('.');
            _categoryName = pos < 0 ? categoryName : categoryName.Substring(pos + 1);
            _enabled = enabled;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _enabled && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            Console.ForegroundColor =
                logLevel == LogLevel.Critical ? ConsoleColor.Magenta :
                logLevel == LogLevel.Error ? ConsoleColor.Red :
                logLevel == LogLevel.Warning ? ConsoleColor.Yellow :
                logLevel == LogLevel.Information ? ConsoleColor.White :
                logLevel == LogLevel.Debug ? ConsoleColor.Gray :
                logLevel == LogLevel.Trace ? ConsoleColor.DarkGray : ConsoleColor.Cyan;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {logLevel.ToString()[0]} [{_categoryName}] {formatter(state, exception)}");
            foreach (var ex in exception.SelectChain(e => e.InnerException))
            {
                Console.WriteLine($" {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
