using MergeDungeon.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeDungeon.Core
{
    public class LootBagTile : TileBase
    {
        private bool _initialized;

        public void Init(LootTable table)
        {
            if (def != null)
            {
                // prefer definition-based rolls
                int min = Mathf.Max(0, def.minRolls);
                int max = Mathf.Max(min, def.maxRolls);
                lootRemaining = Random.Range(min, max + 1);
            }
            else
            {
                // fallback: use provided table
                lootRemaining = table != null ? table.RollCount() : lootRemaining;
            }
            _initialized = true;
            RefreshVisual();
        }

        private void Start()
        {
            if (!_initialized && lootRemaining <= 0)
            {
                if (def != null)
                {
                    int min = Mathf.Max(0, def.minRolls);
                    int max = Mathf.Max(min, def.maxRolls);
                    lootRemaining = Random.Range(min, max + 1);
                }
                else
                {
                    // fallback attempt: cannot roll without a table, keep 0
                }
                RefreshVisual();
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData); // selection only
        }

        public override void OnActivateTap()
        {
            if (lootRemaining <= 0)
            {
                if (currentCell != null) currentCell.ClearTileIf(this);
                Destroy(gameObject);
                return;
            }

            bool spawned = false;
            if (def != null && def.lootTable != null)
            {
                var dropDef = def.lootTable.RollItemDefinition(GridManager.Instance != null ? GridManager.Instance.tileDatabase : null);
                if (dropDef != null && GridManager.Instance != null && GridManager.Instance.tileService != null)
                {
                    var empty = GridManager.Instance.CollectEmptyCells();
                    if (empty.Count > 0)
                    {
                        var cell = empty[Random.Range(0, empty.Count)];
                        var t = GridManager.Instance.tileFactory != null ? GridManager.Instance.tileFactory.Create(dropDef) : null;
                        if (t != null)
                        {
                            cell.SetTile(t);
                            spawned = true;
                        }
                    }
                }
            }
            else if (GridManager.Instance != null)
            {
                // legacy fallback path unavailable without lootTable; do nothing
            }

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
