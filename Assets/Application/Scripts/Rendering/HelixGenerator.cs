using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// 나선형(Helix) 경로 생성기
    /// 교차점에서 TopRope가 BottomRope를 감싸는 나선형 경로를 생성합니다.
    /// </summary>
    public static class HelixGenerator
    {
        // ========== 기본 설정값 ==========
        public const float DefaultHeight = 0.8f;       // Z축 상승 높이 (더 크게)
        public const int DefaultWrapCount = 1;         // 감김 횟수
        public const int DefaultSamples = 20;          // 보간 샘플 수
        public const float DefaultRadiusMargin = 0.15f; // 튜브 반경 마진 (더 크게)
        public const float DefaultProgressLength = 0.8f; // 진행 방향 길이 (더 길게)

        /// <summary>
        /// 교차점 주변 helix 경로 생성
        /// </summary>
        /// <param name="intersectionPoint">교차점 위치 (2D 평면 기준)</param>
        /// <param name="ropeDirection">현재 로프 진행 방향 (정규화됨)</param>
        /// <param name="tubeRadius">튜브 반지름</param>
        /// <param name="height">Z축 상승 높이</param>
        /// <param name="wrapCount">감김 횟수</param>
        /// <param name="samples">보간 샘플 수</param>
        /// <returns>나선형 경로 포인트 목록</returns>
        public static List<Vector3> GenerateHelixPath(
            Vector3 intersectionPoint,
            Vector3 ropeDirection,
            float tubeRadius,
            float height = DefaultHeight,
            int wrapCount = DefaultWrapCount,
            int samples = DefaultSamples)
        {
            var result = new List<Vector3>();

            // 방향이 0이면 기본값 사용
            if (ropeDirection.sqrMagnitude < 0.001f)
            {
                ropeDirection = Vector3.right;
            }
            ropeDirection = ropeDirection.normalized;

            // 로프 방향에 수직인 두 벡터 계산
            Vector3 up = Vector3.forward; // Unity에서 Z-up 대신 카메라가 -Z를 바라보므로 forward 사용
            Vector3 right = Vector3.Cross(ropeDirection, up);

            // 만약 ropeDirection이 up과 평행하면 다른 축 사용
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(ropeDirection, Vector3.up);
            }
            right = right.normalized;

            Vector3 perpUp = Vector3.Cross(right, ropeDirection).normalized;

            // 나선 반지름 (튜브 + 마진)
            float helixRadius = tubeRadius + DefaultRadiusMargin;

            // 진행 방향 전체 길이
            float progressLength = DefaultProgressLength;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;

                // 각도: wrapCount 바퀴 회전
                float angle = t * wrapCount * Mathf.PI * 2f;

                // Z축 상승: 사인 곡선 (0 → peak → 0)
                float zLift = height * Mathf.Sin(t * Mathf.PI);

                // 원형 궤도 위치
                Vector3 radialOffset = Mathf.Cos(angle) * right + Mathf.Sin(angle) * perpUp;

                // 진행 방향으로의 오프셋 (나선이 로프 방향으로 늘어지도록)
                float progressOffset = (t - 0.5f) * progressLength;

                // 최종 포인트: 중심 + 진행방향 오프셋 + 궤도 + 높이
                // 카메라가 -Z를 바라보므로, -Z 방향(카메라 쪽)으로 올라가야 보임
                Vector3 point = intersectionPoint
                    + ropeDirection * progressOffset
                    + radialOffset * helixRadius
                    + new Vector3(0, 0, -zLift); // 카메라 쪽으로 올라감

                result.Add(point);
            }

            return result;
        }

        /// <summary>
        /// 간단한 아치형 경로 생성 (helix 대신 사용 가능)
        /// 교차점에서 위로 올라갔다 내려오는 부드러운 아치
        /// </summary>
        public static List<Vector3> GenerateArchPath(
            Vector3 intersectionPoint,
            Vector3 ropeDirection,
            float height = DefaultHeight,
            float length = 0.5f,
            int samples = 10)
        {
            var result = new List<Vector3>();

            if (ropeDirection.sqrMagnitude < 0.001f)
            {
                ropeDirection = Vector3.right;
            }
            ropeDirection = ropeDirection.normalized;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;

                // 진행 방향 오프셋
                float progressOffset = (t - 0.5f) * length;

                // Z축 상승: 사인 곡선
                float zLift = height * Mathf.Sin(t * Mathf.PI);

                Vector3 point = intersectionPoint
                    + ropeDirection * progressOffset
                    + new Vector3(0, 0, -zLift); // 카메라 쪽으로 올라감

                result.Add(point);
            }

            return result;
        }

        /// <summary>
        /// Helix 파라미터 유효성 검사 및 보정
        /// </summary>
        public static void ValidateParameters(
            ref float height,
            ref int wrapCount,
            ref int samples)
        {
            if (height <= 0) height = DefaultHeight;
            if (wrapCount < 1) wrapCount = 1;
            if (samples < 5) samples = 5;
            if (samples > 100) samples = 100;
        }
    }
}
