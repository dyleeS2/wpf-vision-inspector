# 🔍 WPF Vision Inspector

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF%20(MVVM)-0078D4?style=flat-square)
![C++](https://img.shields.io/badge/Engine-C%2B%2B%20%2F%20OpenCV%204.7.0-00599C?style=flat-square&logo=cplusplus)

WPF Vision Inspector는 합성 영상으로 산업용 비전 검사의 기본 흐름을 실행하는 Windows 데모입니다. C# WPF 클라이언트가 프레임을 생성하고 C++ OpenCV DLL로 검사한 뒤 영상, OK/NG 판정, 결함 위치를 표시합니다. 기본 실행에는 카메라나 PLC가 필요하지 않습니다.

## 현재 구현

```text
NativeVisionService에서 Bgr24 합성 프레임 생성
  → P/Invoke로 VisionEngine.dll의 InspectFrameInRoi 호출
  → 검사 결과를 받아 WPF WriteableBitmap과 Canvas 갱신
```

| 영역 | 동작 |
| --- | --- |
| MVVM과 검사 실행 | `CommunityToolkit.Mvvm` 8.4.2로 상태와 명령을 바인딩합니다. `Single Grab`은 한 프레임을 처리하고, `Start Live`는 프레임 생성과 검사를 순차 반복합니다. `Stop Live`는 반복 취소를 요청합니다. |
| 합성 영상과 네이티브 검사 | 기본 서비스인 `NativeVisionService`가 백그라운드에서 1280×1024 Bgr24 프레임을 만들고 매 5번째 프레임에 어두운 80×80 사각형을 삽입합니다. `fixed`로 고정한 영상 버퍼와 ROI 좌표를 P/Invoke로 `VisionEngine.dll`의 `InspectFrameInRoi`에 전달합니다. |
| OpenCV 판정 | ROI 내부에 회색조 변환과 역이진화를 적용해 외곽선을 찾습니다. 면적 500픽셀² 이상인 외곽선의 바운딩 박스를 최대 50개 반환하며, 반환된 결함이 있으면 NG입니다. 결함 좌표는 원본 영상 기준으로 반환합니다. |
| WPF 표시 | UI 스레드에서 `Buffer.MemoryCopy`로 재사용하는 `WriteableBitmap.BackBuffer`를 갱신합니다. 화면에는 OK/NG, 네이티브 검사 시간, 결함 바운딩 박스와 최근 100건의 로그를 표시합니다. 프레임용 관리 배열은 매 검사에서 새로 할당합니다. |
| 뷰어와 ROI | 마우스 휠로 커서 중심 확대·축소, 마우스 오른쪽 버튼 드래그로 이동, `Fit View (Reset Zoom)`으로 화면 맞춤을 실행합니다. 노란 ROI는 이미지 경계 안에서 이동할 수 있으며 크기는 고정입니다. 단일·연속 검사는 프레임마다 검사 시작 시점의 ROI를 사용합니다. |

`MockVisionService`도 소스에 있으며 기본 실행은 `NativeVisionService`를 사용합니다.

### 남은 로드맵

- [ ] 실제 카메라 영상 획득과 Modbus TCP 기반 PLC 트리거 수신·결과 송신
- [ ] `Channel<T>` 기반 획득·검사 큐와 `ArrayPool<T>` 기반 프레임 버퍼 재사용
- [ ] NG 이미지 비동기 파일 저장

## 프로젝트 구조

```text
src/
├── VisionClient/
│   ├── VisionClient.sln         # WPF와 C++ 프로젝트를 함께 여는 솔루션
│   ├── VisionClient.csproj      # net8.0-windows, WPF, CommunityToolkit.Mvvm
│   ├── Interop/                 # 네이티브 검사 P/Invoke, 결과 구조체
│   ├── Models/                  # 검사 결과와 ROI 좌표
│   ├── Services/                # 검사 인터페이스, 네이티브 및 모의 서비스
│   ├── ViewModels/              # 검사 명령, 프레임 갱신, 판정과 로그
│   └── Views/                   # 메인 화면, 뷰어 조작, 오버레이
└── VisionEngine/
    ├── VisionEngine.vcxproj     # MSVC v143, OpenCV 링크 및 DLL 복사 설정
    ├── VisionEngine.hpp         # C ABI 및 결과 구조체
    └── VisionEngine.cpp         # OpenCV 검사 구현
```

## 개발 및 실행

- Windows x64와 Visual Studio 2022가 필요합니다. WPF용 **.NET 데스크톱 개발**과 네이티브 DLL용 **C++를 사용한 데스크톱 개발** 워크로드를 설치합니다. 프로젝트는 .NET 8 대상 팩 및 데스크톱 런타임, MSVC v143, Windows 10 SDK 계열을 사용합니다.
- [VisionEngine.vcxproj](src/VisionEngine/VisionEngine.vcxproj)는 OpenCV 4.7.0 x64의 `build` 폴더를 `C:\Visual\3rd_party\opencv\build`로 지정합니다. 다른 경로에 설치했다면 프로젝트의 추가 포함 디렉터리, 추가 라이브러리 디렉터리, 빌드 후 DLL 복사 경로를 맞춥니다. `Debug | x64`는 `x64\vc16\lib\opencv_world470d.lib`에 링크하고 `x64\vc16\bin\opencv_world470d.dll`을 출력 폴더로 복사합니다.

1. [VisionClient.sln](src/VisionClient/VisionClient.sln)을 열고 솔루션 구성을 `Debug | x64`로 선택합니다.
2. **빌드 → 구성 관리자**에서 `VisionClient`와 `VisionEngine`의 **빌드**를 모두 체크합니다. 저장소의 `Debug | x64` 솔루션 설정에는 `VisionClient`의 빌드 체크가 빠져 있습니다. `Release | x64` 솔루션 구성도 현재 두 프로젝트의 `Debug | x64` 구성에 매핑됩니다.
3. 솔루션을 빌드합니다. `src/VisionClient/bin/x64/Debug/net8.0-windows/`에 `VisionClient.exe`, `VisionEngine.dll`, `opencv_world470d.dll`이 함께 있어야 합니다.
4. `VisionClient`를 시작 프로젝트로 선택해 `F5`로 실행합니다. 기본 ROI에서 `Single Grab`을 다섯 번 완료하면 5번째 프레임의 NG 판정과 결함 박스를 확인할 수 있습니다. 앱을 다시 실행해 ROI를 오른쪽 아래 끝으로 옮긴 후 같은 절차를 반복하면 5번째 프레임에서 어두운 사각형은 보이고 판정은 OK입니다. 연속 검사는 `Start Live`와 `Stop Live`로 조작합니다.
