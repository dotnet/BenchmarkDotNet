namespace BenchmarkDotNet.Helpers;

internal static class FileCleanupHelper
{
    public static void Cleanup(string path)
    {
        if (File.Exists(path))
        {
            CleanupFile(new FileInfo(path));
            return;
        }

        if (Directory.Exists(path))
        {
            CleanupDirectory(new DirectoryInfo(path));
        }
    }

    private static void CleanupFile(FileInfo fileInfo)
    {
        TryExecute(() =>
        {
            File.SetAttributes(fileInfo.FullName, FileAttributes.Normal);
            File.Delete(fileInfo.FullName);
        });
    }

    private static void CleanupDirectory(DirectoryInfo directoryInfo)
    {
        // Delete files
        foreach (var file in EnumerateFiles(directoryInfo))
        {
            CleanupFile(file);
        }

        // Delete sub directories
        foreach (var subDirectory in EnumerateSubDirectories(directoryInfo))
        {
            CleanupDirectory(subDirectory);
        }

        // Delete directory
        TryExecute(() => directoryInfo.Delete(recursive: false));
    }

    private static void TryExecute(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // Ignore exceptions for access denied, in use, etc.
        }
    }

#if NETSTANDARD2_0
    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
        => TryExecute(() => directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly), []);

    private static IEnumerable<DirectoryInfo> EnumerateSubDirectories(DirectoryInfo directory)
        => TryExecute(() => directory.EnumerateDirectories(), []);

    private static T TryExecute<T>(Func<T> func, T fallback)
    {
        try
        {
            return func();
        }
        catch
        {
            // Ignore exceptions for access denied, in use, etc.
            return fallback;
        }
    }
#else
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
        => directory.EnumerateFiles("*", EnumerationOptions);

    private static IEnumerable<DirectoryInfo> EnumerateSubDirectories(DirectoryInfo currentDir)
        => currentDir.EnumerateDirectories("*", EnumerationOptions);
#endif
}
