# Rope Untangle Spiral (가칭) - MVP 기획 및 기술 명세서

## 1. 타이틀 및 소개

- **프로젝트명(가칭):** Rope Untangle Spiral
- **한 줄 소개:** 핀을 슬롯에 스냅 이동해 로프 교차를 제거하는 Untangle 퍼즐에, 교차점에서 로프가 **"나선형으로 감기는"** 3D 연출을 더한 하이퍼캐주얼 퍼즐.
- **장르:** Untangle 퍼즐 + 하이퍼캐주얼 + 라이트 메타(선택)

---

## 2. 테마와 화면 설명

### 2-1. 테마/비주얼

- **주요 비주얼 요소:**
  - **Pin:** 3D Sphere (하이라이트/그림자 적용)
  - **Rope:** 3D Tube Mesh (꼬임 텍스처, UV 타일링 적용)
  - **Intersection:** "나선형 감김(여러 번 랩)" 연출 + 상하 깊이(Z)로 가독성 확보
- **컨셉 키워드:** 정리, 해결, 만족감 ("얽힘 → 정돈"의 시각적 쾌감)

### 2-2. 게임 화면 구성 (모바일 세로)

- **상단:** Level 표시 / 현재 교차 수 (0 목표) / Pause 버튼
- **중앙:** 슬롯 그리드 + 핀(Pin) + 로프(Tube Mesh)
- **하단:** Undo / Hint / (옵션) Shuffle
- **피드백:**
  - **교차 발생:** 교차점 글로우(Glow), 해당 로프 약한 진동
  - **교차 0 (클리어):** 클리어 FX (파티클/사운드)

### 2-3. 유저 FLOW (핵심 루프)

1. 레벨 시작
2. 핀 드래그
3. 빈 슬롯 스냅
4. 교차 수 갱신 (실시간)
5. 교차 0 달성
6. 클리어/보상
7. 다음 레벨

**수익 트리거 (IAA/IAP - MVP 최소 탑재)**

- **Rewarded:** 힌트 / Undo / 클리어 2배 보상
- **Interstitial:** 2~3레벨당 1회 (리모트 조정 권장)
- **IAP:** Remove Ads (선택)

---

## 3. 구현 상세 (단계적 추론 기반)

### 3-1. Unity 프로젝트 TYPE

| 항목           | 설정                                  |
| :------------- | :------------------------------------ |
| **Unity 버전** | 2022.3 LTS 이상                       |
| **템플릿**     | **3D (URP)**                          |
| **카메라**     | **Orthographic** (직교 투영)          |
| **뷰**         | Top-Down 또는 Front (게임판은 평면)   |
| **오브젝트**   | Pin/Rope/Slot 모두 **3D 프리팹** 사용 |

**선택 이유:**

- 교차점에서 "위/아래 + 나선형 감김"을 Z축(깊이)으로 자연스럽게 표현하기 위함.
- 판정은 **2D 기하**로 고정하여 버그 및 튜닝 비용 최소화.
- Rope 표현에 있어 `LineRenderer`보다 `Tube Mesh`/`Spline Mesh`가 나선형 표현에 적합.

### 3-2. 장르 규칙 정의 (이 게임이 '무엇'인지)

- 본질은 **"교차(Intersection) 제거 퍼즐"**입니다.
- 퍼즐의 정답과 난이도는 **로프의 그래프 구조 + 핀의 슬롯 배치**로 결정됩니다.
- "나선형 감김"은 연출이지만, 사용자는 룰처럼 느낄 수 있으므로 **보이는 중심선(Visual Path)**과 **판정 중심선(Logic Path)**을 동일하게 유지해야 합니다.

### 3-3. 핵심 아키텍처 (3 레이어 분리)

1. **Layer 1) 판정 레이어 (Game Logic)**
   - 입력, 스냅, 점유, 교차 판정, 승리 조건 처리.
   - `Vector2`(평면) 기반, Unity 물리엔진 미사용.

2. **Layer 2) 애니메이션 레이어 (Animation)**
   - 드롭/클리어 시 흔들림(스프링-감쇠) 효과.
   - 중심선(path)에 "offset"을 주거나 렌더링용 포인트에만 흔들림 적용.

3. **Layer 3) 렌더링 레이어 (Rendering)**
   - 3D Tube Mesh 생성 및 갱신.
   - 교차점에서 **나선형 감김(랩 수, 반지름, 높이)** 파라미터로 포인트를 변형해 표현.
   - Z축으로 상하 관계를 확정해 Z-fighting(깜빡임) 방지.

### 3-4. 데이터 모델 (MVP)

*Claude 제안 구조 유지 + Rope Path 캐시 추가*
```csharp
public class Slot
{
    public int Id;
    public Vector2 Position;        // 판정용 2D
    public int OccupiedByPinId = -1;
}

public class Pin
{
    public int Id;
    public int SlotIndex;
    public int RopeId;

    public Vector2 LogicPos;        // Slot.Position 캐시
    public Vector3 WorldPos;        // 렌더링(3D)
}

public class Rope
{
    public int Id;
    public Color RopeColor;
    public List<int> PinIds;        // MVP: 2개 (A,B)

    public int RenderPriority;      // 상하 결정(결정론)
    public List<Vector3> RenderPath; // 튜브 메시 생성에 사용하는 중심선(3D)
}

public class Intersection
{
    public int RopeAId, RopeBId;
    public int SegmentAIndex, SegmentBIndex;
    public Vector2 Point;
    public int TopRopeId;
}
```

