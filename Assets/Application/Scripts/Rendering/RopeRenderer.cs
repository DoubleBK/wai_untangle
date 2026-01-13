using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;
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

        // ========== 내부 상태 변수 ==========
        private RopeData _ropeData;
        private TubeMeshGenerator _meshGenerator;
        private Mesh _mesh;
        private MaterialPropertyBlock _propertyBlock;

        // 스플라인 보간된 경로 캐시
        private List<Vector3> _interpolatedPath = new List<Vector3>();

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

        private void OnRopePathsUpdated()
        {
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

            // 스플라인 보간
            _interpolatedPath = SplineInterpolator.InterpolateCatmullRom(
                _ropeData.RenderPath,
                _splineSamplesPerSegment
            );

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
