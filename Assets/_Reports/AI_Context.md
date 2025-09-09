# AI Review Context: MergeDungeon

## Project facts
- **Company:** DefaultCompany
- **Unity:** 6000.2.0b2
- **Active Build Target:** StandaloneWindows64
- **Render Pipeline:** URP
- **Input:** New Input System
- **Platforms:** Android, PC (Windows)
- **Networking:** None (single-player)
- **Save System:** Not implemented (needs design)
- **Performance focus:** Mobile-first (steady 60 FPS, low memory)

## Scenes (enabled in Build Settings)
### Assets/Scenes/Bootstrap.unity
- **Total GameObjects:** 31
- **Hierarchy (depth-limited):**
- **Main Camera** (active:True, layer:Default, tag:MainCamera) — [Transform, Camera, AudioListener, UniversalAdditionalCameraData]
- **Global Light 2D** (active:True, layer:Default, tag:Untagged) — [Transform, Light2D]
- **Canvas** (active:True, layer:UI, tag:Untagged) — [RectTransform, Canvas, CanvasScaler, GraphicRaycaster]
  - **FXLayer** (active:True, layer:UI, tag:Untagged) — [RectTransform]
  - **DragLayer** (active:True, layer:UI, tag:Untagged) — [RectTransform]
  - **Board** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image, GridLayoutGroup]
  - **Map** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image, RoomMapUI]
    - **Next** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image]
      - **NextRoom** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, TextMeshProUGUI]
    - **Current** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image]
      - **CurrentRoom** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, TextMeshProUGUI]
  - **Meter** (active:True, layer:UI, tag:Untagged) — [RectTransform]
    - **AdvanceMeter** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image]
      - **Fill** (active:True, layer:UI, tag:Untagged) — [RectTransform, CanvasRenderer, Image]
