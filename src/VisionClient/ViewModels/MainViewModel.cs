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
    private CancellationTokenSource? _liveCts;

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

    [RelayCommand]
    private async Task GrabOnceAsync()
    {
        if (IsLiveRunning) return;
        var result = await GrabCurrentRoiAsync();
        ApplyResultToUi(result);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ToggleLiveAsync()
    {
        if (IsLiveRunning)
        {
            _liveCts?.Cancel();
            IsLiveRunning = false;
            return;
        }

        IsLiveRunning = true;
        _liveCts = new CancellationTokenSource();

        try
        {
            while (!_liveCts.Token.IsCancellationRequested)
            {
                var result = await GrabCurrentRoiAsync(_liveCts.Token);
                ApplyResultToUi(result);
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("Live inspection stopped.");
        }
        finally
        {
            IsLiveRunning = false;
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

    private void AddLog(string message)
    {
        Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
    }
}
