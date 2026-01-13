# Bug Fix History

## 2026-01-13: 로프-핀 연결 끊김 및 Helix 미표시 버그

### 증상
1. **로프가 핀과 연결되지 않는 현상**: 핀을 드롭한 후 로프 끝이 핀 위치에 도달하지 않고 끊어져 보임
2. **교차점에서 Helix가 표시되지 않는 현상**: 로프가 교차하는 지점에서 나선형(Helix) 형태가 생성되지 않음

### 근본 원인 분석

#### 문제의 핵심: 타이밍 및 상태 관리 문제

**코드 흐름 분석:**

```
[EndDrag 호출]
    ↓
GameManager.SnapPinToSlot()
    ↓
GameManager.RecalculateIntersections()
    ├── IntersectionCalculator.CalculateAllIntersections()
    ├── rope.ApplyHelixAtIntersections() ← Helix 적용됨
    └── OnRopePathsUpdated 이벤트 발생
            ↓
        RopeRenderer.OnRopePathsUpdated()
            ├── if (_isSimulating) return; ← ⚠️ 여기서 스킵됨!
            └── UpdateMesh() ← 호출되지 않음
    ↓
[DOTween 애니메이션 실행 중...]
    ↓
OnSnapAnimationComplete()
    ↓
StopPhysicsSimulation() ← 이 시점에야 _isSimulating = false
```

#### 버그 1: 로프-핀 연결 끊김

**원인:**
1. `OnSnapAnimationComplete()`에서 `StopPhysicsSimulation()` 호출 후 명시적인 메시 업데이트가 없음
   - 위치: `DragController.cs:240-251`

2. 물리 시뮬레이션 정지 후 로프 메시가 마지막 시뮬레이션 프레임의 위치에 그대로 남아있음
   - 물리 시뮬레이션의 마지막 앵커 위치와 실제 핀의 최종 스냅 위치가 다를 수 있음

3. `RopeRenderer.OnRopePathsUpdated()`가 `_isSimulating == true` 체크로 인해 조기 반환됨
   - 위치: `RopeRenderer.cs:116-126`
   - `RecalculateIntersections()` 호출 시점에 아직 시뮬레이션이 활성화 상태

#### 버그 2: Helix 미표시

**원인:**
1. `GameManager.RecalculateIntersections()` 내부에서 `ApplyHelixAtIntersections()`가 호출되어 Helix가 `RenderPath`에 적용됨
   - 위치: `GameManager.cs:137-141`

2. 하지만 `OnRopePathsUpdated` 이벤트 발생 시 `RopeRenderer`가 시뮬레이션 중이므로 `UpdateMesh()` 호출이 스킵됨
   - 위치: `RopeRenderer.cs:119-122`

3. 물리 시뮬레이션 중에는 `UpdateMeshFromSimulation()`이 `_simulator.GetPositions()`로 경로를 가져와서 Helix가 포함되지 않은 직선 경로로 메시를 업데이트함
   - 위치: `RopeRenderer.cs:313-337`

### 수정 방안

#### 방안 A: 시뮬레이션 정지 후 명시적 메시 업데이트 (권장)

`RopeRenderer.StopPhysicsSimulation()`에서 시뮬레이션 정지 후 자동으로 `UpdateMesh()` 호출:

```csharp
// RopeRenderer.cs
public void StopPhysicsSimulation()
{
    _isSimulating = false;

    // 시뮬레이션 정지 후 RopeData의 최신 RenderPath로 메시 업데이트
    UpdateMesh();
}
```

#### 방안 B: 이벤트 순서 재조정

`DragController.EndDrag()`에서 스냅 애니메이션 완료 후 `RecalculateIntersections()` 호출:

```csharp
// DragController.cs - OnSnapAnimationComplete 내부
private void OnSnapAnimationComplete(bool wasSuccess)
{
    _isAnimating = false;

    // 1. 물리 시뮬레이션 정지
    if (_cachedRopeRenderer != null)
    {
        _cachedRopeRenderer.StopPhysicsSimulation();
    }

    // 2. 교차점 재계산 (Helix 적용 + 메시 업데이트)
    if (wasSuccess)
    {
        GameManager.Instance?.RecalculateIntersections();
    }

    CleanupDragState();
}
```

