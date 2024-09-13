namespace LinuxSystemControlApp.Utils
{
    public static class Functions
    {
        public static string GetReadableFileSize(long bytes)
        {
            string[] sizes = ["bytes", "KB", "MB", "GB", "TB"];
            double len = bytes;
            int order = 0;

            // Move to the next unit if needed
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            // Format the size to 2 decimal places
            return $"{len:0.##} {sizes[order]}";
        }

        public static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length > maxLength)
            {
                return string.Concat(input.AsSpan(0, maxLength), "...");
            }

            return input;
        }
    }
}
