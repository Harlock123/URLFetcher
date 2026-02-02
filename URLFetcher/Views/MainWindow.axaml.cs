using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using URLFetcher.ViewModels;

namespace URLFetcher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.FolderSelectionRequested += OnFolderSelectionRequested;
        }
    }

    private async Task<string?> OnFolderSelectionRequested()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storageProvider)
            return null;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Download Folder",
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
