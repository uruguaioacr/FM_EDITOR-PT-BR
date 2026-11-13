using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMOpenEditor.Core.Research;
using FMOpenEditor.Core.Localization;

namespace FMOpenEditor.App.ViewModels.Research;

public sealed record CandidateRow(string OffsetText, string Type, string Formula, string Stored, string Json);

/// <summary>
/// Enter several records and the value the game shows for each; it finds the offset, type and scale
/// that explain all of them.
/// </summary>
public sealed partial class CorrelatorViewModel(ResearchContext context) : ObservableObject
{
    public ObservableCollection<CandidateRow> Candidates { get; } = [];

    [ObservableProperty] private string _samplesText = "";
    [ObservableProperty] private string _length = "0x800";
    [ObservableProperty] private string _status = Loc.T("One record per line: its base address, then the value the game shows. Ten or more records with different values work best.");
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private CandidateRow? _selected;

    public void AddSample(nint recordBase)
    {
        var line = ResearchFormat.Hex(recordBase) + " ";
        SamplesText = string.IsNullOrWhiteSpace(SamplesText) ? line : SamplesText.TrimEnd() + Environment.NewLine + line;
        Status = Loc.F("Added {0}. Type the value the game shows for it after the address.", ResearchFormat.Hex(recordBase));
    }

    [RelayCommand]
    private async Task Correlate()
    {
        if (!ResearchFormat.TryLength(Length, out var len)) { Status = Loc.T("Enter a record length, e.g. 0x800."); return; }
        var samples = new List<CorrelationSample>();
        var lineNo = 0;
        foreach (var raw in SamplesText.Split('\n'))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split([' ', '\t', ';', '='], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !ResearchFormat.TryHex(parts[0], out var a, context) || !ResearchFormat.TryNumber(parts[1], out var v))
            {
                Status = Loc.F("Line {0} should be \"address value\", e.g. \"0x1F2A3B40 15\".", lineNo);
                return;
            }
            samples.Add(new CorrelationSample((nint)a, v));
        }

        IsBusy = true;
        Status = Loc.F("Comparing {0} records…", samples.Count);
        try
        {
            var result = await Task.Run(() => FieldCorrelator.Correlate(context.Memory, samples, len));
            Candidates.Clear();
            foreach (var c in result.Candidates)
            {
                // The definition's scale means shown = stored ÷ scale, which fits all three kinds.
                var json = $"{{ \"offset\": \"0x{c.Offset:X}\", \"type\": \"{c.Type}\", \"scale\": {c.Scale.ToString(CultureInfo.InvariantCulture)} }}";
                Candidates.Add(new CandidateRow($"0x{c.Offset:X}", c.Type, c.Formula,
                    string.Join(", ", c.Stored.Take(8).Select(x => x.ToString("G6", CultureInfo.InvariantCulture))), json));
            }
            var summary = result.Candidates.Count switch
            {
                0 => Loc.T("No match."),
                1 => Loc.T("One match: very likely the field. Confirm by writing to it in the game."),
                var n => Loc.F("{0} matches. Add records with different values to narrow it down.", n),
            };
            Status = string.Join(" ", result.Warnings.Prepend(summary));
        }
        catch (Exception ex) { Status = ex.Message; }
        finally { IsBusy = false; }
    }
}
