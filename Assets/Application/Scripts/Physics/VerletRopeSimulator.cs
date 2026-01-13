using System.Collections.Generic;
using UnityEngine;

namespace Game.Physics
{
    /// <summary>
    /// Verlet Integration 기반 로프 물리 시뮬레이터
    /// 핀 드래그 시 로프가 찰랑거리는 자연스러운 물리 효과를 제공합니다.
    /// </summary>
    public class VerletRopeSimulator
    {
        // ========== 설정 ==========
        public int NodeCount { get; set; } = 10;
        public float Damping { get; set; } = 0.98f;
        public float Gravity { get; set; } = -2f;
        public int ConstraintIterations { get; set; } = 3;
        public float MaxRopeLength { get; set; } = 5f;  // 최대 로프 길이 (슬롯 단위)

        // ========== 노드 데이터 ==========
        private Vector3[] _positions;
        private Vector3[] _previousPositions;
        private float _segmentLength;
        private bool _isInitialized;

        // ========== 앵커 (고정점) ==========
        private Vector3 _startAnchor;
        private Vector3 _endAnchor;

        /// <summary>
        /// 두 앵커 포인트로 초기화
        /// </summary>
        public void Initialize(Vector3 startPoint, Vector3 endPoint)
        {
            if (NodeCount < 2)
            {
                NodeCount = 2;
            }

            _positions = new Vector3[NodeCount];
            _previousPositions = new Vector3[NodeCount];

            _startAnchor = startPoint;
            _endAnchor = endPoint;

            // 핀 간 거리를 로프 길이로 사용 (항상 핀에 도달해야 함)
            float pinDistance = Vector3.Distance(startPoint, endPoint);
            _segmentLength = pinDistance / (NodeCount - 1);

            // 노드 초기화 (시작점과 끝점 사이에 균등 배치)
            // 제약조건이 고정 길이를 유지하도록 함
            for (int i = 0; i < NodeCount; i++)
            {
                float t = (float)i / (NodeCount - 1);
                Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                _positions[i] = pos;
                _previousPositions[i] = pos;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 앵커 위치 업데이트 (드래그 중 호출)
        /// </summary>
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

        /// <summary>
        /// 물리 스텝 실행
        /// </summary>
        public void Simulate(float deltaTime)
        {
            if (!_isInitialized || _positions == null || _positions.Length < 2)
            {
                return;
            }

            // deltaTime 클램프 (너무 큰 값 방지)
            deltaTime = Mathf.Min(deltaTime, 0.02f);

            // 1. 앵커 고정
            _positions[0] = _startAnchor;
            _positions[NodeCount - 1] = _endAnchor;

            // 2. Verlet Integration (중간 노드만)
            Vector3 gravity = new Vector3(0, Gravity, 0);

            for (int i = 1; i < NodeCount - 1; i++)
            {
                Vector3 velocity = (_positions[i] - _previousPositions[i]) * Damping;
                _previousPositions[i] = _positions[i];
                _positions[i] = _positions[i] + velocity + gravity * deltaTime * deltaTime;
            }

            // 3. 거리 제약조건 (여러 번 반복하여 안정화)
            for (int iteration = 0; iteration < ConstraintIterations; iteration++)
            {
                ApplyDistanceConstraints();
            }

            // 4. 앵커 재고정
            _positions[0] = _startAnchor;
            _positions[NodeCount - 1] = _endAnchor;
        }

        /// <summary>
        /// 거리 제약조건 적용
        /// 각 노드 쌍이 목표 거리를 유지하도록 보정
        /// </summary>
        private void ApplyDistanceConstraints()
        {
            for (int i = 0; i < NodeCount - 1; i++)
            {
                Vector3 delta = _positions[i + 1] - _positions[i];
                float currentDistance = delta.magnitude;

                if (currentDistance < 0.0001f)
                {
                    continue;
                }

                float diff = (_segmentLength - currentDistance) / currentDistance;
                Vector3 offset = delta * 0.5f * diff;

                // 첫 노드는 앵커이므로 움직이지 않음
                if (i != 0)
                {
                    _positions[i] -= offset;
                }

                // 마지막 노드는 앵커이므로 움직이지 않음
                if (i != NodeCount - 2)
                {
                    _positions[i + 1] += offset;
                }
            }
        }

        /// <summary>
        /// 현재 노드 위치 배열 반환 (렌더링용)
        /// </summary>
        public List<Vector3> GetPositions()
        {
            if (!_isInitialized || _positions == null)
            {
                return new List<Vector3>();
            }

            return new List<Vector3>(_positions);
        }

        /// <summary>
        /// 초기화 여부 확인
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 시뮬레이션 리셋
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _positions = null;
            _previousPositions = null;
        }
    }
}
