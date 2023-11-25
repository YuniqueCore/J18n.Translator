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
}
