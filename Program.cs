using SizeOnVdi;

var root = Path.Combine(Path.GetTempPath(), "SizeOnVdi-" + Guid.NewGuid());
Directory.CreateDirectory(root);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
try
{
    Check(Scanner.Scan(root, default).Files.Count == 0, "Empty folder");
    var nested = Directory.CreateDirectory(Path.Combine(root, "nested")).FullName;
    for (int i = 1; i <= 30; i++) File.WriteAllBytes(Path.Combine(nested, $"file-{i}"), new byte[i * 100]);
    var hidden = Path.Combine(root, "hidden");
    File.WriteAllBytes(hidden, new byte[5000]);
    if (OperatingSystem.IsWindows()) File.SetAttributes(hidden, FileAttributes.Hidden);
    var result = Scanner.Scan(root, default);
    Check(result.Count == 31, "Recursive scan and hidden files");
    Check(result.Files.Count == 20 && result.Files[0].Bytes == 5000 && result.Files[^1].Bytes == 1200, "Top 20 order");
    Check(new LargeFile("x", 1048576).MB == 1, "PowerShell MB conversion");
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    try { Scanner.Scan(root, canceled.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { }
    try { Scanner.Scan(Path.Combine(root, "missing"), default); throw new Exception("Missing folder accepted"); }
    catch (DirectoryNotFoundException) { }
    if (OperatingSystem.IsWindows())
    {
        var volume = VolumeReader.Read(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Check(volume.Total > 0 && volume.Free <= volume.Total, "Windows volume capacity");
    }
    Console.WriteLine("All scanner checks passed.");
}
finally { Directory.Delete(root, true); }
