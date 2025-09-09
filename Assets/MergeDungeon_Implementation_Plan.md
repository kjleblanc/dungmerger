# Merge-Dungeon (Unity 6) — Guided Implementation Plan

**Audience:** an AI coding agent + a non-programmer operator  
**Goal:** refactor the prototype into a clean, event-driven, additive-scene architecture with pooling, saving, Addressables, and mobile-first performance.

---

## 0) Scope & success criteria (read first)

**Done =**  
- No “god object”: `Game` scripts are redistributed to **Systems** children.  
- Additive scene flow: `Scene_Bootstrap_Persistent` (+ `Scene_UI_HUD`) + `Scene_Gameplay_*`.  
- Minimal **event channels**: systems talk via ScriptableObjects instead of direct refs.  
- **Pooling** in place for enemies/tiles/popups.  
- **Save/Load** works end-to-end (JSON, atomic writes, backups).  
- **Addressables**: sensible groups & profiles; load/retain/release policy.  
- **New Input System** routes to gameplay/UI; touch merging feels responsive.  
- **URP/mobile** settings and hygiene (UI rebuilds, GC, batching).  
- Git **LFS** & **CI** basic path for Android/Windows builds.

---

## 1) Working branch, safety net, and repo hygiene

1. **Create a new branch:** `feature/architecture-v1`.  
2. Add/update:
   - `.gitignore` (Unity):  
     ```
     [Ll]ibrary/
     [Tt]emp/
     [Bb]uild*/
     [Ll]ogs/
     [Oo]bj/
     UserSettings/
     MemoryCaptures/
     *.csproj
     *.sln
     ```
   - `.gitattributes` (LFS):  
     ```
     *.psd filter=lfs diff=lfs merge=lfs -text
     *.wav filter=lfs diff=lfs merge=lfs -text
     *.fbx filter=lfs diff=lfs merge=lfs -text
     *.tga filter=lfs diff=lfs merge=lfs -text
     *.aseprite filter=lfs diff=lfs merge=lfs -text
     ```
3. Commit: `chore(repo): add unity .gitignore and LFS rules`.

---

## 2) Create folders & move assets (no code yet)

```
Assets/
  Scripts/
    Runtime/
      Core/
      Services/
      Systems/
        Board/
        Dungeon/
        Enemy/
        Progression/
        UI/
    Editor/
      Validators/
      Debug/
  Prefabs/
  ScriptableObjects/
  Addressables/
    Groups/
    Profiles/
```

- Move existing scripts out of the single `Game` object’s script folder into the **closest matching** `Systems/*` subfolder (keep names for now; we’ll refactor in place later).  
- Commit: `refactor(folders): establish Runtime/Systems structure`.

---

## 3) Additive scene strategy (create 3 scenes)

```
[Bootstrap (Persistent)]
   ├─ Systems/
   ├─ Services/
   ├─ PoolHost
   └─ EventChannels (SO assets live in project; referenced here)
[UI_HUD]  (additive)
[Gameplay_Floor01] (additive)
```

**Steps:**
1. Duplicate your current working scene → `Scene_Gameplay_Floor01.unity` and remove any global/singleton-ish things (keep boards/map/meter/spawn points).  
2. Create `Scene_Bootstrap_Persistent.unity`:
   - Empty roots: `Systems`, `Services`, `PoolHost`.  
   - Place **no** gameplay/UI here.
3. Create `Scene_UI_HUD.unity` for HUD/pause/debug overlay.
4. Create `Scripts/Runtime/Core/SceneLoader.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
  static void Boot() {
    if (SceneManager.sceneCount == 1) {
      SceneManager.LoadScene("Scene_Bootstrap_Persistent", LoadSceneMode.Single);
      SceneManager.LoadScene("Scene_UI_HUD", LoadSceneMode.Additive);
      SceneManager.LoadScene("Scene_Gameplay_Floor01", LoadSceneMode.Additive);
    }
  }
}
```

- Commit: `feat(scenes): bootstrap persistent + additive UI/gameplay`.

---

## 4) Messaging — ScriptableObject event channels

