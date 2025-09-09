using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    public class RoomMapUI : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text currentLabel;
        public TMP_Text nextLabel;
        public Image currentSquare;
        public Image nextSquare;
        public RoomChangedEventChannelSO roomChanged;

        [Header("Colors")]
        public Color currentColor = new Color(0.3f, 0.85f, 1f, 1f);
        public Color nextColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        public Color bossColor = new Color(1f, 0.5f, 0.5f, 1f);

        private void OnEnable()
        {
            if (roomChanged != null) roomChanged.Raised += Set;
        }

        private void OnDisable()
        {
            if (roomChanged != null) roomChanged.Raised -= Set;
        }

        public void Set(int floor, int room, int roomsPerFloor)
        {
            bool currentIsBoss = (room == roomsPerFloor);
            bool nextIsBoss = (!currentIsBoss && (room + 1 == roomsPerFloor));
            int nextFloor = currentIsBoss ? (floor + 1) : floor;
            int nextRoom = currentIsBoss ? 1 : (room + 1);

            if (currentLabel != null)
            {
                currentLabel.text = $"F{floor}-R{room}";
            }
            if (nextLabel != null)
            {
                nextLabel.text = nextIsBoss ? "Boss" : $"F{nextFloor}-R{nextRoom}";
            }
            if (currentSquare != null)
            {
                currentSquare.color = currentColor;
            }
            if (nextSquare != null)
            {
                nextSquare.color = nextIsBoss ? bossColor : nextColor;
            }
        }
    }
}

