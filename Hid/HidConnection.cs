using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HyperXBatteryTray.Hid;

public sealed class HidConnection : IDisposable
{
    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;

    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;

    private const uint OPEN_EXISTING = 3;

    private const int ERROR_NO_MORE_ITEMS = 259;

    private static readonly Guid HidClassGuid =
        new("4D1E55B2-F16F-11CF-88CB-001111000030");

    private SafeFileHandle? _writeHandle;
    private SafeFileHandle? _readHandle;

    private FileStream? _writeStream;
    private FileStream? _readStream;

    private bool _disposed;

    public string? DevicePath { get; private set; }

    public bool IsOpen =>
        _writeHandle is { IsInvalid: false } &&
        _readHandle is { IsInvalid: false };

    public bool Open(string devicePath)
    {
        ThrowIfDisposed();

        Close();

        _writeHandle = CreateFile(
            devicePath,
            GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (_writeHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();

            _writeHandle.Dispose();
            _writeHandle = null;

            throw new Win32Exception(
                error,
                $"Não foi possível abrir o HID para escrita. " +
                $"Erro {error}.");
        }

        _readHandle = CreateFile(
            devicePath,
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (_readHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();

            Close();

            throw new Win32Exception(
                error,
                $"Não foi possível abrir o HID para leitura. " +
                $"Erro {error}.");
        }

        _writeStream = new FileStream(
            _writeHandle,
            FileAccess.Write,
            4096,
            isAsync: false);

        _readStream = new FileStream(
            _readHandle,
            FileAccess.Read,
            4096,
            isAsync: false);

        DevicePath = devicePath;

        return true;
    }

    public void Write(
        byte[] report)
    {
        ThrowIfDisposed();

        if (_writeStream == null)
            throw new InvalidOperationException(
                "O dispositivo HID não está aberto.");

        _writeStream.Write(
            report,
            0,
            report.Length);

        _writeStream.Flush();
    }

    public async Task<byte[]?> ReadAsync(
        int reportLength,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_readStream == null)
            throw new InvalidOperationException(
                "O dispositivo HID não está aberto.");

        byte[] buffer =
            new byte[reportLength];

        try
        {
            int bytesRead =
                await _readStream.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length),
                    cancellationToken);

            if (bytesRead <= 0)
                return null;

            if (bytesRead == buffer.Length)
                return buffer;

            byte[] result =
                new byte[bytesRead];

            Buffer.BlockCopy(
                buffer,
                0,
                result,
                0,
                bytesRead);

            return result;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
        catch (IOException)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            throw;
        }
    }

    public static string? FindDevice(
        string interfacePattern)
    {
        Guid hidGuid = HidClassGuid;

        IntPtr deviceInfoSet =
            SetupDiGetClassDevs(
                ref hidGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                DIGCF_PRESENT |
                DIGCF_DEVICEINTERFACE);

        if (deviceInfoSet ==
            INVALID_HANDLE_VALUE)
        {
            return null;
        }

        try
        {
            uint index = 0;

            while (true)
            {
                SP_DEVICE_INTERFACE_DATA interfaceData =
                    new()
                    {
                        cbSize =
                            Marshal.SizeOf<
                                SP_DEVICE_INTERFACE_DATA>()
                    };

                bool result =
                    SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref interfaceData);

                if (!result)
                {
                    int error =
                        Marshal.GetLastWin32Error();

                    if (error ==
                        ERROR_NO_MORE_ITEMS)
                    {
                        break;
                    }

                    break;
                }

                string? path =
                    GetDevicePath(
                        deviceInfoSet,
                        ref interfaceData);

                if (path != null &&
                    path.Contains(
                        interfacePattern,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }

                index++;
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(
                deviceInfoSet);
        }

        return null;
    }

    private static string? GetDevicePath(
        IntPtr deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA interfaceData)
    {
        uint requiredSize = 0;

        SetupDiGetDeviceInterfaceDetail(
            deviceInfoSet,
            ref interfaceData,
            IntPtr.Zero,
            0,
            ref requiredSize,
            IntPtr.Zero);

        if (requiredSize == 0)
            return null;

        IntPtr detailBuffer =
            Marshal.AllocHGlobal(
                (int)requiredSize);

        try
        {
            Marshal.WriteInt32(
                detailBuffer,
                0,
                8);

            bool result =
                SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    detailBuffer,
                    requiredSize,
                    ref requiredSize,
                    IntPtr.Zero);

            if (!result)
                return null;

            string? path =
                Marshal.PtrToStringUni(
                    IntPtr.Add(
                        detailBuffer,
                        8));

            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Normalização defensiva do namespace Win32.
            if (path.StartsWith(
                    @"?\",
                    StringComparison.Ordinal))
            {
                path = @"\" + path;
            }

            if (path.StartsWith(
                    @"\?\",
                    StringComparison.Ordinal))
            {
                path = @"\" + path;
            }

            return path;
        }
        finally
        {
            Marshal.FreeHGlobal(
                detailBuffer);
        }
    }

    public void Close()
    {
        _writeStream?.Dispose();
        _writeStream = null;

        _readStream?.Dispose();
        _readStream = null;

        _writeHandle?.Dispose();
        _writeHandle = null;

        _readHandle?.Dispose();
        _readHandle = null;

        DevicePath = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Close();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private static readonly IntPtr
        INVALID_HANDLE_VALUE = new(-1);

    [DllImport(
        "setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern IntPtr
        SetupDiGetClassDevs(
            ref Guid ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            uint Flags);

    [DllImport(
        "setupapi.dll",
        SetLastError = true)]
    private static extern bool
        SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA
                DeviceInterfaceData);

    [DllImport(
        "setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern bool
        SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA
                DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            ref uint RequiredSize,
            IntPtr DeviceInfoData);

    [DllImport(
        "setupapi.dll",
        SetLastError = true)]
    private static extern bool
        SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet);

    [DllImport(
        "kernel32.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle
        CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }
}