using System;
using UnityEngine;
using Game.Data;
using Game.Rendering;
using Game.Utilities;

namespace Game.Core
{
    /// <summary>
    /// 드래그 컨트롤러 (SYS_023)
    /// 핀 드래그 시작/진행/종료 처리, 스냅 프리뷰, 롤백을 담당합니다.
    /// </summary>
    public class DragController : MonoBehaviour
    {
        // ========== 싱글톤 ==========
        public static DragController Instance { get; private set; }

        // ========== 인스펙터 노출 변수 ==========
        [Header("설정값")]
        [SerializeField] private float _snapRadius = 1.0f;
        [SerializeField] private float _dragScale = 1.2f;
        [SerializeField] private float _rollbackDuration = 0.2f;

        // ========== 내부 상태 변수 ==========
        private PinData _selectedPin;
        private Transform _selectedPinTransform;
        private int _originalSlotIndex;
        private SlotData _targetSlot;
        private bool _isDragging;
        private Vector3 _originalScale;
        private int _originalRenderPriority;
        private RopeRenderer _cachedRopeRenderer;

        // ========== 이벤트 ==========
        /// <summary>
        /// 슬롯 하이라이트 요청 (HUD에서 구독)
        /// </summary>
        public event Action<SlotData> OnSlotHighlightRequested;

        /// <summary>
        /// 슬롯 하이라이트 해제 요청
        /// </summary>
        public event Action OnSlotHighlightCleared;

        /// <summary>
        /// 드래그 시작 시 발생
        /// </summary>
        public event Action<PinData> OnDragStarted;

        /// <summary>
        /// 드래그 종료 시 발생 (성공/실패 모두)
        /// </summary>
        public event Action<PinData, bool> OnDragEnded;

        // ========== 프로퍼티 ==========
        public bool IsDragging => _isDragging;
        public PinData SelectedPin => _selectedPin;
        public float SnapRadius => _snapRadius;

        // ========== 유니티 라이프사이클 ==========
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PrototypeDebug.Log("DragController initialized");
        }

        private void Start()
        {
            // InputManager 이벤트 구독
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPinSelected += StartDrag;
                InputManager.Instance.OnDragUpdate += UpdateDrag;
                InputManager.Instance.OnDragEnd += EndDrag;
            }
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void StartDrag(PinData pin, Vector2 touchPos)
        {
            if (pin == null) return;

            _selectedPin = pin;
            _originalSlotIndex = pin.SlotIndex;
            _isDragging = true;

            // 핀 Transform 찾기 (PinController 컴포넌트에서)
            _selectedPinTransform = FindPinTransform(pin.Id);

            if (_selectedPinTransform != null)
            {
                // 원래 스케일 저장 후 확대
                _originalScale = _selectedPinTransform.localScale;
                _selectedPinTransform.localScale = _originalScale * _dragScale;
            }

            // 로프 렌더링 우선순위 최상위로 변경
            RopeData rope = GameManager.Instance?.GetRopeById(pin.RopeId);
            if (rope != null)
            {
                _originalRenderPriority = rope.RenderPriority;
                rope.RenderPriority = int.MaxValue;
            }

            OnDragStarted?.Invoke(pin);
            PrototypeDebug.Log($"Drag started: Pin {pin.Id}");
        }

        /// <summary>
        /// 드래그 업데이트
        /// </summary>
        public void UpdateDrag(Vector2 worldPos)
        {
            if (!_isDragging || _selectedPin == null) return;

            // 핀 비주얼 위치 업데이트
            if (_selectedPinTransform != null)
            {
                _selectedPinTransform.position = new Vector3(
                    worldPos.x,
                    worldPos.y,
                    _selectedPinTransform.position.z
                );
            }

            // 핀 데이터 위치 업데이트 (프리뷰용)
            _selectedPin.SetPreviewPosition(worldPos);

            // 가장 가까운 빈 슬롯 탐색
            SlotData nearestSlot = GameManager.Instance?.FindNearestEmptySlot(worldPos, _snapRadius);

            // 하이라이트 업데이트
            if (nearestSlot != _targetSlot)
            {
                OnSlotHighlightCleared?.Invoke();
                _targetSlot = nearestSlot;

                if (_targetSlot != null)
                {
                    OnSlotHighlightRequested?.Invoke(_targetSlot);
                }
            }

            // 로프 프리뷰 업데이트
            UpdateRopePreview();
        }

        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void EndDrag(Vector2 worldPos)
        {
            if (!_isDragging || _selectedPin == null) return;

            _isDragging = false;
            OnSlotHighlightCleared?.Invoke();

            // 스냅 가능한 빈 슬롯 탐색
            SlotData targetSlot = GameManager.Instance?.FindNearestEmptySlot(worldPos, _snapRadius);

            bool snapSuccess = false;

            if (targetSlot != null)
            {
                // 스냅 성공
                snapSuccess = GameManager.Instance.SnapPinToSlot(_selectedPin, targetSlot);

                if (snapSuccess && _selectedPinTransform != null)
                {
                    // 스냅 위치로 이동
                    _selectedPinTransform.position = new Vector3(
                        targetSlot.Position.x,
                        targetSlot.Position.y,
                        _selectedPinTransform.position.z
                    );
                }
            }

            if (!snapSuccess)
            {
                // 롤백
                RollbackToOriginalSlot();
            }

            // 핀 스케일 복원
            if (_selectedPinTransform != null)
            {
                _selectedPinTransform.localScale = _originalScale;
            }

            // 로프 렌더링 우선순위 복원
            RestoreRopePriority();

            OnDragEnded?.Invoke(_selectedPin, snapSuccess);
            PrototypeDebug.Log($"Drag ended: Pin {_selectedPin.Id}, Success: {snapSuccess}");

            // 상태 초기화
            _selectedPin = null;
            _selectedPinTransform = null;
            _targetSlot = null;
            _cachedRopeRenderer = null;
        }