**Create:**
- `Scripts/Runtime/Core/Events/BaseEventChannelSO.cs`  
- `Scripts/Runtime/Core/Events/VoidEventChannelSO.cs`  
- `Scripts/Runtime/Core/Events/ItemEventChannelSO.cs` (if needed)  
- `Scripts/Runtime/Core/Events/WaveEventChannelSO.cs`  

```csharp
using System;
using UnityEngine;

public abstract class BaseEventChannelSO<T> : ScriptableObject {
  public event Action<T> Raised;
  public void Raise(T payload) => Raised?.Invoke(payload);
  public void Clear() => Raised = null;
}

[CreateAssetMenu(menuName="Events/Void")]
public sealed class VoidEventChannelSO : ScriptableObject {
  public event Action Raised; public void Raise()=>Raised?.Invoke();
  public void Clear()=>Raised=null;
}
```

**In Project:** Create assets (Right-click → Create → Events) for:
- `EV_OnFixedStep` (Void)  
- `EV_WaveStarted` (payload = your `WaveSpec` type or a simple DTO)  
- `EV_MergeAttempt`, `EV_ItemMerged` (optional)

- Commit: `feat(events): SO event channels and assets`.

---

## 5) Systems roots and minimal scripts

**In `Scene_Bootstrap_Persistent` add empty GameObjects:**
- `Systems/BoardSystem`, `Systems/DungeonSystem`, `Systems/EnemySystem`, `Systems/ProgressionSystem`, `Systems/UISystem`, `PoolHost`.

**Create these scripts with compilable skeletons:**

`Scripts/Runtime/Systems/Enemy/EnemySystem.cs`
```csharp
using UnityEngine;

public sealed class EnemySystem : MonoBehaviour {
  [SerializeField] VoidEventChannelSO onFixedStep;
  void FixedUpdate() => onFixedStep?.Raise();
}
```

`Scripts/Runtime/Systems/Enemy/EnemySpawner.cs`
```csharp
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour {
  [SerializeField] WaveEventChannelSO waveStarted;
  [SerializeField] PoolHost pool;
  [SerializeField] GameObject enemyPrefab;

  void OnEnable(){ if (waveStarted!=null) waveStarted.Raised += OnWave; }
  void OnDisable(){ if (waveStarted!=null) waveStarted.Raised -= OnWave; }

  void OnWave(WaveSpec wave){
    // TODO: iterate wave spec and spawn pooled enemies
  }
}
public sealed class WaveSpec { public int count; /* expand to match your SOs */ }
```

`Scripts/Runtime/Systems/Board/BoardSystem.cs`
```csharp
using UnityEngine;

public sealed class BoardSystem : MonoBehaviour {
  // Holds grid state & merge logic (data-only types in Core/)
  public void TryMergeAt(int x,int y){ /* raise events */ }
}
```

`Scripts/Runtime/Systems/Progression/ProgressionSystem.cs`
```csharp
using UnityEngine;
public sealed class ProgressionSystem : MonoBehaviour {
  public int xp;
  public void OnEnemyDefeated(int value){ xp += value; }
}
```

`Scripts/Runtime/Systems/UI/UISystem.cs`
```csharp
using UnityEngine;
public sealed class UISystem : MonoBehaviour { /* wires HUD, pause, overlays */ }
```

`Scripts/Runtime/Services/PoolHost.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class PoolHost : MonoBehaviour {
  readonly Dictionary<GameObject, Stack<GameObject>> pools = new();
  public GameObject Get(GameObject prefab){
    if(!pools.TryGetValue(prefab, out var s) || s.Count==0)
      return Instantiate(prefab, transform);
    var go = s.Pop(); go.SetActive(true); return go;
  }
  public void Return(GameObject prefab, GameObject go){
    go.SetActive(false);
    if(!pools.TryGetValue(prefab, out var s)) pools[prefab] = s = new();
    s.Push(go);
  }
}
```

- Commit: `feat(systems): minimal systems & pooling host`.

---

## 6) Move logic from `Game` into Systems (safely)

**Goal:** Remove `GridManager`, `DungeonManager`, `AdvanceMeterController`, `EnemyMover`, `BoardController`, `EnemySpawner`, `TileService` from the single `Game` object and distribute:

