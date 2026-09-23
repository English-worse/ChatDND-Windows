namespace ChatDND.Core.Matching;

public static class ProcessPathNormalizer
{
    public static string Normalize(string path)
    {
        if (!TryNormalize(path, out var normalized))
        {
            throw new ArgumentException(
                "Process path must be a rooted, valid path.",
                nameof(path));
        }

        return normalized;
    }

    public static bool TryNormalize(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        try
        {
            if (!Path.IsPathRooted(trimmed))
            {
                return false;
            }

            normalized = Path.GetFullPath(trimmed);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}
