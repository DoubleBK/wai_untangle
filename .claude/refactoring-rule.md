# 리팩토링 규칙

## 리팩토링 진행 절차

1. **ReferenceProject 참고**
   - `D:\wai_untangle\ProjectSettings` 경로의 프로젝트 구조를 먼저 분석
   - 참고할 만한 패턴이 없다면 이유를 서술하고 독자적으로 설계 (사용자 허락 필요)

2. **대상 스크립트 분석**
   - 리팩토링 대상 .cs 파일을 분석
   - 해당 스크립트가 가진 **책임(Responsibility)**들을 나열

3. **리팩토링 계획 수립**
   - 각 책임을 독립적인 클래스로 분리하는 계획 작성
   - 가장 먼저 분리하면 좋을 부분을 추천

4. **리팩토링 실행**
   - 효율적으로 진행
   - 하나의 스크립트가 너무 길지 않도록 유지

## 리팩토링 기준

### 파일 크기 기준
- 🟢 **양호**: 400줄 이하
- 🟡 **주의**: 400~800줄
- 🔴 **위험**: 800줄 이상 → 분리 필요

### 단일 책임 원칙 (SRP)
- 하나의 클래스는 하나의 책임만 가져야 함
- 변경의 이유가 여러 개라면 분리 대상

### 분리 우선순위
1. **독립적으로 동작 가능한 기능** (의존성이 적은 것)
2. **재사용 가능성이 높은 기능**
3. **테스트가 필요한 핵심 로직**

## 현재 프로젝트 리팩토링 대상

| 파일 | 라인 수 | 상태 | 우선순위 |
|------|---------|------|----------|
| DragController.cs | ~1,094 | 🔴 위험 | ✅ 완료 (추가 분리 불필요) |
| PuzzleBoardSetup.cs | ~1,481 | 🔴 위험 | ✅ 완료 (향후 점진적 분리) |
| GroupBorderRenderer.cs | 836 | 🟡 주의 | ✅ 완료 (디버그 로그 제거) |
| LevelClearSequence.cs | 675 | 🟡 주의 | ✅ 완료 (디버그 로그 제거) |

---

## 리팩토링 히스토리

### 2026-01-07: DragController.cs 1차 리팩토링

**분리된 클래스:**
| 원본 파일 | 분리된 클래스 | 새 파일 경로 | 라인 수 |
|-----------|--------------|-------------|---------|
| DragController.cs | PieceGroup | Assets/Scripts/Puzzle/PieceGroup.cs | ~280줄 |

**변경 내역:**
- `PieceGroup` 클래스를 `DragController.cs`에서 분리하여 별도 파일로 이동
- `DragController.cs`: 1,537줄 → ~1,225줄 (약 312줄 감소)

**PieceGroup 클래스 책임:**
- 피스 컬렉션 관리 (Add/Remove)
- 드래그 위치 추적 (OnDragStart/OnDragUpdate)
- 그룹 병합 (MergeGroup/MergeGroupWithSnap)
- 그룹 테두리 렌더링 관리 (GroupBorderRenderer)
- 정렬 순서 관리 (SetSortingOrder)

**다음 리팩토링 대상 (DragController.cs):**
1. 프레임 테두리 → `PieceFrameRenderer` (~200줄)
2. 셰이더 관리 → `PieceShaderController` (~150줄)

---

### 2026-01-07: DragController.cs 2차 리팩토링

**변경 내역:**
- `PieceCardController` 클래스 생성 (향후 사용 대비)
- EdgeCover 미사용 코드 제거 (~135줄 감소)
- `DragController.cs`: ~1,225줄 → ~1,090줄

**생성된 클래스:**
| 파일 경로 | 라인 수 | 설명 |
|-----------|---------|------|
| Assets/Scripts/Puzzle/PieceCardController.cs | ~270줄 | 카드 플립 시스템 (향후 통합 예정) |

**제거된 코드:**
- `_edgeCovers` 배열 및 `_coverSize` 필드
- `RemoveEdgeCover()`, `RestoreEdgeCover()`, `RestoreAllEdgeCovers()` 메서드
- `SetCoverSize()`, `CreateEdgeCovers()` 메서드

---

### 2026-01-07: DragController.cs 3차 리팩토링

**변경 내역:**
- 중복 코드 통합: `CreatePixelSprite()` + `CreateUnitSprite()` → `CreatePixelSprite(float ppu)`
- `DragController.cs`: ~1,090줄 → ~1,094줄 (변경 미미)

**코드 정리:**
- `CreatePixelSprite(float pixelsPerUnit = 100f)` 메서드로 통합
- 프레임 생성 시 `CreatePixelSprite(1f)` 호출로 변경

**분석 결과 - 추가 분리 보류:**
프레임 테두리 시스템(`PieceFrameRenderer`)과 셰이더 관리(`PieceShaderController`) 분리를 검토한 결과:
- 프레임 시스템이 셰이더 시스템과 밀접하게 연결되어 있음 (`_cornerRadii`, `ApplyCornerRadii()` 등)
- 분리 시 오히려 코드 복잡도 증가 예상
- 현재 구조 유지가 더 효율적

