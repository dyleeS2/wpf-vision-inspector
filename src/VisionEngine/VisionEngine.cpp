#include "pch.h"
#include "VisionEngine.hpp"
#include <opencv2/opencv.hpp>
#include <chrono>

extern "C" {

    VISION_API int InspectFrame(
        uint8_t* imageData,
        int width,
        int height,
        int stride,
        DefectInfo* outDefects,
        int maxDefects,
        InspectionSummary* outSummary)
    {
        return InspectFrameInRoi(imageData, width, height, stride, 0, 0, width, height,
            outDefects, maxDefects, outSummary);
    }

    VISION_API int InspectFrameInRoi(
        uint8_t* imageData,
        int width,
        int height,
        int stride,
        int roiX,
        int roiY,
        int roiWidth,
        int roiHeight,
        DefectInfo* outDefects,
        int maxDefects,
        InspectionSummary* outSummary)
    {
        if (!imageData || !outDefects || !outSummary) return -1;

        if (width <= 0 || height <= 0 || stride / 3 < width ||
            roiX < 0 || roiY < 0 || roiWidth <= 0 || roiHeight <= 0 ||
            roiX > width - roiWidth || roiY > height - roiHeight) return -2;

        auto start = std::chrono::high_resolution_clock::now();

        // 1. C# 버퍼 포인터를 복사 없이 cv::Mat으로 래핑 (Bgr24 -> CV_8UC3)
        cv::Mat frame(height, width, CV_8UC3, imageData, stride);

        // 2. 영상 처리 시뮬레이션: 그레이스케일 변환 및 이진화(Threshold)로 어두운 이물 검출
        cv::Mat gray, binary;
        cv::cvtColor(frame(cv::Rect(roiX, roiY, roiWidth, roiHeight)), gray, cv::COLOR_BGR2GRAY);
        cv::threshold(gray, binary, 60, 255, cv::THRESH_BINARY_INV);

        std::vector<std::vector<cv::Point>> contours;
        cv::findContours(binary, contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

        int count = 0;
        for (const auto& contour : contours) {
            double area = cv::contourArea(contour);
            if (area < 500.0) continue; // 노이즈 필터링

            if (count < maxDefects) {
                cv::Rect box = cv::boundingRect(contour);
                outDefects[count] = DefectInfo{
                    count + 1,
                    box.x + roiX,
                    box.y + roiY,
                    box.width,
                    box.height,
                    area
                };

                // [3단계 변경] 원본 이미지 훼손 방지를 위해 C++ 직접 드로잉 제거 (WPF Canvas로 이관)
                //cv::rectangle(frame, box, cv::Scalar(0, 0, 255), 3);
                count++;
            }
        }

        auto end = std::chrono::high_resolution_clock::now();
        double elapsedMs = std::chrono::duration<double, std::milli>(end - start).count();

        outSummary->defectCount = count;
        outSummary->isOk = (count == 0) ? 1 : 0;
        outSummary->processTimeMs = elapsedMs;

        return 0; // 성공 코드
    }
}