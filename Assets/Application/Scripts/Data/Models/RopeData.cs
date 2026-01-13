using System.Collections.Generic;
using UnityEngine;

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
    }
}
