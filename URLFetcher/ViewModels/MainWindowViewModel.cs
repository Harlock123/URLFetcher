using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using URLFetcher.Services;

namespace URLFetcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isDownloading;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _progressText = string.Empty;

    public ObservableCollection<DownloadedFileViewModel> DownloadedFiles { get; } = new();

    // Event to request folder selection from the view
    public event Func<Task<string?>>? FolderSelectionRequested;

    public MainWindowViewModel() : this(new DownloadService())
    {
    }

    public MainWindowViewModel(IDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        if (FolderSelectionRequested != null)
        {
            var folder = await FolderSelectionRequested.Invoke();
            if (!string.IsNullOrEmpty(folder))
            {
                SelectedFolder = folder;
            }
        }
    }

    private bool CanDownload() =>
        !string.IsNullOrWhiteSpace(Url) &&
        !string.IsNullOrWhiteSpace(SelectedFolder) &&
        !IsDownloading;

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        IsDownloading = true;
        DownloadedFiles.Clear();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            StatusText = "Extracting links...";
            Progress = 0;
            ProgressText = "";

            var links = await _downloadService.ExtractLinksAsync(Url, _cancellationTokenSource.Token);

            if (links.Count == 0)
            {
                StatusText = "No links found on the page";
                return;
            }

            StatusText = $"Found {links.Count} links. Starting download...";

            var progressReporter = new Progress<DownloadProgress>(p =>
            {
                Progress = (double)p.Current / p.Total * 100;
                ProgressText = $"{p.Current} of {p.Total}";
                StatusText = p.Status;
            });

            await foreach (var result in _downloadService.DownloadFilesAsync(
                               links,
                               SelectedFolder,
                               progressReporter,
                               _cancellationTokenSource.Token))
            {
                DownloadedFiles.Add(new DownloadedFileViewModel(result.FileName, result.Success, result.ErrorMessage));
            }

            var successCount = DownloadedFiles.Count(f => f.Success);
            var failCount = DownloadedFiles.Count(f => !f.Success);
            StatusText = $"Complete: {successCount} downloaded, {failCount} failed";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private bool CanCancel() => IsDownloading;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        StatusText = "Cancelling...";
    }
}

public class DownloadedFileViewModel
{
    public string FileName { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public string StatusIcon => Success ? "\u2713" : "\u2717";
    public string DisplayText => Success ? FileName : $"{FileName} - {ErrorMessage}";

    public DownloadedFileViewModel(string fileName, bool success, string? errorMessage = null)
    {
        FileName = fileName;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool success)
        {
            return success ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
