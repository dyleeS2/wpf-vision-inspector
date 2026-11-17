using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VisionClient.Models;

namespace VisionClient.Services;

public class MockVisionService : IVisionService
{
    private int _frameCounter;

    public async Task<InspectionResult> GrabAndInspectAsync(int width, int height, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 백그라운드 스레드에서 영상 획득 및 알고리즘 연산 시뮬레이션
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            int stride = width * 3; // Bgr24 (3 bytes per pixel)
            byte[] buffer = new byte[stride * height];
            int offset = Interlocked.Increment(ref _frameCounter) * 4;

            // 테스트용 그라데이션 패턴 채우기 (C++ cv::Mat 데이터 대체)
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int idx = rowStart + (x * 3);
                    buffer[idx] = (byte)((x + offset) % 256); // B
                    buffer[idx + 1] = (byte)((y + offset) % 256); // G
                    buffer[idx + 2] = 40;                         // R
                }
            }

            Thread.Sleep(15); // 가상 검사 연산 시간 (15ms)
            sw.Stop();

            bool isOk = (_frameCounter % 10) != 0; // 10프레임마다 1번 NG 시뮬레이션
            string msg = isOk ? $"Frame #{_frameCounter}: PASS" : $"Frame #{_frameCounter}: DEFECT DETECTED";

            return new InspectionResult(buffer, width, height, isOk, sw.Elapsed.TotalMilliseconds, msg);
        }, ct);
    }
}
