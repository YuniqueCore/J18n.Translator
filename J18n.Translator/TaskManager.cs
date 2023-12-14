using System.Runtime.CompilerServices;

namespace J18n.Translator;

public class TaskManager
{
    private static readonly IJ18nLogger _logger = new J18nLogger();
    public static async Task RunTask(Action action , CancellationToken cToken , string message = "" , bool throwEx = true , LogLevel logLevel = LogLevel.Debug , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $"Task.({methodName}).(message: {message})";
        Task? task = null;
        Exception? exception = null;
        try
        {
            task = Task.Run(action , cToken);
            await task;
        }
        catch(Exception? ex)
        {
            exception = ex;
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
            _logger.WriteOrLog(logLevel , logMsg , exception);
        }
    }
    public static async Task<TResult?> RunTask<TResult>(Func<TResult?> func , CancellationToken cToken , string message = "" , bool throwEx = true , LogLevel logLevel = LogLevel.Debug , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $"Task.({methodName}).(message: {message})";
        Task<TResult?>? task = null;
        Exception? exception = null;
        try
        {
            task = Task.Run(func , cToken);
            return await task;
        }
        catch(Exception? ex)
        {
            exception = ex;
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
            _logger.WriteOrLog(logLevel , logMsg , exception);
        }
        return await Task.FromResult<TResult?>(default);
    }
    public static async Task RunTaskAsync(Func<Task> asyncAction , CancellationToken cToken , string message = "" , bool throwEx = true , LogLevel logLevel = LogLevel.Debug , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $"Task.({methodName}).(message: {message})";
        Task? task = null;
        Exception? exception = null;
        try
        {
            task = Task.Run(asyncAction , cToken);
            await task;
        }
        catch(Exception? ex)
        {
            exception = ex;
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
            _logger.WriteOrLog(logLevel , logMsg , exception);
        }
    }
    public static async Task<TResult?> RunTaskAsync<TResult>(Func<Task<TResult?>> asyncFunc , CancellationToken cToken , string message = "" , bool throwEx = true , LogLevel logLevel = LogLevel.Debug , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $"Task.({methodName}).(message: {message})";
        Task<TResult?>? task = null;
        Exception? exception = null;
        try
        {
            task = Task.Run(asyncFunc , cToken);
            return await task;
        }
        catch(Exception? ex)
        {
            exception = ex;
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
            _logger.WriteOrLog(logLevel , logMsg , exception);
        }
        return await Task.FromResult<TResult?>(default);
    }
}