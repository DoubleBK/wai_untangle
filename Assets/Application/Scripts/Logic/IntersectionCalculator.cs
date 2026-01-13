using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Logic
{
    /// <summary>
    /// 교차 판정 계산기 (Static 클래스)
    /// 순수 수학적 2D 기하학으로 로프 교차를 판정합니다.
    /// 물리 엔진을 사용하지 않아 결정론적 결과를 보장합니다.
    /// </summary>
    public static class IntersectionCalculator
    {
        private const float EPSILON = 1e-6f;

        /// <summary>
        /// 선분 AB와 선분 CD의 교차 여부 판정
        /// </summary>
        /// <param name="A">선분 1의 시작점</param>
        /// <param name="B">선분 1의 끝점</param>
        /// <param name="C">선분 2의 시작점</param>
        /// <param name="D">선분 2의 끝점</param>
        /// <param name="intersection">교차점 좌표 (out)</param>
        /// <returns>교차 여부</returns>
        public static bool SegmentIntersect(
            Vector2 A, Vector2 B, Vector2 C, Vector2 D,
            out Vector2 intersection)
        {
            intersection = Vector2.zero;

            Vector2 r = B - A;
            Vector2 s = D - C;
            float rxs = Cross(r, s);

            // 평행하거나 길이가 0인 경우
            if (Mathf.Abs(rxs) < EPSILON)
            {
                return false;
            }

            float t = Cross(C - A, s) / rxs;
            float u = Cross(C - A, r) / rxs;

            // t와 u가 [0,1] 범위 내에 있으면 교차
            // 끝점에서의 교차는 제외 (eps로 여유 부여)
            if (t > EPSILON && t < 1 - EPSILON && u > EPSILON && u < 1 - EPSILON)
            {
                intersection = A + t * r;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 2D 벡터의 외적 (Cross Product)
        /// </summary>
        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// 두 로프 간의 교차점 목록 반환
        /// </summary>
        public static List<IntersectionData> FindRopeIntersections(
            RopeData ropeA, RopeData ropeB,
            List<PinData> allPins)
        {
            var result = new List<IntersectionData>();

            // 같은 로프면 검사 안 함
            if (ropeA.Id == ropeB.Id) return result;

            // 로프 A의 핀들 좌표 획득
            PinData pinA0 = allPins.Find(p => p.Id == ropeA.StartPinId);
            PinData pinA1 = allPins.Find(p => p.Id == ropeA.EndPinId);

            // 로프 B의 핀들 좌표 획득
            PinData pinB0 = allPins.Find(p => p.Id == ropeB.StartPinId);
            PinData pinB1 = allPins.Find(p => p.Id == ropeB.EndPinId);

            if (pinA0 == null || pinA1 == null || pinB0 == null || pinB1 == null)
            {
                return result;
            }

            // 선분 교차 검사
            if (SegmentIntersect(pinA0.LogicPos, pinA1.LogicPos,
                                  pinB0.LogicPos, pinB1.LogicPos,
                                  out Vector2 point))
            {
                // RenderPriority가 높은 로프가 위
                int topRopeId = ropeA.RenderPriority >= ropeB.RenderPriority
                    ? ropeA.Id
                    : ropeB.Id;

                var intersection = new IntersectionData(
                    ropeA.Id,
                    ropeB.Id,
                    point,
                    topRopeId
                );

                result.Add(intersection);
            }

            return result;
        }

        /// <summary>
        /// 전체 로프 간 교차 계산
        /// O(N²) 복잡도, N < 50 이면 모바일에서 문제없음
        /// </summary>
        public static List<IntersectionData> CalculateAllIntersections(
            List<RopeData> ropes, List<PinData> pins)
        {
            var result = new List<IntersectionData>();

            // 모든 로프 쌍 검사
            for (int i = 0; i < ropes.Count; i++)
            {
                for (int j = i + 1; j < ropes.Count; j++)
                {
                    var crossings = FindRopeIntersections(ropes[i], ropes[j], pins);
                    result.AddRange(crossings);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 핀이 이동했을 때 드래그 프리뷰용 교차 수 계산
        /// 드래그 중인 로프만 검사하여 O(N) 최적화
        /// </summary>
        public static int CalculatePreviewIntersectionCount(
            PinData movedPin, Vector2 previewPos,
            List<RopeData> ropes, List<PinData> pins)
        {
            // 원래 위치 저장
            Vector2 originalPos = movedPin.LogicPos;

            // 임시로 프리뷰 위치 설정
            movedPin.LogicPos = previewPos;

            // 이동한 핀에 연결된 로프 찾기
            RopeData draggedRope = ropes.Find(r => r.Id == movedPin.RopeId);
            if (draggedRope == null)
            {
                movedPin.LogicPos = originalPos;
                return 0;
            }

            int count = 0;

            // 드래그 로프 vs 다른 모든 로프 검사
            foreach (var otherRope in ropes)
            {
                if (otherRope.Id == draggedRope.Id) continue;

                var crossings = FindRopeIntersections(draggedRope, otherRope, pins);
                count += crossings.Count;
            }

            // 원래 위치 복원
            movedPin.LogicPos = originalPos;

            return count;
        }
    }
}
