using System.Reflection;

namespace WebForms.SourceGenerator;

public static class EmbeddedResource
{
    private static readonly string? BaseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public static string GetContent(string relativePath)
    {
        if (BaseDir != null)
        {
            var filePath = Path.Combine(BaseDir, Path.GetFileName(relativePath));

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }

        var baseName = Assembly.GetExecutingAssembly().GetName().Name;
        var resourceName = relativePath
            .TrimStart('.')
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        var manifestResourceName = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName));

        if (string.IsNullOrEmpty(manifestResourceName))
        {
            throw new InvalidOperationException($"Did not find required resource ending in '{resourceName}' in assembly '{baseName}'.");
        }

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(manifestResourceName);

        if (stream == null)
        {
            throw new InvalidOperationException($"Did not find required resource '{manifestResourceName}' in assembly '{baseName}'.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}