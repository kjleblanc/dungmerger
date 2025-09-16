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
                var db = services != null ? services.TileDatabase : null;
                var dropDef = def.lootTable.RollItemDefinition(db);
                if (dropDef != null && services != null && services.Tiles != null)
                {
                    var empty = services.Board != null ? services.Board.CollectEmptyCells() : new System.Collections.Generic.List<BoardCell>();
                    if (empty.Count > 0)
                    {
                        var cell = empty[Random.Range(0, empty.Count)];
                        var t = services.TileFactory != null ? services.TileFactory.Create(dropDef) : null;
                        if (t != null)
                        {
                            cell.SetTile(t);
                            spawned = true;
                        }
                    }
                }
            }
            else if (services != null)
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
