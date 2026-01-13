using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Rendering;
using Game.Utilities;

namespace Game
{
    /// <summary>
    /// 테스트 레벨 셋업
    /// 간단한 퍼즐 데이터를 생성하여 시스템 동작을 확인합니다.
    /// </summary>
    public class TestLevelSetup : MonoBehaviour
    {
        // ========== 인스펙터 노출 변수 ==========
        [Header("프리팹 참조")]
        [SerializeField] private GameObject _pinPrefab;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private GameObject _ropePrefab;
        [SerializeField] private Material _ropeMaterial;

        [Header("그리드 설정")]
        [SerializeField] private int _gridRows = 3;
        [SerializeField] private int _gridCols = 3;
        [SerializeField] private float _slotSpacing = 2f;

        [Header("레벨 설정")]
        [SerializeField] private int _ropeCount = 2;

        [Header("비주얼 설정")]
        [SerializeField] private Color[] _ropeColors = new Color[]
        {
            new Color(0.8f, 0.2f, 0.2f), // 빨강
            new Color(0.2f, 0.5f, 0.8f), // 파랑
            new Color(0.2f, 0.8f, 0.3f), // 초록
            new Color(0.9f, 0.7f, 0.1f), // 노랑
        };

        // ========== 내부 상태 변수 ==========
        private List<GameObject> _slotObjects = new List<GameObject>();
        private List<GameObject> _pinObjects = new List<GameObject>();
        private List<GameObject> _ropeObjects = new List<GameObject>();

        // ========== 유니티 라이프사이클 ==========
        private void Start()
        {
            SetupTestLevel();
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 테스트 레벨 생성
        /// </summary>
        [ContextMenu("Setup Test Level")]
        public void SetupTestLevel()
        {
            // 기존 오브젝트 정리
            ClearLevel();

            // 데이터 생성
            var slots = CreateSlotData();
            var pins = CreatePinData(slots);
            var ropes = CreateRopeData(pins);

            // GameManager에 데이터 설정
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetLevelData(slots, pins, ropes);
            }

            // 비주얼 오브젝트 생성
            CreateSlotObjects(slots);
            CreatePinObjects(pins, ropes);
            CreateRopeObjects(ropes);

            PrototypeDebug.Log($"Test level created: {slots.Count} slots, {pins.Count} pins, {ropes.Count} ropes");
        }

        /// <summary>
        /// 레벨 초기화
        /// </summary>
        [ContextMenu("Clear Level")]
        public void ClearLevel()
        {
            foreach (var obj in _slotObjects) if (obj != null) Destroy(obj);
            foreach (var obj in _pinObjects) if (obj != null) Destroy(obj);
            foreach (var obj in _ropeObjects) if (obj != null) Destroy(obj);

            _slotObjects.Clear();
            _pinObjects.Clear();
            _ropeObjects.Clear();
        }

        // ========== 데이터 생성 ==========

        /// <summary>
        /// 슬롯 데이터 생성 (그리드 배치)
        /// </summary>
        private List<SlotData> CreateSlotData()
        {
            var slots = new List<SlotData>();

            // 그리드 중심 계산
            float offsetX = (_gridCols - 1) * _slotSpacing * 0.5f;
            float offsetY = (_gridRows - 1) * _slotSpacing * 0.5f;

            int id = 0;
            for (int row = 0; row < _gridRows; row++)
            {
                for (int col = 0; col < _gridCols; col++)
                {
                    Vector2 pos = new Vector2(
                        col * _slotSpacing - offsetX,
                        row * _slotSpacing - offsetY
                    );

                    slots.Add(new SlotData(id++, pos));
                }
            }

            return slots;
        }

        /// <summary>
        /// 핀 데이터 생성 (로프 양 끝점)
        /// 교차가 발생하도록 대각선 배치
        /// </summary>
        private List<PinData> CreatePinData(List<SlotData> slots)
        {
            var pins = new List<PinData>();

            // 3x3 그리드 기준 교차가 발생하는 배치:
            // Rope 0: 좌하(0) → 우상(8) - 대각선
            // Rope 1: 좌상(6) → 우하(2) - 반대 대각선
            // 이렇게 하면 중앙에서 X자로 교차함

            if (_gridRows >= 3 && _gridCols >= 3 && _ropeCount >= 2)
            {
                // 교차가 발생하는 고정 배치
                int[,] crossingLayout = new int[,]
                {
                    { 0, 8 },  // Rope 0: 좌하 → 우상
                    { 6, 2 },  // Rope 1: 좌상 → 우하
                };

                int pinId = 0;
                for (int r = 0; r < Mathf.Min(_ropeCount, 2); r++)
                {
                    int slot1 = crossingLayout[r, 0];
                    int slot2 = crossingLayout[r, 1];

                    if (slot1 < slots.Count && slot2 < slots.Count)
                    {
                        // 핀 1
                        PinData pin1 = new PinData(pinId++, slot1, r);
                        pin1.SyncPositionFromSlot(slots[slot1]);
                        slots[slot1].Occupy(pin1.Id);
                        pins.Add(pin1);

                        // 핀 2
                        PinData pin2 = new PinData(pinId++, slot2, r);
                        pin2.SyncPositionFromSlot(slots[slot2]);
                        slots[slot2].Occupy(pin2.Id);
                        pins.Add(pin2);
                    }
                }
            }
            else
            {
                // 기존 순차 배치 (폴백)
                int pinId = 0;
                int ropeId = 0;
                List<int> usedSlotIndices = new List<int>();

                for (int r = 0; r < _ropeCount && usedSlotIndices.Count < slots.Count - 1; r++)
                {
                    int slot1 = FindUnusedSlotIndex(slots, usedSlotIndices);
                    if (slot1 < 0) break;
                    usedSlotIndices.Add(slot1);

                    int slot2 = FindUnusedSlotIndex(slots, usedSlotIndices);
                    if (slot2 < 0) break;
                    usedSlotIndices.Add(slot2);

                    PinData pin1 = new PinData(pinId++, slot1, ropeId);
                    pin1.SyncPositionFromSlot(slots[slot1]);
                    slots[slot1].Occupy(pin1.Id);
                    pins.Add(pin1);

                    PinData pin2 = new PinData(pinId++, slot2, ropeId);
                    pin2.SyncPositionFromSlot(slots[slot2]);
                    slots[slot2].Occupy(pin2.Id);
                    pins.Add(pin2);

                    ropeId++;
                }
            }

            return pins;
        }

