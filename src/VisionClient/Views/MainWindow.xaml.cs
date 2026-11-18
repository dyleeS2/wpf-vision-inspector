using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VisionClient.ViewModels;

namespace VisionClient.Views;

public partial class MainWindow : Window
{
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panOriginX;
    private double _panOriginY;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FitImageToViewport();
    }

    private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
    {
        FitImageToViewport();
    }

    private void FitImageToViewport()
    {
        if (ViewerViewport.ActualWidth <= 0 || ViewerViewport.ActualHeight <= 0) return;

        double scaleX = ViewerViewport.ActualWidth / ImageCanvasContainer.Width;
        double scaleY = ViewerViewport.ActualHeight / ImageCanvasContainer.Height;
        double fitScale = Math.Min(scaleX, scaleY);

        ImgScaleTransform.ScaleX = fitScale;
        ImgScaleTransform.ScaleY = fitScale;

        ImgTranslateTransform.X = (ViewerViewport.ActualWidth - (ImageCanvasContainer.Width * fitScale)) / 2.0;
        ImgTranslateTransform.Y = (ViewerViewport.ActualHeight - (ImageCanvasContainer.Height * fitScale)) / 2.0;
    }

    // 마우스 커서 위치 중심 휠 줌(Zoom)
    private void ViewerViewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point cursorPos = e.GetPosition(ViewerViewport);
        double zoomFactor = e.Delta > 0 ? 1.2 : (1.0 / 1.2);

        double oldScale = ImgScaleTransform.ScaleX;
        double newScale = Math.Clamp(oldScale * zoomFactor, 0.1, 20.0);
        double ratio = newScale / oldScale;

        // 커서 아래 이미지 픽셀이 화면상 같은 위치에 고정되도록 평행이동 보정
        ImgTranslateTransform.X = cursorPos.X - (cursorPos.X - ImgTranslateTransform.X) * ratio;
        ImgTranslateTransform.Y = cursorPos.Y - (cursorPos.Y - ImgTranslateTransform.Y) * ratio;

        ImgScaleTransform.ScaleX = newScale;
        ImgScaleTransform.ScaleY = newScale;
    }

    // 마우스 우클릭 드래그 팬(Pan)
    private void ViewerViewport_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panStartPoint = e.GetPosition(ViewerViewport);
        _panOriginX = ImgTranslateTransform.X;
        _panOriginY = ImgTranslateTransform.Y;
        ViewerViewport.CaptureMouse();
    }

    private void ViewerViewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;

        Point currentPos = e.GetPosition(ViewerViewport);
        ImgTranslateTransform.X = _panOriginX + (currentPos.X - _panStartPoint.X);
        ImgTranslateTransform.Y = _panOriginY + (currentPos.Y - _panStartPoint.Y);
    }

    private void ViewerViewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        ViewerViewport.ReleaseMouseCapture();
    }

    // 노란색 ROI 박스 드래그 이동 (이미지 경계 클램핑 포함)
    private void RoiThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        double nextX = vm.InspectionRoi.X + e.HorizontalChange;
        double nextY = vm.InspectionRoi.Y + e.VerticalChange;

        vm.InspectionRoi.X = Math.Clamp(nextX, 0, ImageCanvasContainer.Width - vm.InspectionRoi.Width);
        vm.InspectionRoi.Y = Math.Clamp(nextY, 0, ImageCanvasContainer.Height - vm.InspectionRoi.Height);
    }
}