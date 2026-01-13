using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 슬롯 데이터 모델
    /// 핀이 위치할 수 있는 고정된 위치를 정의합니다.
    /// </summary>
    [System.Serializable]
    public class SlotData
    {
        /// <summary>
        /// 슬롯 고유 ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 판정용 2D 좌표 (Logic 레이어에서 사용)
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 현재 이 슬롯을 점유하고 있는 핀의 ID
        /// -1이면 비어있음
        /// </summary>
        public int OccupiedByPinId = -1;

        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsEmpty => OccupiedByPinId == -1;

        /// <summary>
        /// 슬롯에 핀 점유 설정
        /// </summary>
        public void Occupy(int pinId)
        {
            OccupiedByPinId = pinId;
        }

        /// <summary>
        /// 슬롯 점유 해제
        /// </summary>
        public void Release()
        {
            OccupiedByPinId = -1;
        }

        public SlotData(int id, Vector2 position)
        {
            Id = id;
            Position = position;
            OccupiedByPinId = -1;
        }
    }
}
