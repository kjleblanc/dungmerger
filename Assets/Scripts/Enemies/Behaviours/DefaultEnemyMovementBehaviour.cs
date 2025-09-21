using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Turn/Default Action", fileName = "EnemyTurn_Default")]
    public class DefaultEnemyMovementBehaviour : EnemyTurnBehaviour
    {
        [Tooltip("If true, prioritise attacking the hero whose slot shares this enemy's preferred column.")]
        public bool requireColumnMatchForAttack = false;
        [Tooltip("Falls back to spawning the enemy's tile definition onto the board if no hero is available.")]
        public bool enableBoardDisruptionFallback = true;

        public override void ExecuteTurn(EnemyController controller, EnemyTurnContext context)
        {
            if (controller == null)
            {
                return;
            }

            var targetHero = SelectHeroTarget(context);
            if (targetHero != null)
            {
                controller.AttackHero(targetHero, ResolveDamage(controller));
                return;
            }

            if (enableBoardDisruptionFallback)
            {
                TryDisruptBoard(controller, context);
            }
        }

        protected virtual int ResolveDamage(EnemyController controller)
        {
            if (controller == null)
            {
                return 1;
            }

            var definition = controller.Definition;
            int baseDamage = definition != null ? definition.baseDamage : 1;
            return Mathf.Max(1, baseDamage);
        }

        protected virtual HeroController SelectHeroTarget(EnemyTurnContext context)
        {
            var heroBench = context.HeroBench;
            if (heroBench == null)
            {
                return null;
            }

            var slots = heroBench.Slots;
            if (slots == null)
            {
                return null;
            }

            int preferredColumn = context.Slot.HasValue ? context.Slot.Value.preferredBoardColumn : -1;
            HeroController fallback = null;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.occupant == null)
                {
                    continue;
                }

                if (preferredColumn >= 0 && slot.preferredBoardColumn == preferredColumn)
                {
                    return slot.occupant;
                }

                if (!requireColumnMatchForAttack && fallback == null)
                {
                    fallback = slot.occupant;
                }
            }

            return fallback;
        }

        protected virtual void TryDisruptBoard(EnemyController controller, EnemyTurnContext context)
        {
            if (controller == null)
            {
                return;
            }

            var board = context.Board;
            var services = context.Services;
            var tileFactory = services != null ? services.TileFactory : null;
            var tileDefinition = controller.Definition != null ? controller.Definition.enemyTile : null;
            if (board == null || tileFactory == null || tileDefinition == null)
            {
                return;
            }

            var tile = tileFactory.Create(tileDefinition);
            if (tile == null)
            {
                return;
            }

            int column = DetermineColumn(context, board.width);
            int row = DetermineRow(context, board.height);

            var cell = board.GetCell(column, row);
            if (cell == null)
            {
                UnityEngine.Object.Destroy(tile.gameObject);
                return;
            }

            if (!cell.IsFreeForTile())
            {
                if (cell.tile != null)
                {
                    UnityEngine.Object.Destroy(cell.tile.gameObject);
                    cell.tile = null;
                }

                if (!cell.IsFreeForTile())
                {
                    UnityEngine.Object.Destroy(tile.gameObject);
                    return;
                }
            }

            cell.SetTile(tile);
        }

        protected int DetermineColumn(EnemyTurnContext context, int boardWidth)
        {
            int preferredColumn = context.Slot.HasValue ? context.Slot.Value.preferredBoardColumn : -1;
            if (preferredColumn >= 0)
            {
                return Mathf.Clamp(preferredColumn, 0, Mathf.Max(0, boardWidth - 1));
            }

            if (boardWidth <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(boardWidth / 2, 0, Mathf.Max(0, boardWidth - 1));
        }

        protected int DetermineRow(EnemyTurnContext context, int boardHeight)
        {
            if (boardHeight <= 0)
            {
                return 0;
            }

            int topRow = Mathf.Max(0, boardHeight - 1);
            int offset = context.Slot.HasValue ? Mathf.Max(0, context.Slot.Value.targetRowOffsetFromTop) : 0;
            return Mathf.Clamp(topRow - offset, 0, topRow);
        }
    }
}
