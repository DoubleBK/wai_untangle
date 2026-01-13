using System;
using UnityEngine;
using Game.Data;
using Game.Utilities;

namespace Game.Core
{
    /// <summary>
    /// 입력 매니저 (SYS_021)
    /// 터치 입력 감지, 좌표 변환, 이벤트 전파를 담당합니다.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ========== 싱글톤 ==========
        public static InputManager Instance { get; private set; }

        // ========== 인스펙터 노출 변수 ==========
        [Header("필수 참조")]
        [SerializeField] private Camera _mainCamera;

        [Header("설정값")]
        [SerializeField] private float _pinHitRadius = 0.5f;
        [SerializeField] private LayerMask _pinLayerMask = -1;

        // ========== 내부 상태 변수 ==========
        private bool _isDragging;
        private PinData _selectedPin;
        private bool _isInputLocked;

        // ========== 이벤트 ==========
        /// <summary>
        /// 핀 선택 시 발생 (DragSystem에서 구독)
        /// </summary>
        public event Action<PinData, Vector2> OnPinSelected;

        /// <summary>
        /// 드래그 업데이트 시 발생
        /// </summary>
        public event Action<Vector2> OnDragUpdate;

        /// <summary>
        /// 드래그 종료 시 발생
        /// </summary>
        public event Action<Vector2> OnDragEnd;

        // ========== 프로퍼티 ==========
        public bool IsDragging => _isDragging;
        public PinData SelectedPin => _selectedPin;
        public bool IsInputLocked => _isInputLocked;

        // ========== 유니티 라이프사이클 ==========
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 카메라 자동 할당
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            PrototypeDebug.Log("InputManager initialized");
        }

        private void Update()
        {
            if (_isInputLocked) return;

            HandleTouchInput();
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 입력 잠금 설정 (팝업, 애니메이션 중 사용)
        /// </summary>
        public void SetInputLock(bool locked)
        {
            _isInputLocked = locked;

            // 잠금 시 진행 중인 드래그 취소
            if (locked && _isDragging)
            {
                CancelDrag();
            }

            PrototypeDebug.Log($"Input lock: {locked}");
        }

        /// <summary>
        /// 현재 드래그 강제 취소
        /// </summary>
        public void CancelDrag()
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectedPin = null;
                PrototypeDebug.Log("Drag cancelled");
            }
        }

        // ========== 내부 유틸리티 ==========

        /// <summary>
        /// 터치 입력 처리 (매 프레임)
        /// </summary>
        private void HandleTouchInput()
        {
#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleMobileTouch();
#endif
        }

        /// <summary>
        /// 마우스 입력 처리 (에디터용)
        /// </summary>
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnTouchStart(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _isDragging)
            {
                OnTouchMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                OnTouchEnd(Input.mousePosition);
            }
        }

        /// <summary>
        /// 모바일 터치 처리
        /// </summary>
        private void HandleMobileTouch()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchStart(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_isDragging)
                    {
                        OnTouchMove(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isDragging)
                    {
                        OnTouchEnd(touch.position);
                    }
                    break;
            }
        }

        /// <summary>
        /// 터치 시작 처리
        /// </summary>
        private void OnTouchStart(Vector2 screenPos)
        {
            Vector2 worldPos = ScreenToWorldPosition(screenPos);
            PinData pin = TryGetPinAtPosition(worldPos);

            if (pin != null)
            {
                _isDragging = true;
                _selectedPin = pin;

                OnPinSelected?.Invoke(pin, worldPos);
                PrototypeDebug.Log($"Pin {pin.Id} selected at {worldPos}");
            }
        }

        /// <summary>
        /// 터치 이동 처리
        /// </summary>
        private void OnTouchMove(Vector2 screenPos)
        {
            Vector2 worldPos = ScreenToWorldPosition(screenPos);
            OnDragUpdate?.Invoke(worldPos);
        }

        /// <summary>
        /// 터치 종료 처리
        /// </summary>
        private void OnTouchEnd(Vector2 screenPos)
        {
            Vector2 worldPos = ScreenToWorldPosition(screenPos);

            OnDragEnd?.Invoke(worldPos);
            PrototypeDebug.Log($"Drag ended at {worldPos}");

            _isDragging = false;
            _selectedPin = null;
        }

        /// <summary>
        /// 스크린 좌표 → 월드 좌표 변환
        /// Orthographic 카메라용
        /// </summary>
        private Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (_mainCamera == null) return Vector2.zero;

            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z)
            );

            return new Vector2(worldPos.x, worldPos.y);
        }

        /// <summary>
        /// 특정 위치에서 핀 찾기
        /// 거리 기반 탐색 (Collider 불필요)
        /// </summary>
        private PinData TryGetPinAtPosition(Vector2 worldPos)
        {
            if (GameManager.Instance == null) return null;

            foreach (var pin in GameManager.Instance.Pins)
            {
                float distance = Vector2.Distance(worldPos, pin.LogicPos);
                if (distance <= _pinHitRadius)
                {
                    return pin;
                }
            }

            return null;
        }

        // ========== 에러 처리 ==========
        private void OnValidate()
        {
            if (_pinHitRadius <= 0)
            {
                _pinHitRadius = 0.5f;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
