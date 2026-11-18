#pragma once
#include <cstdint>

#ifdef VISIONENGINE_EXPORTS
#define VISION_API __declspec(dllexport)
#else
#define VISION_API __declspec(dllimport)
#endif

extern "C" {

    // C# DefectInfo 구조체와 메모리 레이아웃 1:1 일치
    struct DefectInfo {
        int id;
        int x;
        int y;
        int width;
        int height;
        double area;
    };

    // 검사 결과 요약 구조체
    struct InspectionSummary {
        int isOk;               // 1: OK, 0: NG
        int defectCount;        // 검출된 결함 개수
        double processTimeMs;   // C++ 내부 순수 연산 시간
    };

    // 메인 검사 함수: C#이 할당한 버퍼와 구조체 배열을 채워서 반환
    VISION_API int InspectFrame(
        uint8_t* imageData,
        int width,
        int height,
        int stride,
        DefectInfo* outDefects,
        int maxDefects,
        InspectionSummary* outSummary
    );

    // Inspect the ROI and report defect boxes in full-frame coordinates.
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
        InspectionSummary* outSummary
    );
}