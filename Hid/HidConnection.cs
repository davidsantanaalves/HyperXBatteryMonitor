using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using HyperXBatteryTray.Devices;

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

    public int InputReportByteLength { get; private set; }

    public int OutputReportByteLength { get; private set; }

    public int FeatureReportByteLength { get; private set; }

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

        HidCapabilities? capabilities = GetHidCapabilities(_readHandle);

        if (!capabilities.HasValue ||
            capabilities.Value.InputReportByteLength == 0 ||
            capabilities.Value.OutputReportByteLength == 0)
        {
            Close();
            throw new InvalidOperationException(
                "Não foi possível obter os tamanhos dos reports HID do dispositivo.");
        }

        InputReportByteLength = capabilities.Value.InputReportByteLength;
        OutputReportByteLength = capabilities.Value.OutputReportByteLength;
        FeatureReportByteLength = capabilities.Value.FeatureReportByteLength;

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

    public void Write(byte[] report)
    {
        ThrowIfDisposed();

        if (_writeStream == null || OutputReportByteLength <= 0)
        {
            throw new InvalidOperationException(
                "O dispositivo HID não está aberto.");
        }

        if (report.Length > OutputReportByteLength)
        {
            throw new InvalidOperationException(
                $"O comando HID possui {report.Length} bytes, mas o dispositivo " +
                $"aceita reports de saída de {OutputReportByteLength} bytes.");
        }

        byte[] outputReport;

        if (report.Length == OutputReportByteLength)
        {
            outputReport = report;
        }
        else
        {
            outputReport = new byte[OutputReportByteLength];
            Buffer.BlockCopy(report, 0, outputReport, 0, report.Length);
        }

        _writeStream.Write(
            outputReport,
            0,
            outputReport.Length);

        _writeStream.Flush();
    }

    public void SetFeatureReport(byte[] report)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(report);
        ValidateFeatureReportHandle();

        if (report.Length == 0 || report.Length > FeatureReportByteLength)
            throw new ArgumentException("Invalid HID feature report size.", nameof(report));

        byte[] buffer = new byte[FeatureReportByteLength];
        Buffer.BlockCopy(report, 0, buffer, 0, report.Length);
        if (!HidD_SetFeature(_writeHandle!, buffer, buffer.Length))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public byte[] GetFeatureReport(byte reportId)
    {
        ThrowIfDisposed();
        ValidateFeatureReportHandle();

        byte[] buffer = new byte[FeatureReportByteLength];
        buffer[0] = reportId;
        if (!HidD_GetFeature(_writeHandle!, buffer, buffer.Length))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return buffer;
    }

    private void ValidateFeatureReportHandle()
    {
        if (_writeHandle is not { IsInvalid: false, IsClosed: false } || FeatureReportByteLength <= 0)
            throw new InvalidOperationException("HID feature reports are unavailable.");
    }

    public async Task<byte[]?> ReadAsync(
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_readStream == null || InputReportByteLength <= 0)
        {
            throw new InvalidOperationException(
                "O dispositivo HID não está aberto.");
        }

        byte[] buffer = new byte[InputReportByteLength];

        try
        {
            int bytesRead =
                await _readStream.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken);

            if (bytesRead <= 0)
                return null;

            if (bytesRead == buffer.Length)
                return buffer;

            byte[] result = new byte[bytesRead];

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

    public static string? FindDevice(HyperXDeviceDefinition definition)
    {
        Guid hidGuid = HidClassGuid;

        IntPtr deviceInfoSet =
            SetupDiGetClassDevs(
                ref hidGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                DIGCF_PRESENT |
                DIGCF_DEVICEINTERFACE);

        if (deviceInfoSet == INVALID_HANDLE_VALUE)
            return null;

        try
        {
            uint index = 0;

            while (true)
            {
                SP_DEVICE_INTERFACE_DATA interfaceData =
                    new()
                    {
                        cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
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
                    int error = Marshal.GetLastWin32Error();

                    if (error == ERROR_NO_MORE_ITEMS)
                        break;

                    break;
                }

                string? path = GetDevicePath(
                    deviceInfoSet,
                    ref interfaceData);

                if (path != null && definition.Matches(path))
                    return path;

                index++;
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return null;
    }

    private static HidCapabilities? GetHidCapabilities(
        SafeFileHandle handle)
    {
        if (handle.IsInvalid)
            return null;

        if (!HidD_GetPreparsedData(
                handle,
                out IntPtr preparsedData) ||
            preparsedData == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            HIDP_CAPS capabilities = new()
            {
                Reserved = new ushort[17]
            };

            int status = HidP_GetCaps(
                preparsedData,
                ref capabilities);

            if (status != HIDP_STATUS_SUCCESS)
                return null;

            return new HidCapabilities(
                capabilities.InputReportByteLength,
                capabilities.OutputReportByteLength,
                capabilities.FeatureReportByteLength);
        }
        finally
        {
            _ = HidD_FreePreparsedData(preparsedData);
        }
    }

    private readonly record struct HidCapabilities(
        ushort InputReportByteLength,
        ushort OutputReportByteLength,
        ushort FeatureReportByteLength);

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
        InputReportByteLength = 0;
        OutputReportByteLength = 0;
        FeatureReportByteLength = 0;
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

    private const int HIDP_STATUS_SUCCESS = 0x00110000;

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_SetFeature(
        SafeFileHandle hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetFeature(
        SafeFileHandle hidDeviceObject, [In, Out] byte[] reportBuffer, int reportBufferLength);

    [DllImport(
        "hid.dll",
        SetLastError = true)]
    private static extern bool
        HidD_GetPreparsedData(
            SafeFileHandle HidDeviceObject,
            out IntPtr PreparsedData);

    [DllImport(
        "hid.dll",
        SetLastError = true)]
    private static extern bool
        HidD_FreePreparsedData(
            IntPtr PreparsedData);

    [DllImport(
        "hid.dll",
        SetLastError = false)]
    private static extern int
        HidP_GetCaps(
            IntPtr PreparsedData,
            ref HIDP_CAPS Capabilities);

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
    private struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;

        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }
}
