using System.ComponentModel;
using System.Runtime.InteropServices;
using Topshelf;
using Topshelf.HostConfigurators;
using Topshelf.Runtime;
using static Topshelf.Runtime.Windows.NativeMethods;

namespace Webber.Server;

public static class TopshelfExtensions
{
    private const int SERVICE_NO_CHANGE = -1;

    [DllImport("Advapi32.dll", EntryPoint = "ChangeServiceConfigW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern bool ChangeServiceConfig(
        SafeHandle hService,
        int dwServiceType,
        int dwStartType,
        int dwErrorControl,
        [In] string lpBinaryPathName,
        [In] string lpLoadOrderGroup,
        IntPtr lpdwTagId,
        [In] string lpDependencies,
        [In] string lpServiceStartName,
        [In] string lpPassWord,
        [In] string lpDisplayName
    );

    [StructLayout(LayoutKind.Sequential)]
    public struct QUERY_SERVICE_CONFIG
    {
        [MarshalAs(UnmanagedType.U4)] public uint dwBytesNeeded;
        [MarshalAs(UnmanagedType.U4)] public uint dwServiceType;
        [MarshalAs(UnmanagedType.U4)] public uint dwStartType;
        [MarshalAs(UnmanagedType.U4)] public uint dwErrorControl;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpBinaryPathName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpLoadOrderGroup;
        [MarshalAs(UnmanagedType.U4)] public uint dwTagID;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpDependencies;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpServiceStartName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpDisplayName;
    };

    [DllImport("Advapi32.dll", EntryPoint = "QueryServiceConfigW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern bool QueryServiceConfig(
        SafeHandle hService,
        IntPtr lpServiceConfig,
        uint cbBufSize,
        out uint pcbBytesNeeded);

    private static List<(string Name, string Value)> _args = new List<(string Name, string Value)>();
    private static bool _registered = false;
    private static object _lock = new object();

    public static void AddPersistedCommandLineArgument(this HostConfigurator x, string name, Action<string> callback)
    {
        x.AddCommandLineDefinition(name, v =>
        {
            lock (_lock)
            {
                // only register the install callback once
                if (!_registered)
                {
                    x.AfterInstall(HandleAfterInstall);
                    _registered = true;
                }

                // add this arg to be installed
                _args.Add((name, v));

                // call the original callback
                callback(v);
            }
        });
    }

    private static void HandleAfterInstall(InstallHostSettings s)
    {
        if (!_args.Any())
            return;

        using var scmHandle = OpenSCManager(null, null, (int)SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
        if (scmHandle.IsInvalid)
            throw new Win32Exception();

        using var serviceHandle = OpenService(scmHandle, s.ServiceName, (int)SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
        if (serviceHandle.IsInvalid)
            throw new Win32Exception();

        // allocate enough space to hold the service config
        QueryServiceConfig(serviceHandle, IntPtr.Zero, 0, out var sizeNeeded);
        if (sizeNeeded == 0)
            throw new Win32Exception();

        var hGlobal = Marshal.AllocHGlobal((int)sizeNeeded);
        try
        {
            // retrieve existing config
            if (!QueryServiceConfig(serviceHandle, hGlobal, sizeNeeded, out sizeNeeded))
                throw new Win32Exception();

            var conf = Marshal.PtrToStructure<QUERY_SERVICE_CONFIG>(hGlobal);

            // add our persisted command line options to the existing image path
            var args = String.Join(" ", _args.Select(a => $"-{a.Name} \"{a.Value}\""));
            string newImagePath = conf.lpBinaryPathName + " " + args;

            // persist the new image path
            if (!ChangeServiceConfig(serviceHandle,
                SERVICE_NO_CHANGE,
                SERVICE_NO_CHANGE,
                SERVICE_NO_CHANGE,
                newImagePath,
                null, IntPtr.Zero,
                null, null, null, null))
                throw new Win32Exception();
        }
        finally
        {
            Marshal.FreeHGlobal(hGlobal);
        }
    }
}
