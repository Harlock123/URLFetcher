using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace URLFetcher.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff", ".tif", ".ico"
    };

    public DownloadService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; URLFetcher/1.0)");
    }

    public async Task<List<string>> ExtractLinksAsync(string url, CancellationToken cancellationToken = default)
    {
        var baseUri = new Uri(url);
        var html = await _httpClient.GetStringAsync(url, cancellationToken);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = new List<string>();

        // Find links in <a href> tags
        var anchorNodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchorNodes != null)
        {
            foreach (var node in anchorNodes)
            {
                var href = node.GetAttributeValue("href", "");
                TryAddImageLink(baseUri, href, links);
            }
        }

        // Also find <img src> tags for direct image references
        var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgNodes != null)
        {
            foreach (var node in imgNodes)
            {
                var src = node.GetAttributeValue("src", "");
                TryAddImageLink(baseUri, src, links);
            }
        }

        return links.Distinct().ToList();
    }

    private static void TryAddImageLink(Uri baseUri, string href, List<string> links)
    {
        if (string.IsNullOrWhiteSpace(href))
            return;

        // Skip non-file links
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("#") ||
            href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            // Resolve relative URLs to absolute
            var absoluteUri = new Uri(baseUri, href);
            var extension = Path.GetExtension(absoluteUri.LocalPath);

            // Only include image files
            if (ImageExtensions.Contains(extension))
            {
                links.Add(absoluteUri.ToString());
            }
        }
        catch (UriFormatException)
        {
            // Skip malformed URLs
        }
    }

    public async IAsyncEnumerable<DownloadResult> DownloadFilesAsync(
        IReadOnlyList<string> urls,
        string destinationFolder,
        IProgress<DownloadProgress>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fileNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = urls.Count;

        for (var i = 0; i < urls.Count; i++)
        {
            var url = urls[i];
            var fileName = GetFileNameFromUrl(url);

            progress?.Report(new DownloadProgress(i + 1, total, fileName, $"Downloading {fileName}..."));

            var result = await DownloadSingleFileAsync(url, destinationFolder, fileName, fileNameCounts, cancellationToken);
            yield return result;

            if (!result.Success && result.ErrorMessage == "Cancelled")
            {
                yield break;
            }
        }

        progress?.Report(new DownloadProgress(total, total, "", "Download complete"));
    }

    private async Task<DownloadResult> DownloadSingleFileAsync(
        string url,
        string destinationFolder,
        string fileName,
        Dictionary<string, int> fileNameCounts,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Try to get filename from Content-Disposition header
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition?.FileName != null)
            {
                fileName = contentDisposition.FileName.Trim('"');
            }

            var uniquePath = GetUniqueFilePath(destinationFolder, fileName, fileNameCounts);
            var actualFileName = Path.GetFileName(uniquePath);

            await using var fileStream = new FileStream(uniquePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            return new DownloadResult(actualFileName, true);
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult(fileName, false, "Cancelled");
        }
        catch (Exception ex)
        {
            return new DownloadResult(fileName, false, ex.Message);
        }
    }

    private static string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "download";
            }

            // Clean invalid characters
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
        catch
        {
            return "download";
        }
    }

    private static string GetUniqueFilePath(string folder, string fileName, Dictionary<string, int> fileNameCounts)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        // Track how many times we've seen this filename
        if (!fileNameCounts.TryGetValue(fileName, out var count))
        {
            count = 0;
        }

        string path;
        if (count == 0)
        {
            path = Path.Combine(folder, fileName);
            if (!File.Exists(path))
            {
                fileNameCounts[fileName] = 1;
                return path;
            }
        }

        // Find a unique name
        var counter = count > 0 ? count : 1;
        do
        {
            path = Path.Combine(folder, $"{nameWithoutExt}_{counter}{ext}");
            counter++;
        } while (File.Exists(path));

        fileNameCounts[fileName] = counter;
        return path;
    }
}
