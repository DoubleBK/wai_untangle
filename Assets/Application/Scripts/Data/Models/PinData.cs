using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 핀 데이터 모델
    /// 로프의 끝점이 되며 사용자가 드래그하여 이동시키는 객체입니다.
    /// </summary>
    [System.Serializable]
    public class PinData
    {
        /// <summary>
        /// 핀 고유 ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 현재 위치한 슬롯의 인덱스
        /// </summary>
        public int SlotIndex;

        /// <summary>
        /// 연결된 로프의 ID
        /// </summary>
        public int RopeId;

        /// <summary>
        /// 판정용 2D 좌표 (Slot.Position 캐시)
        /// Logic 레이어에서 교차 판정에 사용
        /// </summary>
        public Vector2 LogicPos;

        /// <summary>
        /// 렌더링용 3D 좌표
        /// Rendering 레이어에서 비주얼 표시에 사용
        /// </summary>
        public Vector3 WorldPos;

        /// <summary>
        /// 슬롯 위치로 LogicPos와 WorldPos 동기화
        /// </summary>
        public void SyncPositionFromSlot(SlotData slot)
        {
            LogicPos = slot.Position;
            WorldPos = new Vector3(slot.Position.x, slot.Position.y, 0);
        }

        /// <summary>
        /// 드래그 중 위치 임시 업데이트 (프리뷰용)
        /// </summary>
        public void SetPreviewPosition(Vector2 position)
        {
            LogicPos = position;
            WorldPos = new Vector3(position.x, position.y, WorldPos.z);
        }

        public PinData(int id, int slotIndex, int ropeId)
        {
            Id = id;
            SlotIndex = slotIndex;
            RopeId = ropeId;
        }
    }
}
