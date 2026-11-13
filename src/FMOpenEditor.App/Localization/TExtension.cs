using Avalonia.Markup.Xaml;
using FMOpenEditor.Core.Localization;

namespace FMOpenEditor.App.Localization;

/// <summary>
/// Translated text in XAML: <c>Text="{l:T 'Save changes'}"</c>. The English text is the key; the
/// language is fixed at start-up, so the value is read once.
/// </summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }

    public TExtension(string text) => Text = text;

    public string Text { get; set; } = "";

    // XAML keys write apostrophes as ’ (a plain ' would end the quoted text); the English keeps the plain one.
    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Text.Replace('’', '\''));
}
