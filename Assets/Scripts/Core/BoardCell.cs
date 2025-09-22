using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    public class BoardCell : MonoBehaviour
    {
        public int x;
        public int y;

        [Header("Occupants")]
        public TileBase tile; // current tile occupant

        [Header("Refs")]
        public RectTransform rectTransform;
        public Image bg;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (bg == null) bg = GetComponent<Image>();
        }

        private void OnValidate()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (bg == null) bg = GetComponent<Image>();
        }

        public bool IsEmpty()
        {
            return tile == null;
        }

        public bool IsFreeForTile()
        {
            return tile == null;
        }

        public void SetTile(TileBase next)
        {
            if (tile != null && tile != next && tile.currentCell == this)
            {
                tile.currentCell = null;
            }

            tile = next;
            if (next != null)
            {
                next.currentCell = this;
                var rt = next.GetComponent<RectTransform>();
                if (rt != null && rectTransform != null)
                {
                    rt.SetParent(rectTransform, worldPositionStays: false);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        public void ClearTileIf(TileBase target)
        {
            if (tile == target)
            {
                if (target != null && target.currentCell == this)
                {
                    target.currentCell = null;
                }
                tile = null;
            }
        }
    }
}