        // ========== 내부 유틸리티 ==========

        /// <summary>
        /// 원래 슬롯으로 롤백
        /// </summary>
        private void RollbackToOriginalSlot()
        {
            if (_selectedPin == null || GameManager.Instance == null) return;

            SlotData originalSlot = GameManager.Instance.GetSlotByIndex(_originalSlotIndex);
            if (originalSlot == null) return;

            // 위치 복원
            _selectedPin.SyncPositionFromSlot(originalSlot);

            if (_selectedPinTransform != null)
            {
                // TODO: DOTween으로 부드러운 롤백 애니메이션 적용
                // _selectedPinTransform.DOMove(originalSlot.Position, _rollbackDuration);

                // 현재는 즉시 이동
                _selectedPinTransform.position = new Vector3(
                    originalSlot.Position.x,
                    originalSlot.Position.y,
                    _selectedPinTransform.position.z
                );
            }

            // 로프 경로도 원래 위치로 복원
            RopeData rope = GameManager.Instance.GetRopeById(_selectedPin.RopeId);
            if (rope != null)
            {
                rope.UpdatePinPositionInPath(_selectedPin.Id, _selectedPin.WorldPos, null);

                // RopeRenderer 메시 업데이트
                if (_cachedRopeRenderer != null)
                {
                    _cachedRopeRenderer.UpdateMeshPreview(rope.RenderPath);
                }
            }

            PrototypeDebug.Log($"Rollback to slot {_originalSlotIndex}");
        }

        /// <summary>
        /// 로프 렌더링 우선순위 복원
        /// </summary>
        private void RestoreRopePriority()
        {
            if (_selectedPin == null || GameManager.Instance == null) return;

            RopeData rope = GameManager.Instance.GetRopeById(_selectedPin.RopeId);
            if (rope != null)
            {
                rope.RenderPriority = _originalRenderPriority;
            }
        }

        /// <summary>
        /// 로프 프리뷰 업데이트
        /// </summary>
        private void UpdateRopePreview()
        {
            if (_selectedPin == null || GameManager.Instance == null) return;

            RopeData rope = GameManager.Instance.GetRopeById(_selectedPin.RopeId);
            if (rope != null && _selectedPinTransform != null)
            {
                rope.UpdatePinPositionInPath(
                    _selectedPin.Id,
                    _selectedPinTransform.position,
                    null // 프리뷰에서는 다른 핀 참조 불필요
                );

                // RopeRenderer 찾아서 프리뷰 업데이트
                if (_cachedRopeRenderer == null || _cachedRopeRenderer.RopeId != _selectedPin.RopeId)
                {
                    _cachedRopeRenderer = FindRopeRenderer(_selectedPin.RopeId);
                }

                if (_cachedRopeRenderer != null)
                {
                    _cachedRopeRenderer.UpdateMeshPreview(rope.RenderPath);
                }
            }
        }

        /// <summary>
        /// RopeId로 RopeRenderer 찾기
        /// </summary>
        private RopeRenderer FindRopeRenderer(int ropeId)
        {
            var ropeRenderers = FindObjectsOfType<RopeRenderer>();
            foreach (var renderer in ropeRenderers)
            {
                if (renderer.RopeId == ropeId)
                {
                    return renderer;
                }
            }
            return null;
        }

        /// <summary>
        /// 핀 ID로 Transform 찾기
        /// </summary>
        private Transform FindPinTransform(int pinId)
        {
            // PinController 컴포넌트를 가진 오브젝트 찾기
            // TODO: PinController 구현 후 연동
            // 현재는 태그나 이름으로 찾기
            GameObject pinObject = GameObject.Find($"Pin_{pinId}");
            return pinObject?.transform;
        }

        // ========== 에러 처리 ==========
        private void OnValidate()
        {
            if (_snapRadius <= 0) _snapRadius = 1.0f;
            if (_dragScale <= 0) _dragScale = 1.2f;
            if (_rollbackDuration <= 0) _rollbackDuration = 0.2f;
        }

        private void OnDestroy()
        {
            // InputManager 이벤트 구독 해제
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPinSelected -= StartDrag;
                InputManager.Instance.OnDragUpdate -= UpdateDrag;
                InputManager.Instance.OnDragEnd -= EndDrag;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
