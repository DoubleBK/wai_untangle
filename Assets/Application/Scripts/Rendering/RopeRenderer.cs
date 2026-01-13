using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Physics;
using Game.Utilities;

namespace Game.Rendering
{
    /// <summary>
    /// 로프 렌더러 (SYS_027)
    /// 개별 로프의 3D 튜브 메시를 생성하고 관리합니다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class RopeRenderer : MonoBehaviour
    {
        // ========== 인스펙터 노출 변수 ==========
        [Header("필수 참조")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("튜브 설정")]
        [SerializeField] private float _tubeRadius = 0.08f;
        [SerializeField] private int _radialSegments = 8;
        [SerializeField] private int _splineSamplesPerSegment = 5;

        [Header("UV 설정")]
        [SerializeField] private float _uvTileScale = 2f;

        [Header("물리 시뮬레이션")]
        [SerializeField] private bool _enablePhysics = true;
        [SerializeField] private float _maxRopeLength = 5f;  // 최대 로프 길이 (슬롯 단위, 1 슬롯 = 1 유닛)
        [SerializeField] private int _physicsNodeCount = 12;   // 노드 수 증가 (더 부드러운 곡선)
        [SerializeField] private float _physicsDamping = 0.95f; // 감쇠 낮춤 (더 빠른 안정화)
        [SerializeField] private float _physicsGravity = -5f;   // 중력 증가 (더 많이 처짐)
        [SerializeField] private int _physicsConstraintIterations = 2; // 제약 반복 줄임 (더 느슨함)

        // ========== 내부 상태 변수 ==========
        private RopeData _ropeData;
        private TubeMeshGenerator _meshGenerator;
        private Mesh _mesh;
        private MaterialPropertyBlock _propertyBlock;

        // 스플라인 보간된 경로 캐시
        private List<Vector3> _interpolatedPath = new List<Vector3>();

        // 물리 시뮬레이션
        private VerletRopeSimulator _simulator;
        private bool _isSimulating = false;

        // ========== 프로퍼티 ==========
        public RopeData RopeData => _ropeData;
        public int RopeId => _ropeData?.Id ?? -1;

        // ========== 유니티 라이프사이클 ==========
        private void Awake()
        {
            // 컴포넌트 자동 할당
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            // 메시 생성기 초기화
            _meshGenerator = new TubeMeshGenerator
            {
                TubeRadius = _tubeRadius,
                RadialSegments = _radialSegments,
                UVTileScale = _uvTileScale
            };

            // PropertyBlock 초기화
            _propertyBlock = new MaterialPropertyBlock();

            // 물리 시뮬레이터 초기화
            if (_enablePhysics)
            {
                _simulator = new VerletRopeSimulator
                {
                    MaxRopeLength = _maxRopeLength,
                    NodeCount = _physicsNodeCount,
                    Damping = _physicsDamping,
                    Gravity = _physicsGravity,
                    ConstraintIterations = _physicsConstraintIterations
                };
            }
        }

        private void Start()
        {
            // 초기 메시 생성
            if (_ropeData != null)
            {
                UpdateMesh();
            }

            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRopePathsUpdated += OnRopePathsUpdated;
            }
        }

        private void Update()
        {
            // 물리 시뮬레이션 실행
            if (_isSimulating && _simulator != null)
            {
                _simulator.Simulate(Time.deltaTime);
                UpdateMeshFromSimulation();
            }
        }

        private void OnRopePathsUpdated()
        {
            // 시뮬레이션 중이면 무시 (시뮬레이션이 메시를 직접 업데이트함)
            if (_isSimulating)
            {
                return;
            }

            // 로프 경로가 업데이트되면 메시 재생성
            UpdateMesh();
        }

        // ========== 공개 인터페이스 ==========

        /// <summary>
        /// 로프 데이터로 초기화
        /// </summary>
        public void Initialize(RopeData ropeData, Material ropeMaterial = null)
        {
            _ropeData = ropeData;

            if (_ropeData == null)
            {
                PrototypeDebug.LogWarning("RopeRenderer.Initialize: ropeData is null");
                return;
            }

            // 머티리얼 설정
            if (ropeMaterial != null)
            {
                _meshRenderer.material = ropeMaterial;
            }

            // 색상 설정
            SetColor(_ropeData.RopeColor);

            // 게임오브젝트 이름 설정
            gameObject.name = $"Rope_{_ropeData.Id}";

            // 초기 메시 생성
            UpdateMesh();

            PrototypeDebug.Log($"RopeRenderer initialized: Rope {_ropeData.Id}");
        }

        /// <summary>
        /// 메시 업데이트 (경로 변경 시 호출)
        /// </summary>
        public void UpdateMesh()
        {
            if (_ropeData == null || _ropeData.RenderPath == null || _ropeData.RenderPath.Count < 2)
            {
                return;
            }

            PrototypeDebug.Log($"RopeRenderer {RopeId}: UpdateMesh called, RenderPath count = {_ropeData.RenderPath.Count}");

            // 스플라인 보간
            _interpolatedPath = SplineInterpolator.InterpolateCatmullRom(
                _ropeData.RenderPath,
                _splineSamplesPerSegment
            );

            PrototypeDebug.Log($"RopeRenderer {RopeId}: After spline interpolation, path count = {_interpolatedPath.Count}");

            // 메시 생성
            if (_mesh == null)
            {
                _mesh = _meshGenerator.GenerateMesh(_interpolatedPath);
                _meshFilter.mesh = _mesh;
            }
            else
            {
                // 기존 메시 업데이트 (성능 최적화)
                _meshGenerator.UpdateMesh(_mesh, _interpolatedPath);
            }
        }

