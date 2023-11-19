using J18nTranslator.Attributes;

namespace J18nTranslator.Translations;

[Aliases("Normal Translation API")]
public class NormalAPI : ITranslationAPI
{
    public IEnumerable<string> SupportAPIs { get; } = typeof(APIs).GetEnumNames();
    public enum APIs
    {
        [Aliases("百度翻译")] Baidu,
        [Aliases("DeepL")] DeepL,
        [Aliases("有道翻译")] Youdao,
        [Aliases("谷歌翻译")] Google,
        [Aliases("微软翻译")] Bing,
    }
}

[Aliases("AI Translation API")]
public class AIAPI : ITranslationAPI
{
    public IEnumerable<string> SupportAPIs { get; } = typeof(APIs).GetEnumNames();

    public enum APIs
    {
        [Aliases("文心一言")] wenxinyiyan,
        [Aliases("星火大模型")] xinghuodamoxing,
        [Aliases("ChatGPT3.5")] OpenAI,
        [Aliases("智谱清言")] zhipuqingyan
    }
}


public interface ITranslationAPI
{
    public IEnumerable<string> SupportAPIs { get; }
    public enum APIs;
}

