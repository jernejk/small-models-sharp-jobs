namespace Workshop.App;

internal static class WorkshopPaths
{
    /// <summary>Walks up from the binary so `dotnet run` behaves the same from any directory.</summary>
    public static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    public static string Resolve(string? supplied, string relativeDefault) =>
        Path.GetFullPath(supplied ?? Path.Combine(RepoRoot(), relativeDefault));
}
