using FMOpenEditor.Core.Editing;
using FMOpenEditor.Core.Model;
using FMOpenEditor.Core.Search;
using FMOpenEditor.Core.Localization;

namespace FMOpenEditor.App.ViewModels;

/// <summary>Turns what the user typed into a value for a property: names for references, labels for choices.</summary>
public static class ValueParser
{
    public static object? Parse(EditorSession session, PropertyDef def, string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return def.Kind is ValueKind.Text ? "" : null;

        switch (def.Kind)
        {
            case ValueKind.Reference when def.RefKind is { } kind:
                if (int.TryParse(text, out var id)) return id;
                var wanted = SearchEngine.Normalize(text);
                var match = session.GetAll(kind)
                    .FirstOrDefault(e => SearchEngine.Normalize(session.NameOf(kind, e.Id)) == wanted)
                    ?? session.GetAll(kind).FirstOrDefault(e => SearchEngine.Normalize(session.NameOf(kind, e.Id)).Contains(wanted));
                return match?.Id ?? throw new FormatException(Loc.F("No {0} called '{1}'.", Loc.T(kind.ToString().ToLowerInvariant()), text));

            case ValueKind.Choice when def.Choices is { } choices:
                var idx = choices.ToList().FindIndex(c => string.Equals(c, text, StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(Loc.T(c), text, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) return idx;
                return int.TryParse(text, out var n) ? n : throw new FormatException(Loc.F("'{0}' isn't a valid {1}.", text, def.DisplayLabel));

            case ValueKind.Boolean:
                return text.ToLowerInvariant() switch
                {
                    "yes" or "y" or "true" or "1" or "on" or "sim" or "s" => true,
                    "no" or "n" or "false" or "0" or "off" or "não" or "nao" => false,
                    _ => throw new FormatException(Loc.T("Use yes or no.")),
                };

            case ValueKind.Money:
                return ParseMoney(text);

            default:
                return text;
        }
    }

    /// <summary>Accepts 1500000, 1.5M, 750k, €2,000,000, in the currency chosen in Settings; returns pounds.</summary>
    public static long ParseMoney(string text) => Money.Parse(text);
}
