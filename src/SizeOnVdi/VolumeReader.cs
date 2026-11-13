using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SizeOnVdi;

public record VolumeStats(string Source, ulong Total, ulong Free);

public static class VolumeReader
{
    public static VolumeStats Read(string profile)
    {
        var target = "Profile-" + Environment.UserName;
        var name = new StringBuilder(1024);
        var handle = FindFirstVolume(name, name.Capacity);
        if (handle != new IntPtr(-1))
        {
            try
            {
                do
                {
                    var label = new StringBuilder(261);
                    if (GetVolumeInformation(name.ToString(), label, label.Capacity,
                        out _, out _, out _, null, 0) &&
                        string.Equals(label.ToString(), target, StringComparison.OrdinalIgnoreCase))
                        return Stats(name.ToString(), "Volume " + target);
                } while (FindNextVolume(handle, name, name.Capacity));
            }
            finally { FindVolumeClose(handle); }
        }
        var mount = new StringBuilder(1024);
        if (!GetVolumePathName(profile, mount, mount.Capacity)) throw new Win32Exception();
        return Stats(mount.ToString(), $"Volume del profilo: {mount} (etichetta {target} non trovata)");
    }

    private static VolumeStats Stats(string path, string source)
    {
        if (!GetDiskFreeSpaceEx(path, out _, out var total, out var free)) throw new Win32Exception();
        return new(source, total, free);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindFirstVolume(StringBuilder name, int length);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool FindNextVolume(IntPtr handle, StringBuilder name, int length);
    [DllImport("kernel32.dll")]
    private static extern bool FindVolumeClose(IntPtr handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformation(string root, StringBuilder label, int size,
        out uint serial, out uint maxLength, out uint flags, StringBuilder? filesystem, int filesystemSize);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumePathName(string file, StringBuilder mount, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetDiskFreeSpaceEx(string path, out ulong available, out ulong total, out ulong free);
}