**현재 DragController.cs 구조:**
1. 퍼즐 데이터 (~20줄)
2. 프레임 시스템 + 셰이더 (~300줄)
3. 드래그 처리 (~50줄)
4. 카드 시스템 (~200줄)
5. 패딩 시스템 (~80줄)
6. 모서리/테두리 제어 (~200줄)
7. 펌핑 애니메이션 (~50줄)

**다음 리팩토링 대상:**
- `PuzzleBoardSetup.cs` (1,523줄) - 2순위

---

### 2026-01-07: PuzzleBoardSetup.cs 1차 리팩토링

**생성된 클래스:**
| 파일 경로 | 라인 수 | 설명 |
|-----------|---------|------|
| Assets/Scripts/Puzzle/PuzzleAnimationController.cs | ~365줄 | 애니메이션 시스템 (인트로/스왑/펌핑) |

**변경 내역:**
- `PuzzleAnimationController` 클래스 생성 (향후 통합 예정)
  - 인트로 애니메이션 (카드 날아가기, 뒤집기)
  - 스왑 애니메이션 (조각/그룹 이동)
  - 펌핑 애니메이션 (병합 효과)
- 미사용 코드 제거: `ShufflePieces()`, `CheckConnections()` (~40줄 감소)
- `PuzzleBoardSetup.cs`: 1,523줄 → ~1,481줄

**분석 결과:**
PuzzleBoardSetup.cs는 퍼즐 보드의 핵심 로직을 담고 있어 무분별한 분리보다는
향후 필요 시 점진적으로 분리하는 것이 적합합니다.

**분리 가능 후보 (향후):**
1. 퍼즐 조각/슬롯 생성 → `PuzzlePieceFactory` (~250줄)
2. 연결 체크/병합 로직 → `PuzzleConnectionManager` (~200줄)

---

### 2026-01-07: GroupBorderRenderer.cs 리팩토링

**분석 결과:**
GroupBorderRenderer.cs는 그룹 테두리 렌더링이라는 단일 책임만 가지고 있어 클래스 분리가 불필요합니다.
디버그 로그만 제거하여 코드 정리를 완료했습니다.

**변경 내역:**
- 디버그 로그 제거 (SetPieces, CalculateAndApplyOutline, ConnectEdgesToPath, MoveAllPoints 등)
- `GroupBorderRenderer.cs`: 925줄 → 836줄 (89줄 감소)

**유지된 로그:**
- Debug.LogWarning (경고 메시지) - 문제 진단에 필요

---

### 2026-01-07: LevelClearSequence.cs 리팩토링

**분석 결과:**
LevelClearSequence.cs는 레벨 클리어 시퀀스라는 단일 흐름을 관리하며,
내부 책임들(시퀀스 생성, Result UI, 코인 날아가기, 축하 효과)이 밀접하게 연결되어 있어 분리 시 복잡도가 증가할 것으로 판단됩니다.
디버그 로그만 제거하여 코드 정리를 완료했습니다.

**변경 내역:**
- 디버그 로그 제거 (PlayClearSequence, CreateAndPlaySequence 단계별 로그, 코인 애니메이션 로그 등)
- `LevelClearSequence.cs`: 707줄 → 675줄 (32줄 감소)

**유지된 로그:**
- Debug.LogWarning (경고 메시지) - 문제 진단에 필요

---

## 새 프로젝트 적용 시

### 초기 설계 원칙 (Rope Untangle Spiral 프로젝트)

새 프로젝트 시작 시에는 처음부터 다음 원칙을 준수하여 설계합니다:

1. **SRP (Single Responsibility Principle) 준수**
   - 클래스 하나는 하나의 책임만 가지도록 설계
   - 변경의 이유가 여러 개인 클래스는 분리 대상

2. **파일 크기 기준 초기 적용**
   - 🟢 **목표**: 400줄 이하 유지
   - 🟡 **주의**: 400줄 초과 시 즉시 분리 검토
   - 설계 단계에서 책임을 명확히 분리하여 큰 파일 생성 방지

3. **3레이어 분리 철저 준수**
   - **Logic 레이어**: 판정 로직만 (2D 기하학, 상태 관리)
   - **Animation 레이어**: 시각 효과만 (트윈, 스프링, 이징)
   - **Rendering 레이어**: 렌더링만 (메시 생성, 셰이더, 머티리얼)
   - 레이어 간 직접 참조 금지, 이벤트/인터페이스로 통신

4. **폴더 구조 기반 네임스페이스**
   ```
   Game.Core      → Core 폴더
   Game.Logic     → Logic 폴더
   Game.Rendering → Rendering 폴더
   Game.Animation → Animation 폴더
   Game.Data      → Data 폴더
   Game.UI        → UI 폴더
   ```

5. **Component Template 준수**
   - `.claude/component-template.md`의 구조를 따름
   - Header로 인스펙터 변수 구분
   - 라이프사이클 메서드 순서 유지
