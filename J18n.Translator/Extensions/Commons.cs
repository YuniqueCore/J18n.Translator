﻿using System.Diagnostics.CodeAnalysis;

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
