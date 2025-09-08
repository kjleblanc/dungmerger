using MergeDungeon.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeDungeon.Core
{
    public class LootBagTile : TileBase
    {
        public LootTable lootTable;
        private bool _initialized;

        public void Init(LootTable table)
        {
            lootTable = table;
            lootRemaining = lootTable != null ? lootTable.RollCount() : lootRemaining;
            _initialized = true;
            RefreshVisual();
        }

        private void Start()
        {
            if (!_initialized && lootTable != null && lootRemaining <= 0)
            {
                lootRemaining = lootTable.RollCount();
                RefreshVisual();
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData); // selection only
        }

        public override void OnActivateTap()
        {
            if (lootTable == null) return;
            if (lootRemaining <= 0)
            {
                if (currentCell != null) currentCell.ClearTileIf(this);
                Destroy(gameObject);
                return;
            }

            var drop = lootTable.RollItem();
            bool spawned = GridManager.Instance.TrySpawnTileAtRandom(drop);

            if (spawned)
            {
                lootRemaining = Mathf.Max(0, lootRemaining - 1);
                RefreshVisual();
                if (lootRemaining <= 0)
                {
                    if (currentCell != null) currentCell.ClearTileIf(this);
                    Destroy(gameObject);
                }
            }
        }
    }
}
