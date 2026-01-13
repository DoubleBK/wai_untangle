# Working Progress

## 2026-01-13 작업 내역

### 완료된 작업

#### 1. 로프-핀 연결 끊김 및 Helix 미표시 버그 수정

**문제 증상:**
- 핀 드롭 후 로프 끝이 핀 위치에 도달하지 않음
- 로프 교차점에서 나선형(Helix) 형태가 생성되지 않음

**근본 원인:**
타이밍 및 상태 관리 문제로, `RecalculateIntersections()` 호출 시점에 `_isSimulating == true` 상태여서 `RopeRenderer.OnRopePathsUpdated()`가 조기 반환되어 `UpdateMesh()`가 호출되지 않았음.

```
[EndDrag]
    ↓
SnapPinToSlot() → RecalculateIntersections() → OnRopePathsUpdated()
                                                    ↓
                                    if (_isSimulating) return; ← 스킵됨!
    ↓
[DOTween 애니메이션]
    ↓
OnSnapAnimationComplete() → StopPhysicsSimulation()
                                    ↓
                            _isSimulating = false (메시 업데이트 없음)
```

**수정 내용:**
- 파일: `Assets/Application/Scripts/Rendering/RopeRenderer.cs`
- `StopPhysicsSimulation()`에서 `UpdateMesh()` 호출 추가

```csharp
public void StopPhysicsSimulation()
{
    _isSimulating = false;
    UpdateMesh();  // 추가됨
}
```

#### 2. bug-fix-history.md 작성

버그 분석 및 수정 내역을 문서화:
- 파일: `Assets/Application/Scripts/bug-fix-history.md`
- 3개 버그의 원인 분석 및 수정 내용 기록:
  1. 로프-핀 연결 끊김 및 Helix 미표시 버그
  2. 핀 드롭 시 잘못된 위치로 이동하는 버그
  3. 로프 끊김 현상 (드래그 중)

---

### 이전 세션에서 완료된 작업 (참고)

- Verlet 물리 시뮬레이션 구현
- 물리 파라미터 튜닝 (노드 수, 감쇠, 중력, 제약 반복)
- 핀 드롭 위치 버그 수정 (`FindNearestEmptySlot`에 `excludePinId` 파라미터 추가)
- 드래그 중 로프 끊김 버그 수정 (`SetAnchorPositions`에서 `_segmentLength` 재계산)

---

### 테스트 필요 항목

- [ ] 핀 드래그 후 드롭 시 로프가 핀에 정확히 연결되는지
- [ ] 로프 교차점에서 Helix가 정상적으로 표시되는지
- [ ] 드래그 중 물리 시뮬레이션이 정상 동작하는지
- [ ] 롤백 시에도 로프가 정상적으로 표시되는지
- [ ] 여러 번 드래그/드롭 반복 시 상태가 정상인지

---

### 수정된 파일 목록

| 파일 | 수정 내용 |
|------|-----------|
| `RopeRenderer.cs` | `StopPhysicsSimulation()`에서 `UpdateMesh()` 호출 추가 |
| `bug-fix-history.md` | 신규 생성 - 버그 분석 및 수정 내역 문서화 |

---

### 다음 작업 예정

- Unity에서 버그 수정 테스트
- 물리 파라미터 세밀 튜닝 (필요 시)
