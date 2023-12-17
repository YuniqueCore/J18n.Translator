using System.Diagnostics.CodeAnalysis;

namespace J18n.Translator.Extensions;

public static class Commons
{

    /// <summary>
    /// Repeatedly executes a specified condition delegate until it returns false or the timeout is reached.
    /// </summary>
    /// <typeparam name="T">Type of the return value of the delegate.</typeparam>
    /// <param name="condition">Delegate returning true to continue or false to interrupt.</param>
    /// <param name="timeout">Maximum execution time in seconds, default is 60 seconds.</param>
    public static void DoWhileUntilOrTimeout(Func<bool> condition , int timeout = 60)
    {
        // Continue looping while the condition is true or until the timeout is reached
        DateTime endTime = DateTime.Now.AddSeconds(timeout);
        do
        {
            // Exit the loop if the condition returns false
            if(!condition()) break;
        } while(DateTime.Now < endTime);
    }


    /// <summary>
    /// Returns the original IEnumerable if it is not null and contains elements; otherwise, returns an empty IEnumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements in the IEnumerable.</typeparam>
    /// <param name="list">The input IEnumerable to check.</param>
    /// <returns>
    /// The original IEnumerable if it is not null and contains elements; otherwise, an empty IEnumerable.
    /// </returns>
    public static IEnumerable<T>? NullOrNotEmpty<T>(this IEnumerable<T> list) where T : class
    {
        return list is not null && list.Any() ? list : null;
    }

    /// <summary>
    /// Returns the original IEnumerable of KeyValuePairs if it is not null and contains elements; otherwise, returns an empty IEnumerable.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the KeyValuePairs.</typeparam>
    /// <typeparam name="TValue">The type of values in the KeyValuePairs.</typeparam>
    /// <param name="pairs">The input IEnumerable of KeyValuePairs to check.</param>
    /// <returns>
    /// The original IEnumerable of KeyValuePairs if it is not null and contains elements; otherwise, an empty IEnumerable.
    /// </returns>
    public static IEnumerable<KeyValuePair<TKey , TValue>>? NullOrNotEmpty<TKey, TValue>(this IEnumerable<KeyValuePair<TKey , TValue>> pairs)
    {
        return pairs is not null && pairs.Any() ? pairs : null;
    }

    /// <summary>
    /// Returns the original Dictionary if it is not null and contains elements; otherwise, returns an empty Dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the Dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the Dictionary.</typeparam>
    /// <param name="dictionary">The input Dictionary to check.</param>
    /// <returns>
    /// The original Dictionary if it is not null and contains elements; otherwise, an empty Dictionary.
    /// </returns>
    public static Dictionary<TKey , TValue>? NullOrNotEmpty<TKey, TValue>(this Dictionary<TKey , TValue>? dictionary)
    {
        return dictionary is not null && dictionary.Count > 0 ? dictionary : null;
    }

    /// <summary>
    /// return nullableList is null ? "" : string.Join(separator , nullableList);
    /// </summary>
    /// <param name="nullableList"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string Join<T>(this IEnumerable<T>? nullableList , string separator)
    {
        return nullableList is null ? "" : string.Join(separator , nullableList);
    }

    /// <summary>
    /// Get the string after the last one identifier.
    /// If not identifier found, return the original string.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? GetLast(this string? text , char identifier = '.')
    {
        if(text == null)
            return null;

        return GetLast(text.AsSpan() , identifier).ToString();
    }

    public static ReadOnlySpan<char> GetLast(this ReadOnlySpan<char> text , char identifier = '.')
    {
        int length = text.Length;

        for(int i = length - 1; i >= 0; i--)
        {
            char ch = text[i];
            if(ch == identifier)
            {
                if(i != length - 1)
                    return text.Slice(i , length - i);
                else
                    return ReadOnlySpan<char>.Empty;
            }
        }
        return text;
    }

}
