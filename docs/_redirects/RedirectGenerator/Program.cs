try
{
    var root = System.Reflection.Assembly.GetExecutingAssembly().Location;
    while (root != null && Path.GetPathRoot(root) != root && Path.GetFileName(root) != "_redirects")
        root = Directory.GetParent(root)?.FullName;
    if (root == null || Path.GetFileName(root) != "_redirects")
    {
        Console.Error.WriteLine("Can't find the _redirects folders");
        return;
    }

    var redirectsDirectory = Path.Combine(root, "redirects");
    if (Directory.Exists(redirectsDirectory))
        Directory.Delete(redirectsDirectory, true);
    Directory.CreateDirectory(redirectsDirectory);

    var redirects = File.ReadAllLines(Path.Combine(root, "_redirects"))
        .Select(line => line.Split(' '))
        .Select(parts => (source: parts[0], target: parts[1]))
        .ToList();

    bool EnsureDirectoryExists(string? fullDirectoryName)
    {
        if (fullDirectoryName == null)
            return false;
        if (Directory.Exists(fullDirectoryName))
            return true;
        if (!EnsureDirectoryExists(Directory.GetParent(fullDirectoryName)?.FullName))
            return false;
        Directory.CreateDirectory(fullDirectoryName);
        return true;
    }

    foreach (var (source, target) in redirects)
    {
        var fileName = source.StartsWith("/") || source.StartsWith("\\") ? source[1..] : source;
        var fullFileName = Path.Combine(redirectsDirectory, fileName);
        var content =
            $"<!doctype html>" +
            $"<html lang=en-us>" +
            $"<head>" +
            $"<title>{target}</title>" +
            $"<link rel=canonical href='{target}'>" +
            $"<meta name=robots content=\"noindex\">" +
            $"<meta charset=utf-8><meta http-equiv=refresh content=\"0; url={target}\">" +
            $"</head>" +
            $"</html>";
        EnsureDirectoryExists(Path.GetDirectoryName(fullFileName));
        File.WriteAllText(fullFileName, content);
    }
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
}