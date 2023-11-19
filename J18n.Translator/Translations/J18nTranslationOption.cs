using J18n.Translator.Attributes;

namespace J18n.Translations;


public enum J18nLanguage
{
    [Aliases("en" , "en-us")] English,
    [Aliases("zh" , "zh-cn")] Chinese,
    [Aliases("ja" , "jp")] Japanese,
    [Aliases("ko" , "kr")] Korean,
}


public class J18nTranslationOption : ITranslationOption
{
    public J18nTranslationOption( ) { }

    public J18nTranslationOption(J18nLanguage from , J18nLanguage to , ITranslationAPI translationApiType)
    {
        TranslationApiType = translationApiType;
        From = from;
        To = to;
    }

    public ITranslationAPI TranslationApiType { get; set; } = new NormalAPI();
    public IEnumerable<string>? SelectedAPI { get; set; } = Enumerable.Empty<string>();
    public J18nLanguage From { get; set; }
    public J18nLanguage To { get; set; }

    public J18nTranslationOption SetTranslationApiType(ITranslationAPI translationApiType)
    {
        TranslationApiType = translationApiType;
        return this;
    }

    public J18nTranslationOption SetFrom(J18nLanguage from)
    {
        From = from;
        return this;
    }

    public J18nTranslationOption SetTo(J18nLanguage to)
    {
        To = to;
        return this;
    }
}


public interface ITranslationOption
{
    public ITranslationAPI TranslationApiType { get; set; }
    public IEnumerable<string>? SelectedAPI { get; set; }
    public J18nLanguage From { get; set; }
    public J18nLanguage To { get; set; }
}