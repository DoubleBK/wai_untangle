using UnityEngine;
using Game.Data;
using Game.Utilities;

namespace Game
{
    /// <summary>
    /// 핀 컨트롤러
    /// 개별 핀의 비주얼과 인터랙션을 관리합니다.
    /// </summary>
    public class PinController : MonoBehaviour
    {
        // ========== 인스펙터 노출 변수 ==========
        [Header("필수 참조")]
        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("비주얼 설정")]
        [SerializeField] private float _normalScale = 1f;
        [SerializeField] private float _selectedScale = 1.2f;

        // ========== 내부 상태 변수 ==========
        private PinData _pinData;
        private MaterialPropertyBlock _propertyBlock;
        private bool _isSelected;

        // ========== 프로퍼티 ==========
        public PinData PinData => _pinData;
        public int PinId => _pinData?.Id ?? -1;
        public bool IsSelected => _isSelected;

        // ========== 유니티 라이프사이클 ==========
        private void Awake()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            _propertyBlock = new MaterialPropertyBlock();
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 핀 데이터로 초기화
        /// </summary>
        public void Initialize(PinData pinData, Color color)
        {
            _pinData = pinData;

            if (_pinData == null)
            {
                PrototypeDebug.LogWarning("PinController.Initialize: pinData is null");
                return;
            }

            // 위치 설정
            transform.position = _pinData.WorldPos;

            // 색상 설정
            SetColor(color);

            // 게임오브젝트 이름 설정
            gameObject.name = $"Pin_{_pinData.Id}";

            PrototypeDebug.Log($"PinController initialized: Pin {_pinData.Id}");
        }

        /// <summary>
        /// 핀 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            if (_meshRenderer == null) return;

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            float targetScale = selected ? _selectedScale : _normalScale;
            transform.localScale = Vector3.one * targetScale;

            // TODO: DOTween으로 부드러운 스케일 애니메이션
            // transform.DOScale(targetScale, 0.1f);
        }

        /// <summary>
        /// 위치 업데이트 (스냅 후)
        /// </summary>
        public void UpdatePosition(Vector3 newPosition)
        {
            transform.position = newPosition;

            if (_pinData != null)
            {
                _pinData.WorldPos = newPosition;
                _pinData.LogicPos = new Vector2(newPosition.x, newPosition.y);
            }
        }

        /// <summary>
        /// 슬롯 위치로 동기화
        /// </summary>
        public void SyncToSlot(SlotData slot)
        {
            if (slot == null || _pinData == null) return;

            _pinData.SyncPositionFromSlot(slot);
            transform.position = _pinData.WorldPos;
        }

        // ========== 에러 처리 ==========
        private void OnValidate()
        {
            if (_normalScale <= 0) _normalScale = 1f;
            if (_selectedScale <= 0) _selectedScale = 1.2f;
        }
    }
}
