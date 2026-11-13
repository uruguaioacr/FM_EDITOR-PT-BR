using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FMOpenEditor.App.ViewModels;
using FMOpenEditor.Core.Localization;

namespace FMOpenEditor.App.Views;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel() => InitializeComponent();

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || TopLevel.GetTopLevel(this) is not { } top) return;
        var picked = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Loc.T("Choose your Football Manager graphics folder"),
            AllowMultiple = false,
        });
        if (picked.Count > 0 && picked[0].TryGetLocalPath() is { } path) vm.GraphicsFolder = path;
    }
}
