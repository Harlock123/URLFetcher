using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace URLFetcher.Services;

public record DownloadProgress(int Current, int Total, string CurrentFile, string Status);

public record DownloadResult(string FileName, bool Success, string? ErrorMessage = null);

public interface IDownloadService
{
    Task<List<string>> ExtractLinksAsync(string url, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DownloadResult> DownloadFilesAsync(
        IReadOnlyList<string> urls,
        string destinationFolder,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