        /// <summary>
        /// 프리뷰 경로로 메시 업데이트 (드래그 중)
        /// </summary>
        public void UpdateMeshPreview(List<Vector3> previewPath)
        {
            if (previewPath == null || previewPath.Count < 2)
            {
                return;
            }

            // 스플라인 보간
            _interpolatedPath = SplineInterpolator.InterpolateCatmullRom(
                previewPath,
                _splineSamplesPerSegment
            );

            // 메시 업데이트
            if (_mesh != null)
            {
                _meshGenerator.UpdateMesh(_mesh, _interpolatedPath);
            }
        }

        /// <summary>
        /// 로프 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            if (_meshRenderer == null) return;

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color); // Legacy shader support
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 튜브 반지름 설정
        /// </summary>
        public void SetTubeRadius(float radius)
        {
            _tubeRadius = radius;
            _meshGenerator.TubeRadius = radius;
            UpdateMesh();
        }

        /// <summary>
        /// 렌더링 순서 설정 (Z sorting)
        /// </summary>
        public void SetRenderOrder(int order)
        {
            if (_meshRenderer == null) return;

            _meshRenderer.sortingOrder = order;

            // Z 위치로도 정렬 (3D에서 깊이 정렬)
            Vector3 pos = transform.position;
            pos.z = -order * 0.01f; // 음수로 카메라에 가깝게
            transform.position = pos;
        }

        /// <summary>
        /// 교차점에서 글로우 효과 활성화
        /// </summary>
        public void SetIntersectionGlow(bool enabled, Vector3 glowPosition = default)
        {
            if (_meshRenderer == null) return;

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat("_GlowIntensity", enabled ? 1f : 0f);
            _propertyBlock.SetVector("_GlowPosition", glowPosition);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        // ========== 물리 시뮬레이션 ==========

        /// <summary>
        /// 물리 시뮬레이션 시작 (드래그 시작 시 호출)
        /// </summary>
        public void StartPhysicsSimulation(Vector3 startPoint, Vector3 endPoint)
        {
            if (_simulator == null || !_enablePhysics)
            {
                return;
            }

            _simulator.Initialize(startPoint, endPoint);
            _isSimulating = true;
        }

        /// <summary>
        /// 물리 앵커 위치 업데이트 (드래그 중 호출)
        /// </summary>
        public void UpdatePhysicsAnchors(Vector3 startPoint, Vector3 endPoint)
        {
            if (_simulator == null || !_isSimulating)
            {
                return;
            }

            _simulator.SetAnchorPositions(startPoint, endPoint);
        }

        /// <summary>
        /// 물리 시뮬레이션 정지 (드래그 종료 시 호출)
        /// </summary>
        public void StopPhysicsSimulation()
        {
            _isSimulating = false;

            // 시뮬레이션 정지 후 RopeData의 최신 RenderPath로 메시 업데이트
            // (Helix가 적용된 경로로 메시 갱신)
            UpdateMesh();
        }

        /// <summary>
        /// 물리 시뮬레이션 활성화 여부
        /// </summary>
        public bool IsSimulating => _isSimulating;

        /// <summary>
        /// 시뮬레이션 결과로 메시 업데이트
        /// </summary>
        private void UpdateMeshFromSimulation()
        {
            if (_simulator == null || !_simulator.IsInitialized)
            {
                return;
            }

            var physicsPath = _simulator.GetPositions();
            if (physicsPath == null || physicsPath.Count < 2)
            {
                return;
            }

            // 스플라인 보간
            _interpolatedPath = SplineInterpolator.InterpolateCatmullRom(
                physicsPath,
                _splineSamplesPerSegment
            );

            // 메시 업데이트
            if (_mesh != null)
            {
                _meshGenerator.UpdateMesh(_mesh, _interpolatedPath);
            }
        }

        // ========== 에러 처리 ==========
        private void OnValidate()
        {
            if (_tubeRadius <= 0) _tubeRadius = 0.08f;
            if (_radialSegments < 3) _radialSegments = 3;
            if (_splineSamplesPerSegment < 1) _splineSamplesPerSegment = 1;

            // 에디터에서 변경 시 메시 생성기 업데이트
            if (_meshGenerator != null)
            {
                _meshGenerator.TubeRadius = _tubeRadius;
                _meshGenerator.RadialSegments = _radialSegments;
                _meshGenerator.UVTileScale = _uvTileScale;
            }
        }

        private void OnDestroy()
        {
            // GameManager 이벤트 구독 해제
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRopePathsUpdated -= OnRopePathsUpdated;
            }

            // 메시 정리
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_mesh);
                }
                else
                {
                    DestroyImmediate(_mesh);
                }
            }
        }
    }
}
