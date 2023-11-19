using J18n.Translations;

namespace J18n.Translator.Translations;

/// <summary>
/// Translation Engine Base Factory
/// </summary>
public abstract class J18nTranslationBaseEngine : ITranslationEngine
{
    public ITranslationOption TranslationOption { get; set; } = new J18nTranslationOption()
    {
        TranslationApiType = new NormalAPI() ,
        From = J18nLanguage.English ,
        To = J18nLanguage.Chinese ,
    };

    public abstract Task<bool> TranslateAsync(CancellationToken ctsToken);
}

public interface ITranslationEngine
{
    public ITranslationOption TranslationOption { get; set; }
    public abstract Task<bool> TranslateAsync(CancellationToken ctsToken);
}
