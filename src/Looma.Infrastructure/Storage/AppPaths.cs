using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Storage;

public static class AppPaths
{
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Looma"
    );

    public static string DatabasePath => Path.Combine(Root, "looma.db");
    public static string FilesFolder  => Path.Combine(Root, "files");

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(FilesFolder);
    }

    public static void EnsureDatabaseCreated(LoomaDbContext context)
    {
        context.Database.Migrate();
    }
}