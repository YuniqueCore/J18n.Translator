using J18nCore.Attributes;

namespace J18nCore;

public class J18nTranslationOption
{
    public J18nTranslationOption() { }
    public J18nTranslationOption(J18nLanguage from, J18nLanguage to, ITranslationAPI translationAPI)
    {
        TranslationAPI = translationAPI;
        From = from;
        To = to;
    }
    public ITranslationAPI TranslationAPI { get; set; } = new NormalAPI();
    public IEnumerable<string> SupportAPIs => TranslationAPI.SupportAPIs;
    public J18nLanguage From { get; private set; }
    public J18nLanguage To { get; set; }

    public J18nTranslationOption SetTranslationAPI(ITranslationAPI translationAPI)
    {
        this.TranslationAPI = translationAPI;
        return this;
    }

    public J18nTranslationOption SetFrom(J18nLanguage from)
    {
        this.From = from;
        return this;
    }

    public J18nTranslationOption SetTo(J18nLanguage to)
    {
        this.To = to;
        return this;
    }
}



public enum J18nLanguage
{
    [Aliases("en", "en-us")]
    English,
    [Aliases("zh", "zh-cn")]
    Chinese,
    [Aliases("ja", "jp")]
    Japanese,
    [Aliases("ko", "kr")]
    Korean,
}


[Aliases("Normal Translation API")]
public class NormalAPI : ITranslationAPI
{
    enum APIs
    {
        [Aliases("百度翻译")] Baidu,
        [Aliases("DeepL")] DeepL,
        [Aliases("有道翻译")] Youdao,
        [Aliases("谷歌翻译")] Google,
        [Aliases("微软翻译")] Bing,
    }
    public IEnumerable<string> SupportAPIs => typeof(APIs).GetEnumNames();
}

[Aliases("AI Translation API")]
public class AIAPI : ITranslationAPI
{
    enum APIs
    {
        [Aliases("文心一言")]
        wenxinyiyan,
        [Aliases("星火大模型")]
        xinghuodamoxing,
        [Aliases("ChatGPT3.5")]
        OpenAI,
        [Aliases("智谱清言")]
        zhipuqingyan
    }
    public IEnumerable<string> SupportAPIs => typeof(APIs).GetEnumNames();
}


public interface ITranslationAPI
{
    /// <summary>
    /// The support apis
    /// </summary>
    public IEnumerable<string> SupportAPIs { get; }
}