### 3-5. 인게임 핵심 규칙 (로직/알고리즘)

#### A) 입력: 드래그 & 스냅

1. **Select:** 유저가 핀 터치 시 히트 테스트(Raycast).
2. **Drag:** 드래그 중 "가장 가까운 빈 슬롯"을 찾아 하이라이트 표시.
3. **Drop (Release):**
   - 빈 슬롯 범위 내라면: 점유 업데이트 (`OccupiedByPinId` 변경).
   - 아니라면: 원래 위치로 롤백.
   - **Action:** 드롭 성공 시 `Pin.LogicPos` 갱신 → 연결된 `Rope`들의 `Path` 갱신 → **교차 재계산** → 승리 조건 체크.

#### B) 교차 판정: 2D 기하학 (결정론적)

- 물리 엔진의 `Collider`/`Trigger` 이벤트를 승리 판정에 사용하지 않습니다 (오차 방지).
- 순수 수학적 **선분-선분 교차(Line Segment Intersection)** 알고리즘을 사용합니다.
```csharp
// 선분 AB와 선분 CD의 교차 여부 판정 (교차점 반환)
public static bool SegmentIntersect(
    Vector2 A, Vector2 B, Vector2 C, Vector2 D,
    out Vector2 intersection, float eps = 1e-6f)
{
    intersection = Vector2.zero;
    Vector2 r = B - A;
    Vector2 s = D - C;
    float rxs = Cross(r, s);

    // 평행하거나 0인 경우 처리
    if (Mathf.Abs(rxs) < eps) return false;

    float t = Cross(C - A, s) / rxs;
    float u = Cross(C - A, r) / rxs;

    // t와 u가 0~1 사이면 교차함
    if (t >= -eps && t <= 1 + eps && u >= -eps && u <= 1 + eps)
    {
        intersection = A + t * r;
        return true;
    }
    return false;
}

static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
```

#### C) 성능 최적화 (필수)

- **드래그 중 (Real-time):** 현재 움직이는 핀에 연결된 로프 vs 나머지 로프들만 검사 (O(N)).
- **드롭 시 (Commit):** 전체 로프 간 교차 재계산 (O(N²)). (N < 50 이므로 모바일에서 부하 없음)

#### D) 승리 조건
```
if (TotalIntersectionCount == 0) → Level Clear
```

---

## 4. "나선형 꼬임(여러 번 감김)" 렌더링 설계 (MVP 핵심)

### 4-1. MVP 권장 방식: 교차점 전용 '랩(Helix) 포인트 삽입' + 튜브 메시

- 전체 로프를 물리 시뮬레이션 하지 않고, 교차점(Intersection Point) 부근에서만 기하학적으로 경로를 변형합니다.
- TopRope가 BottomRope를 타고 넘어가면서 나선형(Helix)을 그리도록 점(Vertex)을 추가합니다.

#### (1) Top/Bottom 결정 (Z-Sorting)

- 각 로프에 고유한 `RenderPriority` (정수) 부여.
- 교차 발생 시, Priority가 높은 쪽이 TopRope가 되어 위로 감습니다.
- **Tip:** 드래그 중인 로프는 일시적으로 가장 높은 Priority를 가져야 가독성이 좋습니다.

#### (2) 헬릭스(랩) 경로 생성 개념
```csharp
// 중심선(Center)을 기준으로 나선형으로 감기는 포인트 리스트 생성
List<Vector3> GenerateHelixWrapPoints(
    Vector3 center, Vector3 axisDir, // 교차점 중심, 감길 축(Bottom 로프 방향)
    float radius, float height,      // 튜브 반지름 + 여유값, 위로 들릴 높이
    int wrapCount, int samples)      // 몇 바퀴 감을지, 샘플링 해상도
{
    axisDir.Normalize();
    // 축에 수직인 벡터(u, v) 구하기
    Vector3 u = Vector3.Cross(axisDir, Vector3.up).normalized;
    if (u.sqrMagnitude < 1e-4f) u = Vector3.Cross(axisDir, Vector3.right).normalized;
    Vector3 v = Vector3.Cross(axisDir, u).normalized;

    var pts = new List<Vector3>();
    float totalAngle = Mathf.PI * 2f * wrapCount;

    for (int i = 0; i <= samples; i++)
    {
        float t = (float)i / samples; // 0.0 ~ 1.0
        float angle = totalAngle * t;
        
        // Z축(Up)으로 살짝 들리면서(Sine curve), 원형 궤적(Cos, Sin)을 그림
        float zLift = height * Mathf.Sin(Mathf.PI * t); 
        Vector3 radial = (Mathf.Cos(angle) * u + Mathf.Sin(angle) * v) * radius;
        
        // 실제론 3D 평면 좌표계에 맞게 조정 필요
        pts.Add(center + radial + new Vector3(0, zLift, 0)); 
    }
    return pts;
}
```