단, 이 방안은 `SnapPinToSlot()` 내부에서 이미 `RecalculateIntersections()`를 호출하므로 중복 호출 문제가 있음.

#### 방안 C: 하이브리드 접근 (권장)

1. `RopeRenderer.StopPhysicsSimulation()`에서 `UpdateMesh()` 호출
2. `SnapPinToSlot()` 내부의 `RecalculateIntersections()` 호출은 유지
3. `RopeRenderer.OnRopePathsUpdated()`에서 시뮬레이션 상태 체크 로직 수정:

```csharp
// RopeRenderer.cs
private void OnRopePathsUpdated()
{
    // 시뮬레이션 중이어도 RopeData가 업데이트되었으므로
    // 다음 StopPhysicsSimulation() 시 최신 경로가 적용됨
    if (_isSimulating)
    {
        return;
    }

    UpdateMesh();
}
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `DragController.cs` | 드래그 시작/종료 및 물리 시뮬레이션 제어 |
| `RopeRenderer.cs` | 로프 메시 렌더링 및 물리 시뮬레이션 실행 |
| `GameManager.cs` | 교차점 계산 및 Helix 적용 |
| `RopeData.cs` | Helix 포인트 생성 및 RenderPath 관리 |
| `VerletRopeSimulator.cs` | Verlet 물리 시뮬레이션 |

### 테스트 체크리스트

- [ ] 핀 드래그 후 드롭 시 로프가 핀에 정확히 연결되는지
- [ ] 로프 교차점에서 Helix가 정상적으로 표시되는지
- [ ] 드래그 중 물리 시뮬레이션이 정상 동작하는지
- [ ] 롤백 시에도 로프가 정상적으로 표시되는지
- [ ] 여러 번 드래그/드롭 반복 시 상태가 정상인지

---

## 2026-01-13: 핀 드롭 시 잘못된 위치로 이동하는 버그

### 증상
핀을 특정 위치에 드롭했을 때, 해당 위치가 아닌 다른 슬롯으로 이동해버림

### 근본 원인
`GameManager.FindNearestEmptySlot()`에서 드래그 중인 핀이 점유한 슬롯을 후보에서 제외하여, 원래 위치에 다시 놓을 수 없었음

### 수정 내용
`FindNearestEmptySlot()`에 `excludePinId` 파라미터 추가:

```csharp
// GameManager.cs
public SlotData FindNearestEmptySlot(Vector2 position, float maxDistance, int excludePinId = -1)
{
    foreach (var slot in _slots)
    {
        // 빈 슬롯이거나, 드래그 중인 핀이 점유한 슬롯인 경우 후보에 포함
        bool isAvailable = slot.IsEmpty || slot.OccupiedByPinId == excludePinId;
        if (!isAvailable) continue;
        // ...
    }
}
```

### 관련 파일
- `GameManager.cs`
- `DragController.cs`

---

## 2026-01-13: 로프 끊김 현상 (드래그 중)

### 증상
핀 드래그 중 로프가 핀에서 끊어져 보이는 현상

### 근본 원인
`VerletRopeSimulator.SetAnchorPositions()`에서 `_segmentLength`를 재계산하지 않아, 앵커 위치가 변경되어도 세그먼트 길이가 초기값으로 유지됨

### 수정 내용
```csharp
// VerletRopeSimulator.cs
public void SetAnchorPositions(Vector3 start, Vector3 end)
{
    _startAnchor = start;
    _endAnchor = end;

    // 핀 간 거리에 맞게 세그먼트 길이 재계산
    float pinDistance = Vector3.Distance(start, end);
    if (pinDistance > 0.001f && NodeCount > 1)
    {
        _segmentLength = pinDistance / (NodeCount - 1);
    }
}
```

### 관련 파일
- `VerletRopeSimulator.cs`
