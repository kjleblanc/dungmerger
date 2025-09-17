using MergeDungeon.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeDungeon.Core
{
    public class LootBagTile : TileBase
    {
        private bool _initialized;
        [SerializeField] private LootContainerDefinition containerDefinition;

        public void Init(LootContainerDefinition container)
        {
            containerDefinition = container;
            lootRemaining = Mathf.Max(0, RollInitialLootCount());
            _initialized = true;
            RefreshVisual();
        }

        private int RollInitialLootCount()
        {
            if (containerDefinition != null)
            {
                return containerDefinition.RollCount();
            }

            return lootRemaining > 0 ? lootRemaining : 1;
        }

        private LootTable ResolveLootTable()
        {
            return containerDefinition != null ? containerDefinition.lootTable : null;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            lootRemaining = Mathf.Max(0, RollInitialLootCount());
            _initialized = true;
            RefreshVisual();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData); // selection only
        }

        public override void OnActivateTap()
        {
            EnsureInitialized();

            if (lootRemaining <= 0)
            {
                if (currentCell != null) currentCell.ClearTileIf(this);
                Destroy(gameObject);
                return;
            }

            var table = ResolveLootTable();
            if (table == null || services == null)
            {
                return;
            }

            bool spawned = false;
            var db = services.TileDatabase;
            var tiles = services.TileFactory;
            var board = services.Board;

            if (board != null && tiles != null)
            {
                var empty = board.CollectEmptyCells();
                if (empty != null && empty.Count > 0)
                {
                    var cell = empty[Random.Range(0, empty.Count)];
                    var dropDef = table.RollItemDefinition(db);
                    if (dropDef != null)
                    {
                        var t = tiles.Create(dropDef);
                        if (t != null)
                        {
                            cell.SetTile(t);
                            spawned = true;
                        }
                    }
                }
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
