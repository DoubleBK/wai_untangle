using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// 스플라인 보간기
    /// Catmull-Rom Spline으로 제어점을 지나가는 부드러운 곡선을 생성합니다.
    /// </summary>
    public static class SplineInterpolator
    {
        /// <summary>
        /// Catmull-Rom Spline 보간
        /// 제어점들을 지나가는 부드러운 곡선의 포인트 목록을 반환합니다.
        /// </summary>
        /// <param name="controlPoints">제어점 목록 (최소 2개)</param>
        /// <param name="samplesPerSegment">세그먼트당 샘플 수 (높을수록 부드러움)</param>
        /// <returns>보간된 경로 포인트 목록</returns>
        public static List<Vector3> InterpolateCatmullRom(
            List<Vector3> controlPoints,
            int samplesPerSegment = 10)
        {
            var result = new List<Vector3>();

            if (controlPoints == null || controlPoints.Count < 2)
            {
                return result;
            }

            // 2개 점이면 직선
            if (controlPoints.Count == 2)
            {
                result.Add(controlPoints[0]);
                result.Add(controlPoints[1]);
                return result;
            }

            // 각 세그먼트 보간
            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                // Catmull-Rom은 4개 점 필요 (앞뒤로 확장)
                Vector3 p0 = controlPoints[Mathf.Max(0, i - 1)];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[i + 1];
                Vector3 p3 = controlPoints[Mathf.Min(controlPoints.Count - 1, i + 2)];

                // 세그먼트 내 샘플링
                for (int j = 0; j < samplesPerSegment; j++)
                {
                    float t = j / (float)samplesPerSegment;
                    result.Add(CatmullRomPoint(p0, p1, p2, p3, t));
                }
            }

            // 마지막 점 추가
            result.Add(controlPoints[controlPoints.Count - 1]);

            return result;
        }

        /// <summary>
        /// Catmull-Rom 단일 점 보간
        /// </summary>
        /// <param name="p0">이전 제어점</param>
        /// <param name="p1">현재 세그먼트 시작점</param>
        /// <param name="p2">현재 세그먼트 끝점</param>
        /// <param name="p3">다음 제어점</param>
        /// <param name="t">보간 파라미터 [0, 1]</param>
        /// <returns>보간된 점</returns>
        public static Vector3 CatmullRomPoint(
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            // Catmull-Rom 공식
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// 경로의 총 길이 계산
        /// </summary>
        public static float CalculatePathLength(List<Vector3> path)
        {
            if (path == null || path.Count < 2) return 0f;

            float length = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                length += Vector3.Distance(path[i], path[i + 1]);
            }
            return length;
        }

        /// <summary>
        /// 경로를 균등한 거리로 리샘플링
        /// UV 매핑에 필요한 균일 분포 생성
        /// </summary>
        /// <param name="path">원본 경로</param>
        /// <param name="segmentLength">세그먼트 길이</param>
        /// <returns>리샘플링된 경로</returns>
        public static List<Vector3> ResamplePath(List<Vector3> path, float segmentLength)
        {
            var result = new List<Vector3>();

            if (path == null || path.Count < 2 || segmentLength <= 0)
            {
                return result;
            }

            result.Add(path[0]);

            float accumulatedLength = 0f;
            int currentIndex = 0;

            while (currentIndex < path.Count - 1)
            {
                Vector3 currentPoint = path[currentIndex];
                Vector3 nextPoint = path[currentIndex + 1];
                float segmentDist = Vector3.Distance(currentPoint, nextPoint);

                if (accumulatedLength + segmentDist >= segmentLength)
                {
                    // 새 포인트 삽입
                    float remaining = segmentLength - accumulatedLength;
                    float t = remaining / segmentDist;
                    Vector3 newPoint = Vector3.Lerp(currentPoint, nextPoint, t);
                    result.Add(newPoint);

                    // 현재 위치 업데이트
                    path[currentIndex] = newPoint;
                    accumulatedLength = 0f;
                }
                else
                {
                    accumulatedLength += segmentDist;
                    currentIndex++;
                }
            }

            // 마지막 점 추가
            if (result[result.Count - 1] != path[path.Count - 1])
            {
                result.Add(path[path.Count - 1]);
            }

            return result;
        }
    }
}
