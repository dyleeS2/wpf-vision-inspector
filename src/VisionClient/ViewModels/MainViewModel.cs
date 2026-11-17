using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public ObservableCollection<string> Logs { get; } = new();

    public MainViewModel()
    {
        //_visionService = new MockVisionService();
        _visionService = new NativeVisionService();
        // Bgr24 포맷으로 WriteableBitmap 사전 할당 (재할당 방지)
        _displayImage = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr24, null);
    }

    [RelayCommand]
    private async Task GrabOnceAsync()
    {
        if (IsLiveRunning) return;
        var result = await _visionService.GrabAndInspectAsync(ImageWidth, ImageHeight);
        ApplyResultToUi(result);
    }

    [RelayCommand]
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
                var result = await _visionService.GrabAndInspectAsync(ImageWidth, ImageHeight, _liveCts.Token);
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

    private void ApplyResultToUi(InspectionResult result)
    {
        // WriteableBitmap은 UI 스레드에서만 Lock/Unlock 가능
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
