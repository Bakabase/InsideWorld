﻿using Bakabase.Abstractions.Components.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bakabase.Abstractions.Components.Cover;

public record CoverDiscoveryResult(bool IsVirtualPath, string Path, string Ext, byte[]? Data = null)
{
    /// <summary>
    /// The path may be an inner path inside a compressed file, video, etc. You should check its existence before apply io operations on it.
    /// </summary>
    public string Path { get; } = Path;

    public byte[]? Data { get; } = Data;
    private readonly string _ext = Ext;

    /// <summary>
    /// It means <see cref="Path"/> is not a real path if it is true.
    /// </summary>
    public bool IsVirtualPath { get; } = IsVirtualPath;

    public async Task<string> SaveTo(string pathWithoutExtension, bool overwrite, CancellationToken ct)
    {
        var path = $"{pathWithoutExtension}{_ext}";

        if (!overwrite && File.Exists(path))
        {
            throw new Exception(
                $"Failed to save cover, since there is already a file exists in [{path}] and {nameof(overwrite)} is not set to true.");
        }

        if (Data != null)
        {
            await File.WriteAllBytesAsync(path, Data, ct);
        }
        else
        {
            await using var fs = new FileStream(Path, FileMode.Open);
            await using var to = new FileStream(path, FileMode.Truncate);
            await fs.CopyToAsync(to, ct);
        }

        return path;
    }

    public async Task<Image<Argb32>> LoadByImageSharp(CancellationToken ct)
    {
        return IsVirtualPath
            ? await Image.LoadAsync<Argb32>(new MemoryStream(Data!), ct)
            : await Image.LoadAsync<Argb32>(Path, ct);
    }
}