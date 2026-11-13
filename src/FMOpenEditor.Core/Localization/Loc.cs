using System.Globalization;
using System.Text.Json;

namespace FMOpenEditor.Core.Localization;

/// <summary>A language the editor can be shown in.</summary>
public sealed record LanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Translates the editor's interface. The English text is the key: anything without a translation
/// is shown in English, so a missing entry never breaks a screen. Translations are embedded JSON
/// files named <c>i18n.&lt;code&gt;.json</c> mapping English text to the translated text.
/// The language is chosen once at start-up (see <see cref="Use"/>); changing it needs a restart.
/// </summary>
public static class Loc
{
    public const string English = "en";
    public const string PortugueseBrazil = "pt-BR";

    public static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new(English, "English"),
        new(PortugueseBrazil, "Português (Brasil)"),
    ];

    private static Dictionary<string, string> _map = new(StringComparer.Ordinal);

    /// <summary>The language in use.</summary>
    public static string Language { get; private set; } = English;

    /// <summary>Culture for dates shown to the user. Numbers stay invariant so typed values parse the same everywhere.</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

    public static bool IsEnglish => Language == English;

    /// <summary>The language to use for a saved choice; null or unknown follows the system language.</summary>
    public static string Resolve(string? saved)
    {
        if (saved is not null && Languages.Any(l => l.Code == saved)) return saved;
        return CultureInfo.CurrentUICulture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
            ? PortugueseBrazil
            : English;
    }

    /// <summary>Switches the interface language. Unknown codes fall back to English.</summary>
    public static void Use(string code)
    {
        var map = code == English ? null : LoadEmbedded(code);
        if (map is null)
        {
            _map = new(StringComparer.Ordinal);
            Language = English;
            Culture = CultureInfo.InvariantCulture;
            return;
        }
        _map = map;
        Language = code;
        try { Culture = CultureInfo.GetCultureInfo(code); }
        catch (CultureNotFoundException) { Culture = CultureInfo.InvariantCulture; }
    }

    /// <summary>The translation of <paramref name="text"/>, or the text itself when there is none.</summary>
    public static string T(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        return _map.TryGetValue(text, out var t) && t.Length > 0 ? t : text;
    }

    /// <summary>
    /// Translates text whose meaning depends on where it is shown (the "Balance" column is money, the
    /// "Balance" attribute is not). Looks up "context|text" first, then the text alone.
    /// </summary>
    public static string T(string context, string text) =>
        _map.TryGetValue(context + "|" + text, out var t) && t.Length > 0 ? t : T(text);

    /// <summary>Translates a format string such as "Saved {0} change(s)." and fills it in.</summary>
    public static string F(string format, params object?[] args) => string.Format(T(format), args);

    /// <summary>Every entry of an embedded translation, for tests.</summary>
    public static IReadOnlyDictionary<string, string> Entries(string code) =>
        LoadEmbedded(code) ?? new Dictionary<string, string>();

    private static Dictionary<string, string>? LoadEmbedded(string code)
    {
        using var stream = typeof(Loc).Assembly.GetManifestResourceStream($"i18n.{code}.json");
        if (stream is null) return null;
        try
        {
            var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, options);
            return data is null ? null : new Dictionary<string, string>(data, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
