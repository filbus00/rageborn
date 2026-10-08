using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Puts the active quests' steps for this depth on the level (2026-10-08, Docs/05 Quests): captives to free, objects
    /// to take, named foes, the watch fire to hold and things to break, each where <see cref="QuestSites"/> says, marked
    /// on the minimap and by a faint gold light. Made by <see cref="DungeonLevel"/>; places in Start, once the enemy
    /// manager is ready.
    /// </summary>
    public class QuestObjectives : MonoBehaviour
    {
        DungeonLevel level;
        DungeonLayout layout;
        int depth;
        int levelSeed;

        public static void Create(DungeonLevel level, DungeonLayout layout, Transform root, int depth, int levelSeed)
        {
            var go = new GameObject("Quest Objectives");
            go.transform.SetParent(root, false);
            var objectives = go.AddComponent<QuestObjectives>();
            objectives.level = level;
            objectives.layout = layout;
            objectives.depth = depth;
            objectives.levelSeed = levelSeed;
            Minimap.QuestMarks.Clear();
        }

        void Start()
        {
            QuestSync.Bosses();
            var quests = GameSession.Current.Quests;
            foreach (var (quest, step) in quests.StepsAt(depth))
            {
                var cells = QuestSites.Cells(layout, step);
                foreach (var cell in cells)
                    Minimap.QuestMarks.Add(cell);
                switch (step.Kind)
                {
                    case QuestStepKind.Rescue:
                        CaptiveSite.Create(transform, quest.Id, step, cells[0]);
                        break;
                    case QuestStepKind.Recover:
                        RelicSite.Create(transform, quest.Id, step, cells[0]);
                        break;
                    case QuestStepKind.NamedFoe:
                        NamedFoe.Create(transform, quest.Id, step, cells[0], level, layout.EnemyLevel);
                        break;
                    case QuestStepKind.HoldFire:
                        WatchFire.Create(transform, quest.Id, step, cells[0], level, depth, levelSeed);
                        break;
                    case QuestStepKind.Break:
                        BreakTargets.Create(transform, quest.Id, step, cells, level, depth, levelSeed, layout.EnemyLevel);
                        break;
                }
            }
            FindAnyObjectByType<Minimap>()?.RefreshMarks();
        }

        /// <summary>A step done in the dungeon: the quest moves on, a banner says so, the minimap mark dims.</summary>
        public static void Done(string questId, QuestStep step, Vector2Int? cell = null)
        {
            if (!GameSession.Current.Quests.CompleteStep(questId, step.Target))
                return;
            var next = GameSession.Current.Quests.CurrentStep(questId);
            var line = step.DoneLine ?? $"{QuestRules.TargetName(step.Target)}: done.";
            HintBanner.Current?.Show(next != null ? $"{line}\n{next.Log}" : line);
            if (cell.HasValue)
                Minimap.QuestMarks.Remove(cell.Value);
            FindAnyObjectByType<Minimap>()?.RefreshMarks();
            QuestSync.Bosses();
        }

        /// <summary>A soft gold light over a quest thing, so it is found in the dark.</summary>
        public static void Glow(Transform parent)
        {
            var light = WorldLights.Add(parent, new Color(1f, 0.85f, 0.45f), 0.8f, 0.3f, 2.8f, 0.4f);
            if (light != null)
                parent.gameObject.AddComponent<FlickerLight>().Init(light, parent.position.x);
        }

        public static Vector3 World(Vector2Int cell)
        {
            var w = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            return new Vector3(w.x, w.y, 0f);
        }

        /// <summary>Whether any living enemy is within a ground distance of a point.</summary>
        public static bool EnemyNear(Vector2 ground, float range)
        {
            var manager = FindAnyObjectByType<EnemyManager>();
            if (manager == null)
                return false;
            foreach (var enemy in manager.Active)
                if (enemy.IsAlive && Vector2.Distance(enemy.GroundPosition, ground) <= range)
                    return true;
            return false;
        }
    }

    /// <summary>Boss steps: done once the depth's boss is dead, even if it died before the quest reached the step.</summary>
    public static class QuestSync
    {
        public static void Bosses()
        {
            var session = GameSession.Current;
            foreach (var quest in QuestRules.All)
            {
                var step = session.Quests.CurrentStep(quest.Id);
                if (step == null || step.Kind != QuestStepKind.Boss)
                    continue;
                if (session.IsKilled(DungeonRules.LevelId(step.Depth) + "/Boss", 0) && session.Quests.CompleteStep(quest.Id, step.Target))
                    HintBanner.Current?.Show($"{step.DoneLine}\n{session.Quests.CurrentStep(quest.Id)?.Log}");
            }
        }
    }

    /// <summary>Someone held below (Tobin, Pellam, Bram, Ivy): freed by walking up once no enemy is near.</summary>
    public class CaptiveSite : WalkOnTrigger
    {
        string questId;
        QuestStep step;
        Vector2Int cell;

        public static void Create(Transform parent, string questId, QuestStep step, Vector2Int cell)
        {
            var go = new GameObject("Captive " + step.Target, typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = QuestObjectives.World(cell);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.8f;
            NpcFigure.Create(go.transform, step.Target, new Color(0.6f, 0.55f, 0.45f));
            TravelArt.Label(go.transform, QuestRules.Title(step.Target), LootColors.Gold, 1.5f);
            QuestObjectives.Glow(go.transform);
            var site = go.AddComponent<CaptiveSite>();
            site.questId = questId;
            site.step = step;
            site.cell = cell;
        }

        protected override void OnWalkedOn()
        {
            if (QuestObjectives.EnemyNear(IsoMath.WorldToGround(transform.position), 7f))
            {
                HintBanner.Current?.Show($"{QuestRules.TargetName(step.Target)}: \"They are still here. Kill them first!\"");
                return;
            }
            QuestObjectives.Done(questId, step, cell);
            Destroy(gameObject, 0.05f);
        }
    }

    /// <summary>An object to take: a log, notes, the oath-stone, the great lamp. Walked up to, it is hers.</summary>
    public class RelicSite : WalkOnTrigger
    {
        string questId;
        QuestStep step;
        Vector2Int cell;

        static string Art(string target) => target switch
        {
            "oath_stone" => "quest_oath_stone",
            "great_lamp" => "quest_great_lamp",
            _ => "quest_lectern",
        };

        public static void Create(Transform parent, string questId, QuestStep step, Vector2Int cell)
        {
            var go = new GameObject("Relic " + step.Target, typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = QuestObjectives.World(cell);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.7f;
            var sprite = WorldArt.Place(Art(step.Target), go.transform, go.transform.position);
            if (sprite != null)
                WorldArt.Lit(sprite);
            else
                TravelArt.Figure(go.transform, new Color(0.8f, 0.7f, 0.4f));
            TravelArt.Label(go.transform, QuestRules.Title(step.Target), LootColors.Gold, 1.3f);
            QuestObjectives.Glow(go.transform);
            var site = go.AddComponent<RelicSite>();
            site.questId = questId;
            site.step = step;
            site.cell = cell;
        }

        protected override void OnWalkedOn()
        {
            QuestObjectives.Done(questId, step, cell);
            Destroy(gameObject, 0.05f);
        }
    }

    /// <summary>
    /// A unique foe (Gorrak, Vessa, Sir Aldric, the Reliquary Keeper): an enemy of its kind four levels up, its name over
    /// its head, waiting in its lair among its pack. Its death finishes the step.
    /// </summary>
    public class NamedFoe : MonoBehaviour
    {
        const int LevelsUp = 4;

        string questId;
        QuestStep step;
        Vector2Int cell;
        EnemyController foe;
        EnemyManager manager;
        Transform label;

        static PackMember Kind(string target) => target switch
        {
            "gorrak" => PackMember.Ghoul,
            "vessa" => PackMember.Cutthroat,
            "aldric" => PackMember.SkeletonKnight,
            _ => PackMember.GravePriest,
        };

        public static void Create(Transform parent, string questId, QuestStep step, Vector2Int cell, DungeonLevel level, int enemyLevel)
        {
            var manager = FindAnyObjectByType<EnemyManager>();
            var definition = level != null ? level.DefinitionOf(Kind(step.Target)) : null;
            if (manager == null || definition == null)
                return;
            var go = new GameObject("Named " + step.Target);
            go.transform.SetParent(parent, false);
            var named = go.AddComponent<NamedFoe>();
            named.questId = questId;
            named.step = step;
            named.cell = cell;
            named.manager = manager;
            named.foe = manager.Spawn(definition, IsoMath.CellToGround(cell), false, null, enemyLevel + LevelsUp);
            var labelObject = new GameObject("Name");
            labelObject.transform.SetParent(go.transform, false);
            TravelArt.Label(labelObject.transform, QuestRules.Title(step.Target), new Color(1f, 0.6f, 0.25f), 0f);
            named.label = labelObject.transform;
            manager.Killed += named.OnKilled;
        }

        void LateUpdate()
        {
            if (foe == null || label == null)
                return;
            var height = foe.Definition != null ? 1.4f * foe.Definition.VisualScale : 1.4f;
            label.position = foe.transform.position + new Vector3(0f, height, 0f);
            label.gameObject.SetActive(foe.IsAlive && foe.gameObject.activeInHierarchy);
        }

        void OnKilled(EnemyController enemy)
        {
            if (enemy != foe)
                return;
            manager.Killed -= OnKilled;
            foe = null;
            QuestObjectives.Done(questId, step, cell);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.Killed -= OnKilled;
        }
    }

    /// <summary>
    /// The drowned watch's fire (2026-10-08): walked up to, it is lit, and must be held: for 30 seconds the level's dead
    /// come at it in waves from the dark, and the fire only catches while she stays near it. Then it burns.
    /// </summary>
    public class WatchFire : WalkOnTrigger
    {
        const float HoldSeconds = 30f;
        const float Near = 6f;
        const float WaveEvery = 5f;

        string questId;
        QuestStep step;
        Vector2Int cell;
        DungeonLevel level;
        int depth;
        int levelSeed;
        SpriteRenderer sprite;
        TextMesh progressText;
        float held;
        float waveTimer;
        int wave;
        bool burning;
        bool done;

        public static void Create(Transform parent, string questId, QuestStep step, Vector2Int cell, DungeonLevel level, int depth, int levelSeed)
        {
            var go = new GameObject("Watch Fire", typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = QuestObjectives.World(cell);
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var fire = go.AddComponent<WatchFire>();
            fire.questId = questId;
            fire.step = step;
            fire.cell = cell;
            fire.level = level;
            fire.depth = depth;
            fire.levelSeed = levelSeed;
            fire.sprite = WorldArt.Place("vigil_fire_unlit", go.transform, go.transform.position);
            if (fire.sprite != null)
                WorldArt.Lit(fire.sprite);
            TravelArt.Label(go.transform, QuestRules.Title(step.Target), LootColors.Gold, 1.6f);
            var text = new GameObject("Progress", typeof(TextMesh));
            text.transform.SetParent(go.transform, false);
            text.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            fire.progressText = text.GetComponent<TextMesh>();
            fire.progressText.anchor = TextAnchor.LowerCenter;
            fire.progressText.characterSize = 0.08f;
            fire.progressText.fontSize = 48;
            fire.progressText.color = new Color(1f, 0.7f, 0.3f);
            text.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
            QuestObjectives.Glow(go.transform);
        }

        protected override void OnWalkedOn()
        {
            if (burning || done)
                return;
            burning = true;
            waveTimer = 1f;
            if (fire != null)
                return;
            HintBanner.Current?.Show("The fire takes. Hold it while it catches: stay near.");
            fire = WorldLights.Add(transform, new Color(1f, 0.55f, 0.25f), 1.2f, 0.3f, 5f, 0.4f);
            if (fire != null)
                gameObject.AddComponent<FlickerLight>().Init(fire, 1.7f);
        }

        UnityEngine.Rendering.Universal.Light2D fire;

        void Update()
        {
            if (!burning || done)
                return;
            var manager = FindAnyObjectByType<EnemyManager>();
            var near = manager != null && Vector2.Distance(manager.PlayerGround, IsoMath.WorldToGround(transform.position)) <= Near;
            if (near)
                held += Time.deltaTime;
            progressText.text = near ? $"{Mathf.CeilToInt(HoldSeconds - held)}" : "Stay near the fire!";

            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0f && manager != null && level != null)
            {
                waveTimer = WaveEvery;
                SpawnWave(manager);
            }
            if (held < HoldSeconds)
                return;
            done = true;
            progressText.text = "";
            var lit = WorldArt.Get("vigil_fire_lit");
            if (sprite != null && lit != null)
                sprite.sprite = lit;
            QuestObjectives.Done(questId, step, cell);
        }

        // Four of the level's own kinds, from four sides out in the dark, already hunting her.
        void SpawnWave(EnemyManager manager)
        {
            var members = PackComposition.Roll(depth, PackKind.Normal, 4, levelSeed, 1000 + wave);
            var origin = IsoMath.WorldToGround(transform.position);
            for (var i = 0; i < members.Length; i++)
            {
                var definition = level.DefinitionOf(members[i]);
                if (definition == null)
                    continue;
                var angle = (wave * 47f + i * 90f) * Mathf.Deg2Rad;
                var spot = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6.5f;
                if (!manager.Nav.IsWalkable(IsoMath.GroundToCell(spot)))
                    continue;
                manager.Spawn(definition, spot, true, null, level.Layout.EnemyLevel);
            }
            wave++;
        }
    }

    /// <summary>
    /// Things to destroy with arrows (the rift heart, the bone pyres): standing enemies that call others while they
    /// stand. The step is done when the last falls.
    /// </summary>
    public class BreakTargets : MonoBehaviour
    {
        const float CallEvery = 7f;

        string questId;
        QuestStep step;
        List<Vector2Int> cells;
        DungeonLevel level;
        int depth;
        int levelSeed;
        int enemyLevel;
        EnemyManager manager;
        readonly List<EnemyController> targets = new List<EnemyController>();
        readonly Dictionary<EnemyController, GameObject> labels = new Dictionary<EnemyController, GameObject>();
        float timer;
        int call;

        public static void Create(Transform parent, string questId, QuestStep step, List<Vector2Int> cells, DungeonLevel level, int depth, int levelSeed, int enemyLevel)
        {
            var manager = FindAnyObjectByType<EnemyManager>();
            var definition = level != null ? level.DeepDefinition(step.Target == "rift_heart" ? "RiftHeart" : "BonePyre") : null;
            if (manager == null || definition == null)
            {
                // Without the definitions (Tools > ARPG > Add Quest Foes not run), the step is an object to reach.
                RelicSite.Create(parent, questId, step, cells[0]);
                return;
            }
            var go = new GameObject("Break " + step.Target);
            go.transform.SetParent(parent, false);
            var targets = go.AddComponent<BreakTargets>();
            targets.questId = questId;
            targets.step = step;
            targets.cells = cells;
            targets.level = level;
            targets.depth = depth;
            targets.levelSeed = levelSeed;
            targets.enemyLevel = enemyLevel;
            targets.manager = manager;
            foreach (var cell in cells)
            {
                var target = manager.Spawn(definition, IsoMath.CellToGround(cell), false, null, enemyLevel);
                targets.targets.Add(target);
                // The name hangs over the target, kept in this object (the enemy is pooled and reused).
                var label = new GameObject("Name");
                label.transform.SetParent(go.transform, false);
                label.transform.position = target.transform.position;
                TravelArt.Label(label.transform, step.Target == "rift_heart" ? "Rift Heart" : "Bone Pyre", new Color(1f, 0.5f, 0.3f), 1.6f);
                targets.labels.Add(target, label);
            }
            manager.Killed += targets.OnKilled;
            targets.timer = CallEvery;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = CallEvery;
            // Only while she is close: the call is the target answering her.
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || Vector2.Distance(target.GroundPosition, manager.PlayerGround) > 12f)
                    continue;
                var members = PackComposition.Roll(depth, PackKind.Normal, 2, levelSeed, 2000 + call++);
                foreach (var member in members)
                {
                    var definition = level.DefinitionOf(member);
                    var spot = target.GroundPosition + Random.insideUnitCircle.normalized * 2.5f;
                    if (definition != null && manager.Nav.IsWalkable(IsoMath.GroundToCell(spot)))
                        manager.Spawn(definition, spot, true, null, enemyLevel);
                }
            }
        }

        void OnKilled(EnemyController enemy)
        {
            if (!targets.Contains(enemy))
                return;
            targets.Remove(enemy);
            if (labels.TryGetValue(enemy, out var label))
                Destroy(label);
            if (targets.Count > 0)
            {
                HintBanner.Current?.Show($"{(step.Target == "rift_heart" ? "Rift Heart" : "A bone pyre")} falls. {targets.Count} left.");
                return;
            }
            manager.Killed -= OnKilled;
            foreach (var cell in cells)
                Minimap.QuestMarks.Remove(cell);
            QuestObjectives.Done(questId, step);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.Killed -= OnKilled;
        }
    }
}
