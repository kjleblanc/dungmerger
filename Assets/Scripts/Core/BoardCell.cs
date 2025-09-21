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
        public EnemyController enemy; // current enemy occupant
        public HeroController hero;   // current hero occupant

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
            return tile == null && enemy == null && hero == null;
        }

        public bool IsFreeForTile()
        {
            return tile == null && enemy == null && hero == null;
        }

        public void SetTile(TileBase t)
        {
            tile = t;
            if (t != null)
            {
                t.currentCell = this;
                var rt = t.GetComponent<RectTransform>();
                rt.SetParent(rectTransform, worldPositionStays: false);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        public void ClearTileIf(TileBase t)
        {
            if (tile == t)
            {
                tile = null;
            }
        }

        public void SetEnemy(EnemyController e)
        {
            enemy = e;
            if (e != null)
            {
                var rt = e.GetComponent<RectTransform>();
                rt.SetParent(rectTransform, worldPositionStays: false);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        public void ClearEnemyIf(EnemyController e)
        {
            if (enemy == e)
            {
                enemy = null;
            }
        }

        public void SetHero(HeroController h)
        {
            hero = h;
            if (h != null)
            {
                var rt = h.GetComponent<RectTransform>();
                rt.SetParent(rectTransform, worldPositionStays: false);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        public void ClearHeroIf(HeroController h)
        {
            if (hero == h)
            {
                hero = null;
            }
        }
    }
}
