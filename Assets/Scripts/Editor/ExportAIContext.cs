#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ExportAIContext
{
    // ---------- CONFIG ----------
    private const string OutDir = "Assets/_Reports";
    private const string OutFile = "AI_Context.md";

    // Markdown size controls
    private const int SceneTreeMaxDepth = 3;
    private const int MaxChildrenPerNode = 40;
    private const int GodObjectComponentThreshold = 6; // flag GOs with >= this many components
    private const int TopCountsLimit = 25;             // top N for component/type frequency
    private const int TopPrefabsByComponents = 20;

    // Project facts (edit if desired)
    private static readonly string[] DeclaredPlatforms = { "Android", "PC (Windows)" };
    private const string DeclaredPipeline = "URP";
    private const string DeclaredInput = "New Input System";
    private const string PerfNotes = "Mobile-first (steady 60 FPS, low memory)";
    private const string SaveStatus = "Not implemented (needs design)";
    private const string Networking = "None (single-player)";

    // Heuristic buckets to suggest system placement
    private static readonly (string bucket, string[] keywords)[] HeuristicBuckets = new[]
    {
        ("BoardSystem",   new[] { "Grid", "Board", "Tile", "Cell" }),
        ("DungeonSystem", new[] { "Dungeon", "Room", "Spawner", "Wave", "Map" }),
        ("EnemySystem",   new[] { "Enemy", "AI", "Mover" }),
        ("ProgressionSystem", new[] { "Advance", "Meter", "Progress" }),
        ("UISystem",      new[] { "UI", "Selection", "HUD" }),
        ("InventorySystem", new[] { "Inventory", "Loot", "Bag" }),
    };

    // ---------- MENU ----------
    [MenuItem("Tools/Reports/Export AI Context (single markdown)")]
    public static void Export()
    {
        Directory.CreateDirectory(OutDir);
        var sb = new StringBuilder(128 * 1024);

        // Header / Facts
        string product = PlayerSettings.productName;
        string company = PlayerSettings.companyName;
        string unity   = Application.unityVersion;
        string target  = EditorUserBuildSettings.activeBuildTarget.ToString();

        sb.AppendLine($"# AI Review Context: {product}");
        sb.AppendLine();
        sb.AppendLine("## Project facts");
        sb.AppendLine($"- **Company:** {company}");
        sb.AppendLine($"- **Unity:** {unity}");
        sb.AppendLine($"- **Active Build Target:** {target}");
        sb.AppendLine($"- **Render Pipeline:** {DeclaredPipeline}");
        sb.AppendLine($"- **Input:** {DeclaredInput}");
        sb.AppendLine($"- **Platforms:** {string.Join(", ", DeclaredPlatforms)}");
        sb.AppendLine($"- **Networking:** {Networking}");
        sb.AppendLine($"- **Save System:** {SaveStatus}");
        sb.AppendLine($"- **Performance focus:** {PerfNotes}");
        sb.AppendLine();

        // Remember currently open scenes to restore later
        var prevOpen = Enumerable.Range(0, EditorSceneManager.sceneCount)
            .Select(EditorSceneManager.GetSceneAt)
            .Select(s => s.path)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        // Gather scene info
        var scenes = BuildSceneInfos(out var sceneComponentFreq, out var godCandidates);
        WriteScenesToMarkdown(sb, scenes);
        WriteGodObjectsToMarkdown(sb, godCandidates);

        // Prefabs
        var prefabStats = BuildPrefabStats();
        WritePrefabsToMarkdown(sb, prefabStats);

        // ScriptableObjects
        var soStats = BuildScriptableObjectStats();
        WriteScriptableObjectsToMarkdown(sb, soStats);

        // Component frequency across scenes
        WriteTopComponentsToMarkdown(sb, sceneComponentFreq);

        // Review ask
        WriteReviewAsk(sb);

        // Restore previous scenes (best effort)
        TryRestoreScenes(prevOpen);

        // Write file
        string outPath = Path.Combine(OutDir, OutFile);
        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"AI context exported → {outPath}");
    }

    // ---------- DATA ----------
    [Serializable] private class SceneInfo
    {
        public string path;
        public int totalGameObjects;
        public List<Node> roots = new List<Node>();
    }

    [Serializable] private class Node
    {
        public string name;
        public string path;
        public bool active;
        public string tag;
        public string layer;
        public string[] components;
        public List<Node> children = new List<Node>();
    }

    private class GodObjectCand
    {
        public string scenePath;
        public string objectPath;
        public string name;
        public string[] components;
        public string[] guessedBuckets;
    }

    private class PrefabEntry
    {
        public string path;
        public string name;
        public string[] components;
        public int componentCount;
    }

    private class PrefabStats
    {
        public int totalPrefabs;
        public List<PrefabEntry> topByComponents = new List<PrefabEntry>();
        public List<PrefabEntry> pfNamed = new List<PrefabEntry>(); // names starting with PF_
    }

    private class SOStats
    {
        public int totalAssets;
        public List<(string type, int count)> topTypes = new List<(string type, int count)>();
    }

    // ---------- SCENES ----------
    private static List<SceneInfo> BuildSceneInfos(out Dictionary<string, int> componentFreq, out List<GodObjectCand> godObjects)
    {
        componentFreq = new Dictionary<string, int>(512);
        godObjects = new List<GodObjectCand>();
        var results = new List<SceneInfo>();

        var enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (enabledScenes.Length == 0)
        {
            Debug.LogWarning("No enabled scenes in Build Settings. Add at least one scene.");
            return results;
        }

        foreach (var s in enabledScenes)
        {
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            var info = new SceneInfo { path = s.path };

            int goCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                info.roots.Add(BuildNode(root, root.name, 0, ref goCount, componentFreq, s.path, godObjects));
            }

            info.totalGameObjects = goCount;
            results.Add(info);
        }

        return results;
    }

    private static Node BuildNode(
        GameObject go,
        string path,
        int depth,
        ref int goCounter,
        Dictionary<string, int> compFreq,
        string scenePath,
        List<GodObjectCand> gods)
    {
        goCounter++;

        var comps = go.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => c.GetType().Name)
            .ToArray();

        foreach (var c in comps)
        {
            compFreq.TryGetValue(c, out var n);
            compFreq[c] = n + 1;
        }

        // Flag god-object candidates
        if (go.name.Equals("Game", StringComparison.OrdinalIgnoreCase) || comps.Length >= GodObjectComponentThreshold)
        {
            gods.Add(new GodObjectCand
            {
                scenePath = scenePath,
                objectPath = path,
                name = go.name,
                components = comps,
                guessedBuckets = GuessBuckets(comps, path)
            });
        }

        var node = new Node
        {
            name = go.name,
            path = path,
            active = go.activeSelf,
            tag = go.tag,
            layer = LayerMask.LayerToName(go.layer),
            components = comps
        };

        if (depth < SceneTreeMaxDepth)
        {
            int childCount = go.transform.childCount;
            int limit = Mathf.Min(childCount, MaxChildrenPerNode);
            for (int i = 0; i < limit; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                node.children.Add(BuildNode(child, $"{path}/{child.name}", depth + 1, ref goCounter, compFreq, scenePath, gods));
            }
            if (childCount > limit)
            {
                node.children.Add(new Node
                {
                    name = $"… {childCount - limit} more children (truncated)",
                    path = path + "/…",
                    active = true,
                    tag = "-",
                    layer = "-",
                    components = Array.Empty<string>(),
                    children = new List<Node>()
                });
            }
        }

        return node;
    }

    private static string[] GuessBuckets(string[] components, string objectPath)
    {
        var hits = new HashSet<string>();
        foreach (var (bucket, words) in HeuristicBuckets)
        {
            bool match =
                components.Any(c => words.Any(w => c.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                words.Any(w => objectPath.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match) hits.Add(bucket);
        }
        return hits.ToArray();
    }

    private static void WriteScenesToMarkdown(StringBuilder sb, List<SceneInfo> scenes)
    {
        sb.AppendLine("## Scenes (enabled in Build Settings)");
        foreach (var scene in scenes)
        {
            sb.AppendLine($"### {scene.path}");
            sb.AppendLine($"- **Total GameObjects:** {scene.totalGameObjects}");
            sb.AppendLine("- **Hierarchy (depth-limited):**");
            foreach (var root in scene.roots)
            {
                WriteNodeMd(sb, root, 0);
            }
            sb.AppendLine();
        }
    }

    private static void WriteNodeMd(StringBuilder sb, Node node, int depth)
    {
        string indent = new string(' ', depth * 2);
        string comps = (node.components != null && node.components.Length > 0)
            ? $" — [{string.Join(", ", node.components)}]"
            : "";
        sb.AppendLine($"{indent}- **{node.name}** (active:{node.active}, layer:{node.layer}, tag:{node.tag}){comps}");
        if (node.children == null) return;
        foreach (var ch in node.children)
        {
            WriteNodeMd(sb, ch, depth + 1);
        }
    }

    private static void WriteGodObjectsToMarkdown(StringBuilder sb, List<GodObjectCand> gods)
    {
        if (gods.Count == 0) return;
        sb.AppendLine("## God-object candidates (high component count / named \"Game\")");
        foreach (var g in gods.OrderBy(g => g.scenePath).ThenBy(g => g.objectPath))
        {
            sb.AppendLine($"- **{g.name}** — `{g.objectPath}` in `{g.scenePath}`");
            sb.AppendLine($"  - Components: {string.Join(", ", g.components)}");
            if (g.guessedBuckets != null && g.guessedBuckets.Length > 0)
                sb.AppendLine($"  - Suggested system buckets: {string.Join(", ", g.guessedBuckets)}");
        }
        sb.AppendLine();
    }

    // ---------- PREFABS ----------
    private static PrefabStats BuildPrefabStats()
    {
        var stats = new PrefabStats();
        var guids = AssetDatabase.FindAssets("t:Prefab");
        stats.totalPrefabs = guids.Length;

        var entries = new List<PrefabEntry>(Mathf.Max(guids.Length, 1));
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go) continue;

            var comps = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .Distinct()
                .ToArray();

            var entry = new PrefabEntry
            {
                path = path,
                name = go.name,
                components = comps,
                componentCount = comps.Length
            };
            entries.Add(entry);

            if (go.name.StartsWith("PF_", StringComparison.OrdinalIgnoreCase))
                stats.pfNamed.Add(entry);
        }

        stats.topByComponents = entries
            .OrderByDescending(e => e.componentCount)
            .ThenBy(e => e.name)
            .Take(TopPrefabsByComponents)
            .ToList();

        stats.pfNamed = stats.pfNamed.OrderBy(e => e.name).ToList();
        return stats;
    }

    private static void WritePrefabsToMarkdown(StringBuilder sb, PrefabStats stats)
    {
        sb.AppendLine("## Prefabs");
        sb.AppendLine($"- **Total prefabs:** {stats.totalPrefabs}");
        if (stats.pfNamed.Count > 0)
        {
            sb.AppendLine("- **Gameplay prefabs (name starts with `PF_`):**");
            foreach (var e in stats.pfNamed.Take(TopPrefabsByComponents))
                sb.AppendLine($"  - `{e.name}` — {e.path} — [{string.Join(", ", e.components)}]");
        }
        if (stats.topByComponents.Count > 0)
        {
            sb.AppendLine("- **Top prefabs by component count:**");
            foreach (var e in stats.topByComponents)
                sb.AppendLine($"  - `{e.name}` ({e.componentCount}) — {e.path}");
        }
        sb.AppendLine();
    }

    // ---------- SCRIPTABLE OBJECTS ----------
    private static SOStats BuildScriptableObjectStats()
    {
        var guids = AssetDatabase.FindAssets("t:ScriptableObject");
        var typeCounts = new Dictionary<string, int>(256);

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (!so) continue;

            string t = so.GetType().FullName;
            typeCounts.TryGetValue(t, out var n);
            typeCounts[t] = n + 1;
        }

        var stats = new SOStats
        {
            totalAssets = guids.Length,
            topTypes = typeCounts
                .OrderByDescending(kv => kv.Value)
                .Take(TopCountsLimit)
                .Select(kv => (kv.Key, kv.Value))
                .ToList()
        };
        return stats;
    }

    private static void WriteScriptableObjectsToMarkdown(StringBuilder sb, SOStats stats)
    {
        sb.AppendLine("## ScriptableObjects");
        sb.AppendLine($"- **Total ScriptableObject assets:** {stats.totalAssets}");
        if (stats.topTypes.Count > 0)
        {
            sb.AppendLine("- **Top types:**");
            foreach (var (type, count) in stats.topTypes)
                sb.AppendLine($"  - `{type}` — {count}");
        }
        sb.AppendLine();
    }

    // ---------- COMPONENT FREQUENCY ----------
    private static void WriteTopComponentsToMarkdown(StringBuilder sb, Dictionary<string, int> compFreq)
    {
        if (compFreq.Count == 0) return;
        sb.AppendLine("## Component frequency (scenes only)");
        foreach (var kv in compFreq.OrderByDescending(k => k.Value).Take(TopCountsLimit))
            sb.AppendLine($"- `{kv.Key}` — {kv.Value}");
        sb.AppendLine();
    }

    // ---------- REVIEW ASK ----------
    private static void WriteReviewAsk(StringBuilder sb)
    {
        sb.AppendLine("## Review request (how to use this file)");
        sb.AppendLine("Please provide:");
        sb.AppendLine("1) **Architecture redesign**: break down any god-objects (esp. `Game`) into focused systems (Board/Dungeon/Enemy/Progress/UI), propose additive scene flow, and minimal messaging (event channels).");
        sb.AppendLine("2) **Folder & asset organization**: a scalable Runtime/Features tree, naming conventions, and Addressables grouping per platform.");
        sb.AppendLine("3) **Prefab & pooling plan**: per-entity movers/behaviours over central lists, and pools for tiles/enemies/popups.");
        sb.AppendLine("4) **Domain modeling**: Item/ItemTier/MergeRule/GridCell/Inventory/RecipeGraph; dungeon generation and how systems react to room/state changes.");
        sb.AppendLine("5) **Save system design**: JSON with stable string IDs, versioning/migrations, atomic writes; minimal `ISaveService` sample.");
        sb.AppendLine("6) **Mobile performance/URP**: concrete URP settings, texture/audio import guidance, UI rebuild hygiene, physics layers, GC traps, and a light profiling checklist.");
        sb.AppendLine("7) **Input (New Input System)**: action maps, control schemes (Touch/Gamepad/KBM), routing, and ergonomics for merging.");
        sb.AppendLine("8) **Build/CI & Git hygiene**: `.gitignore`, `.gitattributes`, Git LFS patterns, and basic CI steps (incl. Addressables).");
        sb.AppendLine("9) **Code review**: call out specific files/classes and give before/after refactors, safety/lifetime fixes, and editor tooling ideas.");
        sb.AppendLine();
    }

    // ---------- RESTORE ----------
    private static void TryRestoreScenes(string[] prevPaths)
    {
        // Return to a clean state, then reopen previous scenes additively
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var p in prevPaths)
        {
            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
        }
    }
}
#endif