### 4-2. 튜브 메시 생성기 (Procedural Mesh)

- **역할:** `List<Vector3>` 형태의 경로(Path)를 받아 **3D 원통형 메시(Mesh)**를 생성.
- **필수 구현 요소:**
  - **Spline Interpolation:** 입력된 점들이 각져있으므로 Catmull-Rom Spline으로 부드럽게 보간.
  - **Cross Section:** 각 경로 점마다 원형 단면(Vertices) 배치.
  - **UV Mapping (핵심):**
    - U좌표: 원 둘레 (0~1)
    - V좌표: 경로의 누적 길이(Arc Length) 기반 타일링.
    - **효과:** 텍스처 자체가 꼬인 밧줄 이미지를 사용하면, V좌표 타일링 덕분에 자연스럽게 꼬인 밧줄처럼 보임.

---

## 5. 레벨 생성 (역재생 방식, 풀이 보장)

### 5-1. 생성 원리 (Reverse Entanglement)

1. **Solved State:** 핀과 로프가 전혀 교차하지 않는 "정답 상태"를 먼저 배치합니다.
2. **Scramble:** 핀을 무작위 슬롯으로 이동시킵니다. (물리적 연결 유지)
3. **Validate:**
   - 현재 교차 수가 목표 난이도(예: 3~5개)에 도달했는지 확인.
   - 너무 쉽거나 어려우면 다시 섞기.
4. **Finalize:** 최종 상태를 레벨 데이터(JSON/ScriptableObject)로 저장.

### 5-2. 난이도 파라미터

- **Level 1~5 (Tutorial):** 로프 2~3개, 교차 1~2개.
- **Level 6+:** 로프 3 + floor(Level * 0.1), 슬롯 여유 공간 축소.

---

## 6. MapTool (에디터) 상세 기획

### 6-1. 목표

- Unity Editor 내에서 즉시 레벨을 만들고 테스트(Play) 할 수 있어야 함.
- "2D 판정"과 "3D 비주얼"이 일치하는지 눈으로 검증.

### 6-2. 기능 명세

- **Grid Editor:** 슬롯의 행/열 개수 조절, 비활성 슬롯(구멍) 설정.
- **Pin/Rope Painter:** 마우스 클릭으로 핀 생성, 드래그로 로프 연결.
- **Auto Scramble:** "섞기" 버튼을 누르면 자동으로 역재생 알고리즘 수행 후 결과 표시.
- **Visual Toggle:** LineRenderer(Debug) 모드와 TubeMesh(Real) 모드 스위칭.

---

## 7. 핵심 기능 우선순위 (로드맵)

| 기능 그룹          | 세부 기능                        | 중요도 | 난이도 | 비고            |
| :----------------- | :------------------------------- | :----- | :----- | :-------------- |
| **Phase 1 (Core)** | 드래그 앤 드롭, 슬롯 스냅        | 필수   | 하     | 조작감 최우선   |
|                    | 선분 교차 판정 (2D 로직)         | 필수   | 중     | 결정론적 판정   |
|                    | 기본 튜브 메시 생성              | 필수   | 중     | 직선 튜브       |
| **Phase 2 (Visual)** | 교차점 나선형(Helix) 랩핑 구현 | 핵심   | 상     | 이 게임의 USP   |
|                    | 로프 텍스처 UV 타일링            | 필수   | 하     | 퀄리티 업       |
| **Phase 3 (Content)** | 레벨 에디터 (MapTool)          | 필수   | 상     | 생산성 도구     |
|                    | 역재생 레벨 생성 알고리즘        | 필수   | 중     | 무한 레벨       |

---

## 8. 권장 폴더 구조
```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, InputManager, CameraController
│   ├── Data/           # LevelData, SlotData, PinData
│   ├── Logic/          # UntangleLogic(교차판정), WinCondition
│   ├── Rendering/      # RopeBuilder, TubeMeshGenerator, HelixMath
│   ├── UI/             # InGameUI, PopupUI
│   └── Utils/          # MathHelpers (Spline, Geometry)
├── Editor/             # LevelEditorWindow
├── Prefabs/            # Pin, Slot, RopeObject
├── Materials/          # RopeMat, PinMat, FloorMat
└── Resources/          # LevelData (JSON/SO)
```

---

## 9. MVP "완료 정의" (DoD)

- [ ] **기본 플레이:** 핀을 드래그하여 슬롯에 놓을 수 있고, 로프가 따라 움직인다.
- [ ] **판정 정확성:** 눈에 보이는 교차와 시스템이 인식하는 교차(Count)가 일치한다.
- [ ] **비주얼:** 로프가 3D 튜브 형태이며, 교차하는 지점에서 입체적으로 꼬이는(Helix) 연출이 표현된다.
- [ ] **게임 루프:** 교차 수가 0이 되면 "Clear" 팝업이 뜨고 다음 레벨로 넘어간다.
- [ ] **저작 도구:** 에디터에서 버튼 한 번으로 "풀 수 있는 꼬인 레벨"을 생성할 수 있다.