using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Storage;

public static class AppPaths
{
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Looma"
    );

    public static string DatabasePath => Path.Combine(Root, "looma.db");
    public static string DocumentsFolder => Path.Combine(Root, "documents");
    public static string FilesFolder => DocumentsFolder;

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(DocumentsFolder);
    }

    public static void EnsureDatabaseCreated(LoomaDbContext context)
    {
        context.Database.Migrate();
    }

    public static string GetDocumentStoragePath(Guid id)
    {
        var exact = Path.Combine(DocumentsFolder, id.ToString());
        if (File.Exists(exact))
            return exact;

        var match = Directory.EnumerateFiles(DocumentsFolder, $"{id}.*")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return match ?? exact;
    }

    public static string BuildDocumentFileName(Guid id, string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return string.IsNullOrWhiteSpace(extension)
            ? id.ToString()
            : $"{id}{extension}";
    }
}