| Old component (on `Game`) | New location / role |
|---|---|
| `GridManager` | `BoardSystem` (state + rules) |
| `BoardController` | `BoardViewController` under `BoardSystem` (UI only) |
| `TileService` | `PoolHost` + `TileFactory` (inside `BoardSystem`) |
| `DungeonManager` | `DungeonSystem` (rooms/waves/loot) |
| `EnemySpawner` | `EnemySpawner` (child of `EnemySystem`) |
| `EnemyMover` | **Delete**; replace with per-entity tick (below) |
| `AdvanceMeterController` | `ProgressionSystem` (listens to kills/merges) |

**Per-entity tick (replace central `EnemyMover`):**

`Scripts/Runtime/Systems/Enemy/EnemyController.cs`
```csharp
using UnityEngine;

public sealed class EnemyController : MonoBehaviour {
  [SerializeField] VoidEventChannelSO onFixedStep;
  void OnEnable(){ if(onFixedStep!=null) onFixedStep.Raised += Tick; }
  void OnDisable(){ if(onFixedStep!=null) onFixedStep.Raised -= Tick; }
  void Tick(){ /* move/attack with Time.fixedDeltaTime */ }
}
```

- In **prefab `PF_Enemy`**, add `EnemyController` and assign `onFixedStep` asset.
- Remove any centralized “list of enemies” loops scanning each frame.

- Commit: `refactor(enemies): per-entity stepping via event channel`.

---

## 7) Pooling targets & wiring

Pool these (hosted on `PoolHost` in Bootstrap):  
- `PF_Enemy`, `PF_Tile`, `PF_LootBagTile`, `PF_DamagePopup` (optionally board cells if they churn).

**Usage example in spawner:**
```csharp
var go = pool.Get(enemyPrefab);
go.transform.position = spawnPos;
var ctrl = go.GetComponent<EnemyController>(); // init with data as needed
```

Return to pool on death/despawn:
```csharp
pool.Return(enemyPrefab, gameObject);
```

- Commit: `feat(pooling): pooled enemies/tiles/popups`.

---

## 8) Domain data types (pure C#)

`Scripts/Runtime/Core/Domain.cs`
```csharp
using System;
using System.Collections.Generic;

public readonly struct ItemId { public readonly string Value; public ItemId(string v)=>Value=v; public override string ToString()=>Value; }
public readonly struct ItemTypeId { public readonly string Value; public ItemTypeId(string v)=>Value=v; public override string ToString()=>Value; }
public enum ItemTier { T1,T2,T3,T4,T5 }

[Serializable] public sealed class ItemData { public string id; public string typeId; public int tier; }

public sealed class GridCell { public readonly int x,y; public string itemId; public GridCell(int x,int y){this.x=x; this.y=y;} }
public sealed class Inventory {
  public readonly int w,h; public readonly GridCell[] cells;
  public Inventory(int w,int h){this.w=w; this.h=h; cells=new GridCell[w*h]; for(int y=0;y<h;y++)for(int x=0;x<w;x++) cells[y*w+x]=new GridCell(x,y);}
  public int Index(int x,int y)=>y*w+x;
}
public sealed class MergeRule { public string a, b, result; public int minTier; }
public sealed class RecipeGraph { public readonly Dictionary<string, List<MergeRule>> byInput = new(); }
```

- Commit: `feat(core): domain types for board/items/merges`.

---

## 9) Save system (JSON, atomic, backups)

