namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class GitHubLinks
{
    private const string RepoBaseUrl = "https://github.com/XenoAtom/XenoAtom.Terminal.UI/blob/main/";

    public static bool TryGetSourceUri(string sourcePath, out Uri uri)
    {
        if (!TryGetSourceRelativePath(sourcePath, out var rel))
        {
            uri = default!;
            return false;
        }

        uri = new Uri(RepoBaseUrl + rel.Replace('\\', '/'), UriKind.Absolute);
        return true;
    }

    public static bool TryGetSourceRelativePath(string sourcePath, out string relativePath)
    {
        relativePath = string.Empty;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var start = Directory.GetCurrentDirectory();
        var root = FindRepoRoot(start) ?? FindRepoRoot(AppContext.BaseDirectory);
        if (root is null)
        {
            return false;
        }

        try
        {
            relativePath = Path.GetRelativePath(root, sourcePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindRepoRoot(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
        {
            return null;
        }

        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "license.txt")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return null;
    }
}

