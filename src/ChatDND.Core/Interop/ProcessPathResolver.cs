using System.Runtime.InteropServices;
using System.Text;

namespace ChatDND.Core.Interop;

public static class ProcessPathResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static string? TryResolve(uint processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, (int)processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 32767u;
            var buffer = new StringBuilder((int)capacity);
            return QueryFullProcessImageName(handle, 0, buffer, ref capacity)
                ? buffer.ToString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint desiredAccess,
        bool inheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(
        IntPtr process,
        uint flags,
        StringBuilder executableName,
        ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