`Scripts/Runtime/Services/ISaveService.cs` & `JsonSaveService.cs`
```csharp
using System; using System.IO; using UnityEngine;

public interface ISaveService { SaveGame Current { get; } void SaveNow(); bool TryLoad(out SaveGame save); }

[Serializable] public sealed class SaveGame {
  public int v = 1;
  public string rngSeed = "seed";
  public PlayerState player = new();
  public BoardState board = new();
  public DungeonState dungeon = new();
}
[Serializable] public sealed class PlayerState { public int xp; public string heroId="hero"; }
[Serializable] public sealed class CellState { public int x,y; public string itemId; }
[Serializable] public sealed class ItemState { public string id,typeId; public int tier; }
[Serializable] public sealed class BoardState { public int w=6,h=6; public System.Collections.Generic.List<CellState> cells=new(); public System.Collections.Generic.List<ItemState> items=new(); }
[Serializable] public sealed class DungeonState { public string roomId="start"; public int floor=1; }

public sealed class JsonSaveService : MonoBehaviour, ISaveService {
  [SerializeField] string fileName = "save.json";
  string PathFull => System.IO.Path.Combine(Application.persistentDataPath, fileName);
  public SaveGame Current { get; private set; } = new();

  void Awake(){ TryLoad(out _); }
  public void SaveNow(){
    var json = JsonUtility.ToJson(Current,false);
    var p = PathFull; var tmp=p+".tmp"; var bak=p+".bak";
    File.WriteAllText(tmp,json);
    if(File.Exists(p)) File.Replace(tmp,p,bak);
    else File.Move(tmp,p);
  }
  public bool TryLoad(out SaveGame save){
    var p=PathFull;
    if(!File.Exists(p)){ save=Current; return false; }
    try{ save=JsonUtility.FromJson<SaveGame>(File.ReadAllText(p)); if(save.v!=Current.v) save = Migrate(save); Current=save; return true; }
    catch{ save=Current; return false; }
  }
  SaveGame Migrate(SaveGame old){ old.v=1; return old; }
  void OnApplicationPause(bool pause){ if(pause) SaveNow(); }
  void OnApplicationQuit(){ SaveNow(); }
}
```

**Hook:** Add `JsonSaveService` to `Services` in Bootstrap.  
- Commit: `feat(save): JSON save service with atomic writes & backups`.

---

## 10) Addressables (groups, profiles, lifecycle)

**Groups (create in Addressables window):**
- `Core_Static` (small SO configs, event channel assets) — load once, keep.  
- `Tiles_Visuals`, `Enemies_Visuals`, `UI` — load on gameplay scene, release on unload.

**Profiles:** `Local_Dev`, `Android_Release`, `PC_Release`.  
**Service (optional helper):**

`Scripts/Runtime/Services/AddressablesService.cs`
```csharp
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressablesService {
  readonly List<AsyncOperationHandle> handles=new();
  public T LoadSync<T>(object key) where T:class { var h=Addressables.LoadAssetAsync<T>(key); h.WaitForCompletion(); handles.Add(h); return h.Result; }
  public void ReleaseAll(){ foreach(var h in handles) if(h.IsValid()) Addressables.Release(h); handles.Clear(); }
}
```

- Commit: `feat(addressables): groups/profiles and lifecycle helper`.

---

## 11) UI/Board performance hygiene

- Keep the **HUD on one Canvas**; avoid enabling/disabling entire canvases; prefer `CanvasGroup` alpha/blocksRaycasts.  
- On the Board, **don’t change GridLayoutGroup** every merge; rebuild only on size change. Cache `RectTransform`/`Image` references.  
- Pool `PF_DamagePopup` and reuse text components.

- Commit: `perf(ui): reduce rebuilds and pool popups`.

---

## 12) Input (New Input System) wiring

- Action Maps: **Board**, **UI**, **System**.  
- Control schemes: **Touch**, **Gamepad**, **Keyboard&Mouse**.  
- Add a `PlayerInput` to `UISystem` (in Bootstrap) and route:
  - Board gestures → `BoardInputController` (on Board scene object).  
  - Pause/back → `PauseController` (in `UISystem`).  
- Save **rebinding** JSON via `ISaveService`.

- Commit: `feat(input): action maps + PlayerInput routing`.

---

## 13) URP & mobile performance checklist

- **URP Asset:** enable **SRP Batcher**; **MSAA** → Android 1×, PC 2× if needed.  
- Prefer a single **Global Light 2D**.  
- Texture import: **ASTC** for Android; **BCn** for PC; pack sprites into Atlases.  
- Audio: Vorbis ~112–128 kbps; short SFX as ADPCM.  
- Physics layer matrix: disable UI/Default collisions.  
- GC: remove LINQ in `Update`/`FixedUpdate`; reuse lists/allocs.

