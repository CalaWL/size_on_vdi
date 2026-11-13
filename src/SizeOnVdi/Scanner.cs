namespace SizeOnVdi;

public record LargeFile(string FullName, long Bytes)
{
    public double MB => Math.Round(Bytes / 1048576d, 2);
}
public record ScanResult(List<LargeFile> Files, long Count, long Skipped);

public static class Scanner
{
    public static ScanResult Scan(string root, CancellationToken token, Action<long>? progress = null)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var folders = new Stack<string>();
        folders.Push(root);
        var largest = new PriorityQueue<LargeFile, long>();
        long count = 0, skipped = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (folders.TryPop(out var folder))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos())
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if ((entry.Attributes & FileAttributes.Directory) != 0)
                        {
                            // Do not follow junctions: they can loop or leave the profile.
                            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) skipped++;
                            else folders.Push(entry.FullName);
                        }
                        else if (entry is FileInfo file)
                        {
                            long length = file.Length;
                            count++;
                            largest.Enqueue(new LargeFile(file.FullName, length), length);
                            if (largest.Count > 20) largest.Dequeue();
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                    { skipped++; }
                    if (watch.ElapsedMilliseconds >= 250)
                    {
                        progress?.Invoke(count);
                        watch.Restart();
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { skipped++; }
        }
        return new ScanResult(largest.UnorderedItems.Select(x => x.Element)
            .OrderByDescending(x => x.Bytes).ThenBy(x => x.FullName).ToList(), count, skipped);
    }
}
