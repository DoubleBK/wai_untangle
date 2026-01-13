using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 교차점 데이터 모델
    /// 두 로프가 교차하는 지점의 정보를 저장합니다.
    /// </summary>
    [System.Serializable]
    public class IntersectionData
    {
        /// <summary>
        /// 첫 번째 로프 ID
        /// </summary>
        public int RopeAId;

        /// <summary>
        /// 두 번째 로프 ID
        /// </summary>
        public int RopeBId;

        /// <summary>
        /// 로프 A에서 교차가 발생한 세그먼트 인덱스
        /// MVP에서는 로프가 2개 핀만 연결하므로 항상 0
        /// </summary>
        public int SegmentAIndex;

        /// <summary>
        /// 로프 B에서 교차가 발생한 세그먼트 인덱스
        /// </summary>
        public int SegmentBIndex;

        /// <summary>
        /// 교차점의 2D 좌표
        /// </summary>
        public Vector2 Point;

        /// <summary>
        /// 교차점에서 위에 있는 로프의 ID
        /// RenderPriority가 높은 로프가 위
        /// </summary>
        public int TopRopeId;

        /// <summary>
        /// 교차점에서 아래에 있는 로프의 ID
        /// </summary>
        public int BottomRopeId => TopRopeId == RopeAId ? RopeBId : RopeAId;

        /// <summary>
        /// 두 로프가 같은 로프인지 확인
        /// </summary>
        public bool IsSameRope => RopeAId == RopeBId;

        public IntersectionData(int ropeAId, int ropeBId, Vector2 point, int topRopeId)
        {
            RopeAId = ropeAId;
            RopeBId = ropeBId;
            SegmentAIndex = 0;
            SegmentBIndex = 0;
            Point = point;
            TopRopeId = topRopeId;
        }
    }
}