- **EventSystem** (active:True, layer:Default, tag:Untagged) — [Transform, EventSystem, InputSystemUIInputModule]
- **Game** (active:True, layer:Default, tag:Untagged) — [Transform, GridManager, DungeonManager, AdvanceMeterController, EnemyMover, BoardController, EnemySpawner, TileService]
- **UIManager** (active:True, layer:Default, tag:Untagged) — [Transform, UISelectionManager]
- **Systems** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **BoardSystem (grid/merge/inventory)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **DungeonSystem (rooms/waves/loot)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **EnemySystem (AI/stepping/state)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **ProgressionSystem (meter/xp)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **UISystem (HUD, map, popups)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **Addressables/PoolHost** (active:True, layer:Default, tag:Untagged) — [Transform]
- **Messaging** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **EventChannels (SO)** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **Commands/Queries (C# interfaces)** (active:True, layer:Default, tag:Untagged) — [Transform]
- **Services** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **SaveService** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **Time/Random/Config** (active:True, layer:Default, tag:Untagged) — [Transform]
  - **Audio/VFX** (active:True, layer:Default, tag:Untagged) — [Transform]

## God-object candidates (high component count / named "Game")
- **Game** — `Game` in `Assets/Scenes/Bootstrap.unity`
  - Components: Transform, GridManager, DungeonManager, AdvanceMeterController, EnemyMover, BoardController, EnemySpawner, TileService
  - Suggested system buckets: BoardSystem, DungeonSystem, EnemySystem, ProgressionSystem

## Prefabs
- **Total prefabs:** 39
- **Gameplay prefabs (name starts with `PF_`):**
  - `PF_BoardCell` — Assets/Prefabs/PF_BoardCell.prefab — [RectTransform, CanvasRenderer, Image, BoardCell]
  - `PF_CraftingStationTile` — Assets/Prefabs/PF_CraftingStationTile.prefab — [RectTransform, CanvasRenderer, Image, CanvasGroup, TileBase, CraftingStationTile]
  - `PF_DamagePopup` — Assets/Prefabs/PF_DamagePopup.prefab — [RectTransform, DamagePopup]
  - `PF_Enemy` — Assets/Prefabs/PF_Enemy.prefab — [RectTransform, CanvasRenderer, Image, EnemyController]
  - `PF_Hero` — Assets/Prefabs/PF_Hero.prefab — [RectTransform, CanvasRenderer, HeroController, CanvasGroup]
  - `PF_LootBagTile` — Assets/Prefabs/PF_LootBagTile.prefab — [RectTransform, CanvasRenderer, Image, CanvasGroup, LootBagTile]
  - `PF_Tile` — Assets/Prefabs/PF_Tile.prefab — [RectTransform, CanvasRenderer, Image, CanvasGroup, TileBase]
- **Top prefabs by component count:**
  - `DebugUIPanel` (7) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIPanel.prefab
  - `DebugUIMessageBox` (6) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIMessageBox.prefab
  - `PF_CraftingStationTile` (6) — Assets/Prefabs/PF_CraftingStationTile.prefab
  - `DebugUIBitField` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIBitField.prefab
  - `DebugUIButton` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIButton.prefab
  - `DebugUICanvas` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Runtime UI Resources/DebugUICanvas.prefab
  - `DebugUIColor` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIColor.prefab
  - `DebugUIFoldout` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIFoldout.prefab
  - `DebugUIGroup` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIGroup.prefab
  - `DebugUIHandlerRenderingLayerField` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIHandlerRenderingLayerField.prefab
  - `DebugUIHBox` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIHBox.prefab
  - `DebugUIPersistentCanvas` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Runtime UI Resources/DebugUIPersistentCanvas.prefab
  - `DebugUIRow` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIRow.prefab
  - `DebugUIVBox` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIVBox.prefab
  - `DebugUIVector2` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIVector2.prefab
  - `DebugUIVector3` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIVector3.prefab
  - `DebugUIVector4` (5) — Packages/com.unity.render-pipelines.core/Runtime/Debugging/Prefabs/Widgets/DebugUIVector4.prefab
  - `PF_LootBagTile` (5) — Assets/Prefabs/PF_LootBagTile.prefab
  - `PF_Tile` (5) — Assets/Prefabs/PF_Tile.prefab
  - `PF_BoardCell` (4) — Assets/Prefabs/PF_BoardCell.prefab

## ScriptableObjects
- **Total ScriptableObject assets:** 243
- **Top types:**
  - `UnityEngine.UIElements.StyleSheet` — 128
  - `UnityEngine.UIElements.VisualTreeAsset` — 68
  - `UnityEditor.ShaderGraph.SubGraphAsset` — 8
  - `UnityEngine.Rendering.VolumeProfile` — 3
  - `UnityEditor.SceneTemplate.SceneTemplateAsset` — 3
  - `UnityEngine.Rendering.Universal.PostProcessData` — 3
  - `UnityEngine.InputSystem.InputActionAsset` — 2
  - `MergeDungeon.Core.LootTable` — 2
  - `MergeDungeon.Core.AbilitySpawnTable` — 2
  - `TMPro.TMP_FontAsset` — 2
  - `MergeDungeon.Core.CraftingBook` — 1
  - `MergeDungeon.Core.CraftingRecipe` — 1
  - `MergeDungeon.Core.AbilityConfig` — 1
  - `MergeDungeon.Core.EnemyDatabase` — 1
  - `MergeDungeon.Core.EnemyWave` — 1
  - `MergeDungeon.Core.FloorRoomPool` — 1
  - `MergeDungeon.Core.MergeRules` — 1
  - `MergeDungeon.Core.EnemyVisualLibrary` — 1
  - `MergeDungeon.Core.HeroVisualLibrary` — 1
  - `MergeDungeon.Core.TileVisuals` — 1
  - `UnityEngine.Rendering.Universal.Renderer2DData` — 1
  - `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset` — 1
  - `TMPro.TMP_SpriteAsset` — 1
  - `TMPro.TMP_StyleSheet` — 1
  - `TMPro.TMP_Settings` — 1

## Component frequency (scenes only)
- `Transform` — 19
- `RectTransform` — 12
- `CanvasRenderer` — 8
- `Image` — 6
- `TextMeshProUGUI` — 2
- `Camera` — 1
- `AudioListener` — 1
- `UniversalAdditionalCameraData` — 1
- `Light2D` — 1
- `Canvas` — 1
- `CanvasScaler` — 1
- `GraphicRaycaster` — 1
- `GridLayoutGroup` — 1
- `RoomMapUI` — 1
- `EventSystem` — 1
- `InputSystemUIInputModule` — 1
- `GridManager` — 1
- `DungeonManager` — 1
- `AdvanceMeterController` — 1
- `EnemyMover` — 1
- `BoardController` — 1
- `EnemySpawner` — 1
- `TileService` — 1
- `UISelectionManager` — 1

