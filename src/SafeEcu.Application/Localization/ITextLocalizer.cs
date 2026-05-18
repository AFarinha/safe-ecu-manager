namespace SafeEcu.Application.Localization;

public interface ITextLocalizer
{
    string CurrentLanguageCode { get; }

    IReadOnlyList<LanguageOption> SupportedLanguages { get; }

    void SetLanguage(string languageCode);

    string Text(string key);
}
