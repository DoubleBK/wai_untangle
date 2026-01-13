using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Rendering;

namespace Game.Data
{
    /// <summary>
    /// 로프 데이터 모델
    /// 두 개 이상의 핀을 연결하는 선을 정의합니다.
    /// </summary>
    [System.Serializable]
    public class RopeData
    {
        /// <summary>
        /// 로프 고유 ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 로프 색상
        /// </summary>
        public Color RopeColor;

        /// <summary>
        /// 연결된 핀 ID 목록
        /// MVP에서는 2개 (양 끝점)
        /// </summary>
        public List<int> PinIds;

        /// <summary>
        /// 렌더링 우선순위 (Z 정렬)
        /// 값이 높을수록 위에 표시됨
        /// 교차점에서 Top/Bottom 결정에 사용
        /// </summary>
        public int RenderPriority;

        /// <summary>
        /// 튜브 메시 생성용 3D 경로
        /// Helix 포인트가 삽입된 최종 렌더링 경로
        /// </summary>
        public List<Vector3> RenderPath;

        /// <summary>
        /// 로프의 첫 번째 핀 ID
        /// </summary>
        public int StartPinId => PinIds != null && PinIds.Count > 0 ? PinIds[0] : -1;

        /// <summary>
        /// 로프의 마지막 핀 ID
        /// </summary>
        public int EndPinId => PinIds != null && PinIds.Count > 1 ? PinIds[PinIds.Count - 1] : -1;

        /// <summary>
        /// 렌더링 경로 초기화 (핀 위치 기반)
        /// </summary>
        public void InitializeRenderPath(List<PinData> allPins)
        {
            RenderPath = new List<Vector3>();

            foreach (int pinId in PinIds)
            {
                PinData pin = allPins.Find(p => p.Id == pinId);
                if (pin != null)
                {
                    RenderPath.Add(pin.WorldPos);
                }
            }
        }

        /// <summary>
        /// 특정 핀의 위치가 변경되었을 때 경로 업데이트
        /// </summary>
        public void UpdatePinPositionInPath(int pinId, Vector3 newPosition, List<PinData> allPins)
        {
            int index = PinIds.IndexOf(pinId);
            if (index >= 0 && index < RenderPath.Count)
            {
                RenderPath[index] = newPosition;
            }
        }

        public RopeData(int id, Color color, List<int> pinIds, int renderPriority = 0)
        {
            Id = id;
            RopeColor = color;
            PinIds = pinIds ?? new List<int>();
            RenderPriority = renderPriority;
            RenderPath = new List<Vector3>();
        }

        // ========== Helix 관련 메서드 ==========

        /// <summary>
        /// 교차점에 Helix 포인트 적용
        /// TopRope만 helix를 적용하며, BottomRope는 직선 유지
        /// </summary>
        public void ApplyHelixAtIntersections(
            List<IntersectionData> intersections,
            List<PinData> allPins)
        {
            // 1. 기본 경로 생성 (핀 위치 기반)
            InitializeRenderPath(allPins);

            if (intersections == null || intersections.Count == 0)
            {
                return;
            }

            // 2. 이 로프가 TopRope인 교차점 찾기
            var myIntersections = intersections
                .Where(i => i.TopRopeId == this.Id)
                .OrderBy(i => DistanceAlongPath(i.Point))
                .ToList();

            if (myIntersections.Count == 0)
            {
                return;
            }

            // 3. 각 교차점에 helix 포인트 삽입 (역순으로 처리해야 인덱스가 안 밀림)
            for (int i = myIntersections.Count - 1; i >= 0; i--)
            {
                var intersection = myIntersections[i];
                Vector3 intersectionPoint3D = new Vector3(intersection.Point.x, intersection.Point.y, 0);

                // 로프 진행 방향 계산
                Vector3 direction = GetDirectionAtPoint(intersection.Point);

                // Helix 포인트 생성
                var helixPoints = HelixGenerator.GenerateHelixPath(
                    intersectionPoint3D,
                    direction,
                    0.08f // 튜브 반지름 (TubeMeshGenerator 기본값)
                );

                // 경로에 helix 포인트 삽입
                InsertHelixPointsAtIntersection(intersection.Point, helixPoints);
            }
        }

        /// <summary>
        /// 로프 경로 시작점에서 주어진 점까지의 거리 계산
        /// 교차점 정렬용
        /// </summary>
        private float DistanceAlongPath(Vector2 point)
        {
            if (RenderPath == null || RenderPath.Count < 2)
            {
                return 0f;
            }

            Vector2 start = new Vector2(RenderPath[0].x, RenderPath[0].y);
            return Vector2.Distance(start, point);
        }

        /// <summary>
        /// 주어진 점에서의 로프 진행 방향 계산
        /// </summary>
        private Vector3 GetDirectionAtPoint(Vector2 point)
        {
            if (RenderPath == null || RenderPath.Count < 2)
            {
                return Vector3.right;
            }

            // 간단하게 첫 점에서 마지막 점으로의 방향 사용
            Vector3 start = RenderPath[0];
            Vector3 end = RenderPath[RenderPath.Count - 1];

            Vector3 direction = (end - start).normalized;

            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector3.right;
            }

            return direction;
        }

        /// <summary>
        /// 교차점 위치에 helix 포인트들을 삽입
        /// </summary>
        private void InsertHelixPointsAtIntersection(Vector2 intersectionPoint, List<Vector3> helixPoints)
        {
            if (RenderPath == null || RenderPath.Count < 2 || helixPoints == null || helixPoints.Count == 0)
            {
                return;
            }

            // 교차점과 가장 가까운 세그먼트 찾기
            int insertIndex = FindClosestSegmentIndex(intersectionPoint);

            if (insertIndex < 0)
            {
                return;
            }

            // 기존 경로에서 교차점 근처의 직선을 helix로 대체
            // insertIndex 위치에 helix 포인트들을 삽입
            RenderPath.InsertRange(insertIndex + 1, helixPoints);
        }

        /// <summary>
        /// 교차점과 가장 가까운 세그먼트의 시작 인덱스 반환
        /// </summary>
        private int FindClosestSegmentIndex(Vector2 point)
        {
            if (RenderPath == null || RenderPath.Count < 2)
            {
                return -1;
            }

            float minDist = float.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < RenderPath.Count - 1; i++)
            {
                Vector2 segStart = new Vector2(RenderPath[i].x, RenderPath[i].y);
                Vector2 segEnd = new Vector2(RenderPath[i + 1].x, RenderPath[i + 1].y);

                // 점과 선분 사이 거리 계산
                float dist = PointToSegmentDistance(point, segStart, segEnd);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        /// <summary>
        /// 점과 선분 사이의 최단 거리 계산
        /// </summary>
        private float PointToSegmentDistance(Vector2 point, Vector2 segStart, Vector2 segEnd)
        {
            Vector2 seg = segEnd - segStart;
            float segLengthSq = seg.sqrMagnitude;

            if (segLengthSq < 0.0001f)
            {
                return Vector2.Distance(point, segStart);
            }

            // 선분 위의 가장 가까운 점의 t 파라미터 계산
            float t = Mathf.Clamp01(Vector2.Dot(point - segStart, seg) / segLengthSq);

            // 선분 위의 가장 가까운 점
            Vector2 closest = segStart + t * seg;

            return Vector2.Distance(point, closest);
        }
    }
}