- Commit: `chore(urp): mobile-first settings`.

---

## 14) Editor tooling (quality of life)

**Merge rules validator (stub):**  
`Scripts/Editor/Validators/MergeRuleValidator.cs`
```csharp
using UnityEditor; using UnityEngine;

public sealed class MergeRuleValidator : EditorWindow {
  [MenuItem("Tools/Merge Rules Validator")]
  static void Open(){ GetWindow<MergeRuleValidator>("Merge Rules"); }
  void OnGUI(){
    if(GUILayout.Button("Scan for conflicts")){
      // Load SO_MergeRules, verify determinism/symmetry, log warnings
      Debug.Log("TODO: implement validator");
    }
  }
}
```

- Commit: `feat(editor): merge rules validator stub`.

---

## 15) CI (optional but recommended)

**.github/workflows/build.yml** (template; adjust Unity version & secrets):
```yaml
name: Build
on: [push]
jobs:
  android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: Android
          buildName: MergeDungeon
          androidAppBundle: true
  windows:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: StandaloneWindows64
          buildName: MergeDungeon
```
- Commit: `ci: basic android/windows builds`.

---

## 16) Testing & checkpoints (operator-friendly)

After each major commit:

1. **Play from Bootstrap** (press Play): HUD appears, gameplay scene loads.  
2. Merge an item: no console errors; framerate steady.  
3. Spawn enemies (temporary button/event): they move without a central updater.  
4. Quit/Resume: state persists (board size/xp/room id).  
5. Toggle gameplay scene (simulate floor change): old bundles released (memory stable).

---

## 17) Backlog slices & time boxes

1) **Core split** (systems + events) — ½ day.  
2) **Enemies** (per-entity tick + spawner events) — ½ day.  
3) **Pooling** — ½ day.  
4) **Save** — ½ day.  
5) **Addressables** — ½ day.  
6) **Input & UI hygiene** — ½ day.  
7) **URP tuning & validators** — ½ day.  

---

## 18) Appendix — ASCII architecture & scene flow

**Modules**
```
[Bootstrap (Persistent)]
   ├─ Systems/
   │   ├─ BoardSystem
   │   ├─ DungeonSystem
   │   ├─ EnemySystem
   │   ├─ ProgressionSystem
   │   └─ UISystem
   ├─ Services/ (Save, Addressables)
   ├─ PoolHost
   └─ EventChannels (SO assets)
```

**Scene loading**
```
App Start
 └─ Load Scene_Bootstrap_Persistent
     ├─ Load Scene_UI_HUD (additive)
     └─ Load Scene_Gameplay_Floor01 (additive)
Floor Change
 ├─ Unload current Scene_Gameplay_*
 └─ Load new Scene_Gameplay_*
```

---

# Kickoff prompt for the coding agent

> You are a senior Unity 6 engineer. Use the **attached project snapshot and reports** (gitingest, SceneHierarchyReport.md/json, PrefabCatalog.json, ScriptableObjectCatalog.json, Scene_SampleScene.json) as ground truth. Implement the following plan **exactly**, making small, testable commits. Provide a summary after each step: files changed, key code snippets, and how to verify in Editor. Assume Unity 6 LTS, URP, New Input System, Android + Windows targets.

**Global rules**
- No singletons hidden in code. Prefer ScriptableObject **event channels**.  
- Keep ScriptableObjects **read-only** at runtime; build runtime copies if mutation is required.  
- Favor pooling & Addressables; avoid per-frame LINQ; minimize allocations.  
- All scripts compile at each commit.  
- When touching UI, do not increase Canvas rebuilds.

**Tasks**

1) **Repo hygiene**  
   - Add `.gitignore` and `.gitattributes` (LFS for psd/fbx/wav/tga/aseprite).  
   - Commit: `chore(repo): add unity ignore + LFS`.

2) **Folders & moves**  
   - Create `Scripts/Runtime/{Core,Services,Systems/{Board,Dungeon,Enemy,Progression,UI}}`, `Scripts/Editor/{Validators,Debug}`.  
   - Move existing scripts from the `Game` object’s area into the closest matching `Systems/*`.  
   - Commit: `refactor(folders): organize runtime systems`.

