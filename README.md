# 🔍 WPF Vision Inspector

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp)
![WPF](https://img.shields.io/badge/UI-WPF%20(MVVM)-0078D4?style=flat-square)
![C++](https://img.shields.io/badge/Engine-C%2B%2B17%20%2F%20OpenCV-00599C?style=flat-square&logo=cplusplus)

> **C# WPF 비전 클라이언트 핵심 아키텍처(MVVM, 렌더링, P/Invoke, 통신) 단계별 구현 프로젝트**

---

## 1. 프로젝트 개요 (Project Overview)

**WPF Vision Inspector**는 산업용 자동화(FA) 비전 검사 클라이언트를 구성하는 4가지 핵심 엔지니어링 영역(**MVVM 아키텍처, 고속 영상 렌더링, C++ 네이티브 엔진 연동(P/Invoke), 설비 통신**)을 탑다운(Top-Down) 방식으로 단계별 구현·검증하는 프로젝트입니다.

단순한 기능 나열이 아니라, 실제 현장 라인의 고속 검사 환경에서 요구되는 **UI 응답성 확보**, **GC(Garbage Collection) 부하 억제**, **언매니지드 메모리 안전성**, **비동기 생산자-소비자 파이프라인** 설계를 코드 수준에서 검증하는 것을 목표로 합니다.

### 핵심 데이터 파이프라인

```text
[Camera / Trigger] ──▶ [Channel<T> Bounded Queue] ──▶ [C++ OpenCV DLL (P/Invoke)]
                                                                 │
[PLC (Modbus TCP)] ◀── [OK/NG 판정 및 로그 저장] ◀── [WriteableBitmap (BackBuffer Copy)]
```

---

## 2. 4대 핵심 아키텍처 및 기술 스택 (Architecture & Tech Stack)

| 핵심 영역 | 적용 기술 및 라이브러리 | 설계 목적 및 구현 포인트 |
| :--- | :--- | :--- |
| **1. MVVM 아키텍처** | C# 12, .NET 8, WPF<br>`CommunityToolkit.Mvvm` | Source Generator(`[ObservableProperty]`, `[RelayCommand]`)로 보일러플레이트를 최소화하고 View와 검사 로직 계층 분리 |
| **2. 고속 영상 렌더링** | `WriteableBitmap`<br>`Buffer.MemoryCopy` | 매 프레임 객체 생성을 피하고 사전 할당된 `BackBuffer` 포인터에 직접 복사하여 LOH 단편화 방지 및 30fps+ 실시간 갱신 |
| **3. P/Invoke 네이티브 연동** | C++17, OpenCV 4.x<br>`[DllImport]`, `IntPtr` | C# 관리 힙 버퍼 주소를 `fixed`로 고정해 C++ 알고리즘 DLL에 전달하고 결함 데이터 구조체를 저지연 마샬링 |
| **4. 비동기 설비 통신** | `System.Threading.Channels`<br>`NModbus` (Modbus TCP) | Grab·검사·UI 스레드 격리를 위한 유한 큐(`Channel<T>`) 파이프라인 및 PLC 트리거 수신·판정 송신 |

---

## 3. 단계별 구현 로드맵 (Implementation Roadmap)

동작하는 End-to-End 뼈대를 먼저 구축한 뒤, 핵심 아키텍처 모듈을 주차별로 고도화합니다.

- [x] **1단계 (MVVM & 렌더링 기초): End-to-End 뼈대 및 고속 뷰어 프로토타입 구축**
  - `Models`, `Services`, `ViewModels`, `Views` 계층 분리 및 `CommunityToolkit.Mvvm` 적용
  - `WriteableBitmap.BackBuffer` 직접 메모리 복사 기반의 실시간 라이브 뷰 루프 구현
- [ ] **2단계 (P/Invoke 연동): C++ OpenCV 알고리즘 DLL 구축 및 네이티브 마샬링**
  - C++ 영상처리 라이브러리(`VisionEngine.dll`) 빌드 및 `extern "C"` 인터페이스 익스포트
  - 가상 검사 서비스(`MockVisionService`)를 실제 C++ DLL 호출 서비스로 교체 및 메모리 누수 검증
- [ ] **3단계 (렌더링 & 인터랙션 고도화): 줌/팬 뷰어 및 ROI·결함 오버레이 구현**
  - `ScaleTransform` / `TranslateTransform` 기반 마우스 커서 중심 휠 줌(Zoom) 및 팬(Pan) 구현
  - `Canvas` 오버레이를 통한 검사 영역(ROI) 편집 및 NG 바운딩 박스 동적 바인딩
- [ ] **4단계 (통신 & 안정화): Modbus TCP 설비 통신 결합 및 무중단 파이프라인 완성**
  - `System.Threading.Channels` 기반 비동기 Grab-Inspect 큐 및 `ArrayPool<T>` 버퍼 재사용 적용
  - `NModbus`를 활용한 PLC 트리거 수신·결과 송신 및 NG 이미지 비동기 파일 저장

---

## 4. 프로젝트 폴더 구조 (Directory Structure)

```text
wpf-vision-inspector/
├── src/
│   ├── VisionClient/                 # C# WPF (.NET 8) 클라이언트 애플리케이션
│   │   ├── Interop/                  # C++ DLL P/Invoke 선언 및 구조체 매핑
│   │   ├── Models/                   # 검사 결과(InspectionResult), ROI 데이터 모델
│   │   ├── Services/                 # 카메라, 비전 검사, PLC 통신 서비스 계층
│   │   ├── ViewModels/               # UI 상태 제어 및 커맨드 바인딩
│   │   └── Views/                    # XAML 뷰 및 커스텀 뷰어 컨트롤
│   │
│   └── VisionEngine/                 # (2단계 추가) C++ OpenCV 영상처리 DLL 프로젝트
│       ├── include/                  # C 익스포트 헤더 파일
│       └── src/                      # OpenCV 기반 검사 알고리즘 구현부
│
├── docs/                             # 아키텍처 다이어그램 및 단계별 실행 데모 GIF
├── .gitignore                        # Visual Studio / .NET / C++ 빌드 산출물 제외
└── README.md
```

---

## 5. 개발 및 실행 환경 (Getting Started)

* **OS:** Windows 10 / 11 (x64)
* **IDE:** Visual Studio 2022 (v17.8 이상)
* **SDK & Dependencies:**
  * .NET 8.0 SDK (`net8.0-windows`)
  * NuGet: `CommunityToolkit.Mvvm` (v8.3.2+)
  * OpenCV 4.x (2단계 C++ `VisionEngine.dll` 빌드 시 필요)

```bash
# 저장소 클론
git clone https://github.com/dyleeS2/wpf-vision-inspector.git
cd wpf-vision-inspector
```

1. `src/VisionClient` 경로의 솔루션을 Visual Studio 2022에서 엽니다.
2. 빌드 구성을 `x64`로 설정한 뒤 `F5`를 눌러 1단계 라이브 뷰어 프로토타입을 실행합니다.