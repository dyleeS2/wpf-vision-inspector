using System.Runtime.InteropServices;

namespace VisionClient.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct DefectInfo
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Area { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct InspectionSummary
{
    public int IsOk { get; set; }
    public int DefectCount { get; set; }
    public double ProcessTimeMs { get; set; }
}

public static class VisionNative
{
    private const string DllName = "VisionEngine.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int InspectFrame(
        IntPtr imageData,
        int width,
        int height,
        int stride,
        [Out] DefectInfo[] outDefects,
        int maxDefects,
        out InspectionSummary outSummary
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int InspectFrameInRoi(
        IntPtr imageData,
        int width,
        int height,
        int stride,
        int roiX,
        int roiY,
        int roiWidth,
        int roiHeight,
        [Out] DefectInfo[] outDefects,
        int maxDefects,
        out InspectionSummary outSummary
    );
}
