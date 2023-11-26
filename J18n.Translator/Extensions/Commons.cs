using System.Diagnostics.CodeAnalysis;

namespace J18n.Translator.Extensions;

public static class Commons
{
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
