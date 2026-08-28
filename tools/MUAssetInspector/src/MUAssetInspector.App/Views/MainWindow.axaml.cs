using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MUAssetInspector.App.ViewModels;

namespace MUAssetInspector.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private async void BrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Source Data root" });
        if (folders.Count > 0)
            ViewModel.SetSourceRoot(folders[0].Path.LocalPath);
    }

    private async void BrowseDest_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Destination Data root" });
        if (folders.Count > 0)
            ViewModel.SetDestRoot(folders[0].Path.LocalPath);
    }
}
