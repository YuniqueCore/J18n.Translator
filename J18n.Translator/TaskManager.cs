using System.Runtime.CompilerServices;

namespace J18n.Translator;

public class TaskManager
{
    public static async Task RunTask(Action action , CancellationToken cToken , string message = "" , bool throwEx = true , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $@"Task.({methodName}).(message: {message})";
        Task? task = null;
        try
        {
            task = Task.Run(action , cToken);
            await task;
        }
        catch(Exception ex)
        {
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
        }

    }

    public static async Task<TResult?> RunTask<TResult>(Func<TResult?> func , CancellationToken cToken , string message = "" , bool throwEx = true , [CallerMemberName] string? methodName = null)
    {
        string logMsg = $@"Task.({methodName}).(message: {message})";
        Task<TResult?>? task = null;
        try
        {
            task = Task.Run(func , cToken);
            return await task;

        }
        catch(Exception ex)
        {
            if(throwEx)
            {
                throw;
            }
        }
        finally
        {
            task?.Dispose();
        }
        return await Task.FromResult<TResult?>(default);
    }
}