3) **Scenes**  
   - Create `Scene_Bootstrap_Persistent`, `Scene_UI_HUD`, `Scene_Gameplay_Floor01`.  
   - Add `SceneLoader.cs` (provided) so play-from-any-scene loads all three.  
   - Commit: `feat(scenes): bootstrap + additive flow`.

4) **Event channels**  
   - Add `BaseEventChannelSO`, `VoidEventChannelSO`, and assets: `EV_OnFixedStep`, `EV_WaveStarted`, (optional: `EV_MergeAttempt`, `EV_ItemMerged`).  
   - Commit: `feat(events): event channels + assets`.

5) **Systems roots**  
   - In Bootstrap, create `Systems` children: `BoardSystem`, `DungeonSystem`, `EnemySystem`, `ProgressionSystem`, `UISystem`, plus `PoolHost`, `Services`.  
   - Add minimal scripts (provided stubs).  
   - Commit: `feat(systems): initial components`.

6) **Enemy refactor**  
   - Delete/retire any global `EnemyMover`.  
   - Add `EnemyController` to `PF_Enemy` and wire `EV_OnFixedStep`.  
   - Implement `EnemySpawner` to listen to `EV_WaveStarted`.  
   - Commit: `refactor(enemies): per-entity tick + event-driven spawns`.

7) **Pooling**  
   - Implement `PoolHost` (provided) and wire pooling for `PF_Enemy`, `PF_Tile`, `PF_LootBagTile`, `PF_DamagePopup`.  
   - Replace `Instantiate/Destroy` call sites accordingly.  
   - Commit: `feat(pooling): pools for enemies/tiles/popups`.

8) **Board/UI hygiene**  
   - Ensure Board `GridLayoutGroup` is not modified per merge; rebuild only on board size changes. Cache components and remove LINQ in hot paths.  
   - Commit: `perf(ui): reduce rebuilds, cache refs`.

9) **Domain types**  
   - Add `Core/Domain.cs` for `ItemId`, `ItemData`, `GridCell`, `Inventory`, `MergeRule`, `RecipeGraph`.  
   - Commit: `feat(core): domain models`.

10) **Save system**  
   - Add `ISaveService` and `JsonSaveService` (provided).  
   - Place `JsonSaveService` in Bootstrap’s `Services`.  
   - Call `SaveNow()` on pause/quit; load on Awake.  
   - Commit: `feat(save): json save/load + atomic writes`.

11) **Addressables**  
   - Create groups (`Core_Static`, `Tiles_Visuals`, `Enemies_Visuals`, `UI`) and profiles (`Local_Dev`, `Android_Release`, `PC_Release`).  
   - Ensure SO configs in `Core_Static` (loaded once), visuals in the others.  
   - Add optional `AddressablesService`.  
   - Commit: `feat(addressables): groups/profiles + lifecycle helper`.

12) **Input System**  
   - Ensure Action Maps: `Board`, `UI`, `System`; control schemes: `Touch`, `Gamepad`, `Keyboard&Mouse`.  
   - Add `PlayerInput` on `UISystem`; route to `BoardInputController` & `PauseController`.  
   - Persist rebindings via `ISaveService`.  
   - Commit: `feat(input): maps & routing`.

13) **URP/mobile**  
   - Enable SRP Batcher; Android MSAA 1×, PC 2×; confirm Global Light 2D; texture/audio import settings per platform; prune physics collision matrix.  
   - Commit: `chore(urp): mobile-first settings`.

14) **Editor tools**  
   - Add `MergeRuleValidator` window stub (provided).  
   - Commit: `feat(editor): merge rules validator stub`.

**Verification after each task**
- Provide: playtest notes (what you clicked), console output status, profiler (if touched), screenshots optional.  
- Confirm no new warnings; measure FPS on a representative Android device if available.

**Deliver final artifacts**
- List of new/modified files.  
- Short “operator checklist” to reproduce a clean run: open Bootstrap → Play → see HUD → see board → spawn enemies → perform a merge → quit → relaunch (save persists).  
- Next-steps notes for Jobs/Burst (grid scans, enemy stepping batches) if time remains.
