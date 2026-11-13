using System.Text.RegularExpressions;
using FMOpenEditor.Core.Localization;

namespace FMOpenEditor.Tests;

/// <summary>
/// The Portuguese translation ships embedded and keeps every placeholder of the English text.
/// These tests only read the translation file; they never switch the language, so tests that check
/// English text can run alongside them.
/// </summary>
public class LocalizationTests
{
    private static readonly Regex Placeholder = new(@"\{(\d+)(:[^}]*)?\}");

    [Fact]
    public void Portuguese_translation_is_embedded()
    {
        var entries = Loc.Entries(Loc.PortugueseBrazil);
        Assert.True(entries.Count > 1000, $"only {entries.Count} entries");
        Assert.Equal("Salvar mudanças", entries["Save changes"]);
    }

    [Fact]
    public void Every_translation_keeps_the_placeholders_of_its_English_text()
    {
        static string[] Of(string s) => Placeholder.Matches(s).Select(m => m.Value).Distinct().Order().ToArray();
        var wrong = Loc.Entries(Loc.PortugueseBrazil)
            .Where(kv => !Of(kv.Key.Contains('|') ? kv.Key[(kv.Key.IndexOf('|') + 1)..] : kv.Key).SequenceEqual(Of(kv.Value)))
            .Select(kv => kv.Key)
            .ToList();
        Assert.Empty(wrong);
    }

    [Fact]
    public void Every_translation_formats_without_errors()
    {
        var args = Enumerable.Range(0, 10).Select(_ => (object)new AnyFormat()).ToArray();
        foreach (var (key, value) in Loc.Entries(Loc.PortugueseBrazil))
        {
            if (!Placeholder.IsMatch(value)) continue;
            var ex = Record.Exception(() => string.Format(value, args));
            Assert.True(ex is null, $"'{key}': {ex?.Message}");
        }
    }

    /// <summary>Accepts any format specifier ("N0", "X", "T"), so only the braces themselves are tested.</summary>
    private sealed class AnyFormat : IFormattable
    {
        public string ToString(string? format, IFormatProvider? provider) => "x";
    }

    [Fact]
    public void Unknown_languages_and_saved_choices_resolve_sensibly()
    {
        Assert.Equal(Loc.PortugueseBrazil, Loc.Resolve(Loc.PortugueseBrazil));
        Assert.Equal(Loc.English, Loc.Resolve(Loc.English));
        Assert.Contains(Loc.Resolve("xx-YY"), new[] { Loc.English, Loc.PortugueseBrazil });
        Assert.Empty(Loc.Entries("xx-YY"));
    }
}
