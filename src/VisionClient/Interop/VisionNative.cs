using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VisionClient.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct DefectInfo
{
    public int Id;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public double Area;
}

[StructLayout(LayoutKind.Sequential)]
public struct InspectionSummary
{
    public int IsOk;
    public int DefectCount;
    public double ProcessTimeMs;
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
}
