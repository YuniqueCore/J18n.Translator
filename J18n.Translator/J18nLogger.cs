using Serilog;
using System.Text;

namespace J18n.Translator;

public class J18nLogger : IJ18nLogger
{
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

    public void Log(string messageTemplate)
    {
        Information(messageTemplate);
    }

    public void Log(string messageTemplate , params object?[]? propertyValues)
    {
        Information(messageTemplate , propertyValues);
    }

    public void Information(string messageTemplate)
    {
        _logger.Information(messageTemplate);
    }

    public void Information(string messageTemplate , params object?[]? propertyValues)
    {
        _logger.Information(messageTemplate , propertyValues);
    }

    public void Warning(string messageTemplate)
    {
        _logger.Warning(messageTemplate);
    }

    public void Verbose(string messageTemplate)
    {
        _logger.Verbose(messageTemplate);
    }

    public void Error(string messageTemplate)
    {
        _logger.Error(messageTemplate);
    }

    public void Fatal(string messageTemplate)
    {
        _logger.Fatal(messageTemplate);
    }

    public void Debug(string messageTemplate)
    {
        _logger.Debug(messageTemplate);
    }

    public void Debug(Exception? exception , string messageTemplate , params object?[]? propertyValues)
    {
        _logger.Debug(exception , messageTemplate , propertyValues);
    }

    /// <summary>
    /// If exception is null log otherwise debug
    /// </summary>
    /// <param name="logMsg"></param>
    /// <param name="exception"></param>
    public void DebugOrLog(string logMsg , Exception? exception)
    {
        if(exception is null)
        {
            Debug(exception , logMsg);
        }
        else
        {
            Log(logMsg);
        }
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
    void Log(string messageTemplate);
    void Log(string messageTemplate , params object?[]? propertyValues);
    void Information(string messageTemplate);
    void Information(string messageTemplate , params object?[]? propertyValues);
    void Warning(string messageTemplate);
    void Verbose(string messageTemplate);
    void Error(string messageTemplate);
    void Fatal(string messageTemplate);
    void Debug(string messageTemplate);
    void Debug(Exception? exception , string messageTemplate , params object?[]? propertyValues);
    void DebugOrLog(string logMsg , Exception? exception);
    void CloseAndFlush( );
    ValueTask CloseAndFlushAsync( );
}
