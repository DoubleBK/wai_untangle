using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Logic;
using Game.Utilities;

namespace Game.Core
{
    /// <summary>
    /// 게임 매니저 (Singleton)
    /// 게임 전역 상태와 데이터를 관리합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ========== 싱글톤 ==========
        public static GameManager Instance { get; private set; }

        // ========== 인스펙터 노출 변수 ==========
        [Header("게임 설정")]
        [SerializeField] private int _currentLevelId = 1;

        // ========== 게임 데이터 ==========
        private List<SlotData> _slots = new List<SlotData>();
        private List<PinData> _pins = new List<PinData>();
        private List<RopeData> _ropes = new List<RopeData>();
        private List<IntersectionData> _intersections = new List<IntersectionData>();

        // ========== 프로퍼티 ==========
        public IReadOnlyList<SlotData> Slots => _slots;
        public IReadOnlyList<PinData> Pins => _pins;
        public IReadOnlyList<RopeData> Ropes => _ropes;
        public IReadOnlyList<IntersectionData> Intersections => _intersections;
        public int CurrentLevelId => _currentLevelId;
        public int IntersectionCount => _intersections.Count;

        // ========== 이벤트 ==========
        public event Action<int> OnIntersectionCountChanged;
        public event Action OnLevelCleared;
        public event Action<PinData, SlotData> OnPinSnapped;
        public event Action OnRopePathsUpdated;

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
            DontDestroyOnLoad(gameObject);

            PrototypeDebug.Log("GameManager initialized");
        }

        private void Start()
        {
            // 초기 레벨 로드 (테스트용)
            // LoadLevel(_currentLevelId);
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 레벨 데이터 설정 (외부에서 주입)
        /// </summary>
        public void SetLevelData(List<SlotData> slots, List<PinData> pins, List<RopeData> ropes)
        {
            _slots = slots ?? new List<SlotData>();
            _pins = pins ?? new List<PinData>();
            _ropes = ropes ?? new List<RopeData>();

            // 로프 렌더링 경로 초기화
            foreach (var rope in _ropes)
            {
                rope.InitializeRenderPath(_pins);
            }

            // 초기 교차 계산
            RecalculateIntersections();

            PrototypeDebug.Log($"Level data set: {_slots.Count} slots, {_pins.Count} pins, {_ropes.Count} ropes");
        }

        /// <summary>
        /// 핀을 새 슬롯에 스냅
        /// </summary>
        public bool SnapPinToSlot(PinData pin, SlotData targetSlot)
        {
            if (pin == null || targetSlot == null)
            {
                PrototypeDebug.LogWarning("SnapPinToSlot: pin or targetSlot is null");
                return false;
            }

            // 대상 슬롯이 비어있는지 확인
            if (!targetSlot.IsEmpty)
            {
                PrototypeDebug.LogWarning($"SnapPinToSlot: target slot {targetSlot.Id} is occupied");
                return false;
            }

            // 이전 슬롯 해제
            SlotData previousSlot = GetSlotByIndex(pin.SlotIndex);
            if (previousSlot != null)
            {
                previousSlot.Release();
            }

            // 새 슬롯 점유
            targetSlot.Occupy(pin.Id);
            pin.SlotIndex = _slots.IndexOf(targetSlot);
            pin.SyncPositionFromSlot(targetSlot);

            // 교차 재계산 (내부에서 로프 경로 초기화 + helix 적용)
            RecalculateIntersections();

            // 이벤트 발생
            OnPinSnapped?.Invoke(pin, targetSlot);

            PrototypeDebug.Log($"Pin {pin.Id} snapped to slot {targetSlot.Id}");

            return true;
        }

        /// <summary>
        /// 교차 재계산
        /// </summary>
        public void RecalculateIntersections()
        {
            int previousCount = _intersections.Count;

            _intersections = IntersectionCalculator.CalculateAllIntersections(_ropes, _pins);

            // 모든 로프에 helix 적용
            foreach (var rope in _ropes)
            {
                rope.ApplyHelixAtIntersections(_intersections, _pins);
            }

            // RopeRenderer들에 경로 업데이트 알림
            OnRopePathsUpdated?.Invoke();

            // 교차 수 변경 시 이벤트 발생
            if (_intersections.Count != previousCount)
            {
                OnIntersectionCountChanged?.Invoke(_intersections.Count);
                PrototypeDebug.Log($"Intersection count changed: {previousCount} -> {_intersections.Count}");
            }

            // 승리 조건 확인
            if (_intersections.Count == 0)
            {
                OnLevelCleared?.Invoke();
                PrototypeDebug.Log("Level cleared!");
            }
        }

        /// <summary>
        /// 슬롯 인덱스로 슬롯 데이터 획득
        /// </summary>
        public SlotData GetSlotByIndex(int index)
        {
            if (index < 0 || index >= _slots.Count) return null;
            return _slots[index];
        }

        /// <summary>
        /// ID로 핀 데이터 획득
        /// </summary>
        public PinData GetPinById(int id)
        {
            return _pins.Find(p => p.Id == id);
        }

        /// <summary>
        /// ID로 로프 데이터 획득
        /// </summary>
        public RopeData GetRopeById(int id)
        {
            return _ropes.Find(r => r.Id == id);
        }

        /// <summary>
        /// 위치에서 가장 가까운 빈 슬롯 찾기
        /// </summary>
        public SlotData FindNearestEmptySlot(Vector2 position, float maxDistance)
        {
            SlotData nearest = null;
            float minDist = float.MaxValue;

            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty) continue;

                float dist = Vector2.Distance(position, slot.Position);
                if (dist <= maxDistance && dist < minDist)
                {
                    minDist = dist;
                    nearest = slot;
                }
            }

            return nearest;
        }

        // ========== 에러 처리 ==========
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
