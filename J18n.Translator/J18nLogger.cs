using Serilog;
using System.Diagnostics;
using System.Text;

namespace J18n.Translator;

public static class J18nLoggerExtensions
{
    /// <summary>
    /// <list type="bullet">
    /// <item>Conditionally writes or logs a message with the specified log level and optional exception.</item>
    /// <item>If the logger is enabled for the specified log level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="logLevel">The log level of the message.</param>
    /// <param name="message">The log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the log message.</param>
    public static void WriteOrLog(this IJ18nLogger logger , LogLevel logLevel , string message , Exception? exception = null)
    {
        if(logger.IsEnabled(logLevel))
        {
            if(exception is null)
            {
                logger.Write(logLevel , message);
                return;
            }

            logger.Write(logLevel , exception , message);
        }
        else
        {
            var msg = $"[Debug] Message: {message}\nException: {exception}";
            Debug.WriteLine(msg);
        }
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the [Debug] log level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void DebugOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Debug , message , exception);
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the Debug [Info] level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void InfoOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Info , message , exception);
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the Debug [Warn] level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void WarnOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Warn , message , exception);
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the Debug [Error] level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void ErrorOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Error , message , exception);
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the Debug [Fatal] level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void FatalOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Fatal , message , exception);
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>Writes or logs a debug-level message with optional exception details.</item>
    /// <item>If the logger is enabled for the Debug [Verbose] level, it uses the logger's Write method.</item>
    /// <item>Otherwise, it outputs the message and exception details to the Debug output.</item>
    /// </list>
    /// </summary>
    /// <param name="logger">The logger implementing the IJ18nLogger interface.</param>
    /// <param name="message">The debug-level log message to be written or logged.</param>
    /// <param name="exception">Optional. The exception associated with the debug message.</param>
    public static void VerboseOrLog(this IJ18nLogger logger , string message , Exception? exception = null)
    {
        WriteOrLog(logger , LogLevel.Verbose , message , exception);
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal,
    Verbose
}

public class J18nLogger : IJ18nLogger
{
    static Serilog.Events.LogEventLevel LogLevelMapping(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Info => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warn => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            LogLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            _ => Serilog.Events.LogEventLevel.Information,
        };
    }

    private static ILogger? _logger;
    public static volatile object _lock = new object();
    public static ILogger Logger
    {
        get
        {
            if(_logger is null)
            {
                lock(_lock)
                {
                    _logger ??= InitILogger(GlobalSettings.Logger.LogFile);
                }
            }

            return _logger;
        }
    }

    public J18nLogger(string logPath = GlobalSettings.Logger.LogFile)
    {
        _logger = InitILogger(logPath);
    }

    private static ILogger InitILogger(string logPath)
    {
        // 初始化 Serilog 日志记录器
        return new LoggerConfiguration()
            .WriteTo.Conditional((logevent) => logevent.Level >= Serilog.Events.LogEventLevel.Debug ,
            (sinkConfig) => sinkConfig.Console())
            .WriteTo.Conditional((logEvent) => logEvent.Level >= Serilog.Events.LogEventLevel.Information ,
            (sinkConfig) => sinkConfig.File(logPath ,
            rollOnFileSizeLimit: true ,
            rollingInterval: RollingInterval.Day ,
            fileSizeLimitBytes: 4 * 1024 * 1024 ,
            encoding: Encoding.UTF8))
            .CreateLogger();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return Logger.IsEnabled(LogLevelMapping(logLevel));
    }

    public void Write(LogLevel logLevel , string message)
    {
        Logger.Write(LogLevelMapping(logLevel) , message);
    }

    public void Write(LogLevel logLevel , Exception exception , string message)
    {
        Logger.Write(LogLevelMapping(logLevel) , exception , message);
    }

    public void Write(LogLevel logLevel , string messageTemplate , params object?[]? propertyValues)
    {
        Logger.Write(LogLevelMapping(logLevel) , messageTemplate , propertyValues);
    }

    public void Write(LogLevel logLevel , Exception exception , string messageTemplate , params object?[] propertyValues)
    {
        Logger.Write(LogLevelMapping(logLevel) , exception , messageTemplate , propertyValues);
    }

    public void Debug(string message)
    {
        Logger.Debug(message);
    }

    public void Info(string message)
    {
        Logger.Information(message);
    }

    public void Warning(string message)
    {
        Logger.Warning(message);
    }

    public void Error(string message)
    {
        Logger.Error(message);
    }

    public void Fatal(string message)
    {
        Logger.Fatal(message);
    }

    public void Verbose(string message)
    {
        Logger.Verbose(message);
    }

    public void Debug(Exception? exception , string messageTemplate , params object?[]? propertyValues)
    {
        Write(LogLevel.Debug , exception , messageTemplate , propertyValues);
    }

    public void CloseAndFlush( )
    {
        // Serilog 不需要显式的关闭和刷新
        Serilog.Log.CloseAndFlush();
    }

    public ValueTask CloseAndFlushAsync( )
    {
        // Serilog 不需要显式的异步关闭和刷新

        return Serilog.Log.CloseAndFlushAsync();
    }

    public IJ18nLogger GetLogger( )
    {
        return this;
    }

}

public interface IJ18nLogger
{
    IJ18nLogger GetLogger( );
    bool IsEnabled(LogLevel logLevel);
    void Write(LogLevel logLevel , string message);
    void Write(LogLevel logLevel , Exception exception , string message);
    void Write(LogLevel logLevel , string messageTemplate , params object?[]? propertyValues);
    void Write(LogLevel logLevel , Exception exception , string messageTemplate , params object?[] propertyValues);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Fatal(string message);
    void Verbose(string message);
    void Debug(Exception? exception , string messageTemplate , params object?[]? propertyValues);
    void CloseAndFlush( );
    ValueTask CloseAndFlushAsync( );
}
