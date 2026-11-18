using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VisionClient.Interop;
using VisionClient.Models;

namespace VisionClient.Services;

public class NativeVisionService : IVisionService
{
    private int _frameCounter;
    private const int MaxDefects = 50;

    public async Task<InspectionResult> GrabAndInspectAsync(int width, int height, CancellationToken ct = default,
        Int32Rect? roi = null)
    {
        var bounds = roi ?? new Int32Rect(0, 0, width, height);
        if (bounds.X < 0 || bounds.Y < 0 || bounds.Width <= 0 || bounds.Height <= 0 ||
            bounds.X > width - bounds.Width || bounds.Y > height - bounds.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(roi), "The inspection ROI must be within the frame.");
        }

        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            int stride = width * 3;
            byte[] buffer = new byte[stride * height];
            int frameIdx = Interlocked.Increment(ref _frameCounter);

            // 1. 밝은 배경 이미지 생성 (Grab 시뮬레이션)
            Array.Fill(buffer, (byte)220);

            // 2. 5프레임마다 어두운 사각형 이물(가짜 결함)을 버퍼에 삽입
            if (frameIdx % 5 == 0)
            {
                int defectX = (frameIdx * 37) % (width - 150) + 50;
                int defectY = (frameIdx * 23) % (height - 150) + 50;
                DrawSimulatedSpot(buffer, stride, defectX, defectY, 80, 80);
            }

            var defects = new DefectInfo[MaxDefects];
            InspectionSummary summary;
            int statusCode;

            // 3. GC가 버퍼 주소를 옮기지 못하도록 고정(fixed) 후 C++ DLL 포인터 전달
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    statusCode = VisionNative.InspectFrameInRoi(
                        (IntPtr)pBuffer,
                        width,
                        height,
                        stride,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height,
                        defects,
                        MaxDefects,
                        out summary
                    );
                }
            }

            if (statusCode != 0)
                throw new InvalidOperationException($"VisionEngine DLL Error: {statusCode}");

            bool isOk = summary.IsOk == 1;
            var detectedList = defects.Take(summary.DefectCount).ToList();
            string msg = isOk
                ? $"Frame #{frameIdx}: OK ({summary.ProcessTimeMs:F2} ms)"
                : $"Frame #{frameIdx}: NG - {summary.DefectCount} defect(s) at ({detectedList[0].X}, {detectedList[0].Y})";

            return new InspectionResult(buffer, width, height, isOk, summary.ProcessTimeMs, msg, detectedList);
        }, ct);
    }

    private static void DrawSimulatedSpot(byte[] buffer, int stride, int startX, int startY, int w, int h)
    {
        for (int y = startY; y < startY + h; y++)
        {
            int row = y * stride;
            for (int x = startX; x < startX + w; x++)
            {
                int idx = row + (x * 3);
                buffer[idx] = 10;     // B
                buffer[idx + 1] = 10; // G
                buffer[idx + 2] = 10; // R
            }
        }
    }
}