        /// <summary>
        /// 로프 데이터 생성
        /// </summary>
        private List<RopeData> CreateRopeData(List<PinData> pins)
        {
            var ropes = new List<RopeData>();

            // 핀을 로프별로 그룹화
            Dictionary<int, List<int>> ropePins = new Dictionary<int, List<int>>();

            foreach (var pin in pins)
            {
                if (!ropePins.ContainsKey(pin.RopeId))
                {
                    ropePins[pin.RopeId] = new List<int>();
                }
                ropePins[pin.RopeId].Add(pin.Id);
            }

            // 로프 생성
            foreach (var kvp in ropePins)
            {
                int ropeId = kvp.Key;
                List<int> pinIds = kvp.Value;

                Color color = _ropeColors[ropeId % _ropeColors.Length];

                RopeData rope = new RopeData(ropeId, color, pinIds, ropeId);
                rope.InitializeRenderPath(pins);
                ropes.Add(rope);
            }

            return ropes;
        }

        /// <summary>
        /// 사용되지 않은 슬롯 인덱스 찾기
        /// </summary>
        private int FindUnusedSlotIndex(List<SlotData> slots, List<int> usedIndices)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!usedIndices.Contains(i))
                {
                    return i;
                }
            }
            return -1;
        }

        // ========== 오브젝트 생성 ==========

        /// <summary>
        /// 슬롯 오브젝트 생성
        /// </summary>
        private void CreateSlotObjects(List<SlotData> slots)
        {
            foreach (var slot in slots)
            {
                GameObject slotObj;

                if (_slotPrefab != null)
                {
                    slotObj = Instantiate(_slotPrefab, transform);
                }
                else
                {
                    // 기본 프리미티브 생성
                    slotObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    slotObj.transform.parent = transform;
                    slotObj.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);

                    // 색상 설정
                    var renderer = slotObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }

                slotObj.transform.position = new Vector3(slot.Position.x, slot.Position.y, 0.1f);
                slotObj.name = $"Slot_{slot.Id}";

                _slotObjects.Add(slotObj);
            }
        }

        /// <summary>
        /// 핀 오브젝트 생성
        /// </summary>
        private void CreatePinObjects(List<PinData> pins, List<RopeData> ropes)
        {
            foreach (var pin in pins)
            {
                GameObject pinObj;

                if (_pinPrefab != null)
                {
                    pinObj = Instantiate(_pinPrefab, transform);
                }
                else
                {
                    // 기본 프리미티브 생성
                    pinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pinObj.transform.parent = transform;
                    pinObj.transform.localScale = Vector3.one * 0.5f;
                }

                pinObj.transform.position = pin.WorldPos;
                pinObj.name = $"Pin_{pin.Id}";

                // PinController 추가
                PinController controller = pinObj.GetComponent<PinController>();
                if (controller == null)
                {
                    controller = pinObj.AddComponent<PinController>();
                }

                // 로프 색상 찾기
                RopeData rope = ropes.Find(r => r.Id == pin.RopeId);
                Color color = rope?.RopeColor ?? Color.white;

                controller.Initialize(pin, color);

                _pinObjects.Add(pinObj);
            }
        }

        /// <summary>
        /// 로프 오브젝트 생성
        /// </summary>
        private void CreateRopeObjects(List<RopeData> ropes)
        {
            foreach (var rope in ropes)
            {
                GameObject ropeObj;

                if (_ropePrefab != null)
                {
                    ropeObj = Instantiate(_ropePrefab, transform);
                }
                else
                {
                    // 빈 오브젝트에 필요 컴포넌트 추가
                    ropeObj = new GameObject($"Rope_{rope.Id}");
                    ropeObj.transform.parent = transform;
                    ropeObj.AddComponent<MeshFilter>();
                    MeshRenderer renderer = ropeObj.AddComponent<MeshRenderer>();

                    // 기본 머티리얼 설정
                    if (_ropeMaterial != null)
                    {
                        renderer.material = _ropeMaterial;
                    }
                    else
                    {
                        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    }
                }

                // RopeRenderer 추가 및 초기화
                RopeRenderer ropeRenderer = ropeObj.GetComponent<RopeRenderer>();
                if (ropeRenderer == null)
                {
                    ropeRenderer = ropeObj.AddComponent<RopeRenderer>();
                }

                ropeRenderer.Initialize(rope, _ropeMaterial);
                ropeRenderer.SetRenderOrder(rope.RenderPriority);

                _ropeObjects.Add(ropeObj);
            }
        }

        // ========== 에러 처리 ==========
        private void OnValidate()
        {
            if (_gridRows < 2) _gridRows = 2;
            if (_gridCols < 2) _gridCols = 2;
            if (_slotSpacing <= 0) _slotSpacing = 2f;
            if (_ropeCount < 1) _ropeCount = 1;
        }
    }
}
