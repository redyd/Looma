// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Text;

namespace Looma.Infrastructure.Storage;

/// <summary>
/// Writes files through a temporary sibling file then an atomic rename, so a crash
/// or power loss never leaves a half-written destination.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string content) =>
        WriteAllBytes(path, Encoding.UTF8.GetBytes(content));

    public static void WriteAllBytes(string path, byte[] content) =>
        Write(path, stream => stream.Write(content));

    public static void Copy(string sourcePath, string destinationPath)
    {
        Write(destinationPath, stream =>
        {
            using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            source.CopyTo(stream);
        });
    }

    public static void Write(string path, Action<Stream> writer)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                writer(stream);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
