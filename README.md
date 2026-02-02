# URL Fetcher

A cross-platform desktop application built with .NET 9 and Avalonia UI that downloads images from web pages.

## Features

- Enter any URL to scan for images
- Extracts images from both `<a href>` links and `<img src>` tags
- Supports common image formats: JPG, JPEG, PNG, BMP, GIF, WEBP, TIFF, ICO
- Select a local folder to save downloaded images
- Automatically renames duplicate files with numeric suffixes (image_1.png, image_2.png, etc.)
- Progress bar and status updates during download
- Cancel downloads in progress
- Cross-platform: runs on Windows, macOS, and Linux

## Requirements

- .NET 9.0 SDK or later

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project URLFetcher/
```

## Usage

1. Enter a URL in the text box
2. Click "Browse..." to select a download folder
3. Click "Download All Links" to start downloading images
4. View progress and downloaded files in the list below

## Dependencies

- [Avalonia](https://avaloniaui.net/) - Cross-platform UI framework
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM helpers
- [HtmlAgilityPack](https://html-agility-pack.net/) - HTML parsing

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Lonnie Watson
