using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VisionClient.Interop;
using VisionClient.Models;
using VisionClient.Services;

namespace VisionClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int ImageWidth = 1280;
    private const int ImageHeight = 1024;
    private readonly IVisionService _visionService;
    // 현재 Live 세션의 취소 소스와 루프 Task. 세션이 끝나면 루프 자신이 정리한다.
    private CancellationTokenSource? _liveCts;
    private Task? _liveTask;

    [ObservableProperty]
    private WriteableBitmap _displayImage;

    [ObservableProperty]
    private string _statusText = "READY";

    [ObservableProperty]
    private Brush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private string _cycleTimeText = "0.0 ms";

    [ObservableProperty]
    private bool _isLiveRunning;

    // [3단계 추가] 사용자 지정 ROI 및 검출된 결함 벡터 컬렉션
    [ObservableProperty]
    private RoiBox _inspectionRoi = new();

    public ObservableCollection<DefectInfo> Defects { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public MainViewModel()
    {
        _visionService = new NativeVisionService();
        _displayImage = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr24, null);
    }

    private bool IsLiveLoopActive => _liveTask is { IsCompleted: false };

    [RelayCommand]
    private async Task GrabOnceAsync()
    {
        if (IsLiveRunning || IsLiveLoopActive) return;
        try
        {
            var result = await GrabCurrentRoiAsync();
            ApplyResultToUi(result);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ToggleLiveAsync()
    {
        // 이전 루프가 아직 끝나지 않았다면(정지 진행 중 포함) 새 세션을 시작하지 않고
        // 정지를 요청한 뒤 루프 종료까지 기다린다.
        if (IsLiveLoopActive)
        {
            await StopLiveAsync();
            return;
        }

        var cts = new CancellationTokenSource();
        _liveCts = cts;
        IsLiveRunning = true;
        _liveTask = RunLiveLoopAsync(cts);
        await _liveTask;
    }

    private async Task StopLiveAsync()
    {
        var task = _liveTask;
        if (task is null) return;

        _liveCts?.Cancel();
        // 진행 중인 프레임까지 끝난 뒤 루프의 finally에서 상태가 초기화된다.
        await task;
    }

    private async Task RunLiveLoopAsync(CancellationTokenSource cts)
    {
        // 세션별 토큰을 지역 변수로 고정해 이후 세션의 _liveCts와 섞이지 않게 한다.
        var token = cts.Token;
        AddLog("Live inspection started.");
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await GrabCurrentRoiAsync(token);
                if (token.IsCancellationRequested) break;
                ApplyResultToUi(result);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 정상 정지
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            // 자기 세션일 때만 공유 상태를 초기화한다.
            if (ReferenceEquals(_liveCts, cts))
            {
                _liveCts = null;
                IsLiveRunning = false;
            }
            cts.Dispose();
            AddLog("Live inspection stopped.");
        }
    }

    private Task<InspectionResult> GrabCurrentRoiAsync(CancellationToken ct = default)
    {
        var roi = InspectionRoi;
        // Pixel coordinates inside [left, right) and [top, bottom).
        int left = (int)Math.Ceiling(roi.X);
        int top = (int)Math.Ceiling(roi.Y);
        int right = (int)Math.Ceiling(roi.X + roi.Width);
        int bottom = (int)Math.Ceiling(roi.Y + roi.Height);
        var bounds = new Int32Rect(left, top, right - left, bottom - top);
        return _visionService.GrabAndInspectAsync(ImageWidth, ImageHeight, ct, bounds);
    }

    private void ApplyResultToUi(InspectionResult result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            DisplayImage.Lock();
            try
            {
                unsafe
                {
                    fixed (byte* pSrc = result.ImageBuffer)
                    {
                        long destSize = DisplayImage.BackBufferStride * DisplayImage.PixelHeight;
                        Buffer.MemoryCopy(pSrc, (void*)DisplayImage.BackBuffer, destSize, result.ImageBuffer.Length);
                    }
                }
                DisplayImage.AddDirtyRect(new Int32Rect(0, 0, result.Width, result.Height));
            }
            finally
            {
                DisplayImage.Unlock();
            }

            // [3단계 추가] 결함 벡터 오버레이 갱신
            Defects.Clear();
            foreach (var defect in result.Defects)
            {
                Defects.Add(defect);
            }

            StatusText = result.IsOk ? "OK" : "NG";
            StatusColor = result.IsOk ? Brushes.LimeGreen : Brushes.Crimson;
            CycleTimeText = $"{result.ProcessTimeMs:F1} ms";
            AddLog(result.Message);
        });
    }

    private void ReportError(Exception ex)
    {
        StatusText = "ERROR";
        StatusColor = Brushes.OrangeRed;
        AddLog($"Error: {ex.GetType().Name} - {ex.Message}");
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
    }
}
