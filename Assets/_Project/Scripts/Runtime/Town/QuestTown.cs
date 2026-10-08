using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Talking to a quest giver (2026-10-08, Docs/05 Quests). Walking up to a giver with a quest to hand in, or one to
    /// offer, opens a conversation first (<see cref="DialoguePanel"/>); walking up again opens their usual trade.
    /// </summary>
    public static class QuestTalk
    {
        /// <summary>The baked character each giver's portrait and body use.</summary>
        public static string Character(QuestGiver giver) => giver switch
        {
            QuestGiver.Aldis => "aldis",
            QuestGiver.Merchant => "merchant",
            QuestGiver.Tamer => "pet_vendor",
            QuestGiver.Healer => "healer",
            QuestGiver.StashKeeper => "stash_keeper",
            QuestGiver.Gambler => "gambler",
            QuestGiver.Trainer => "trainer",
            QuestGiver.Ivy => "ivy",
            QuestGiver.Pellam => "pellam",
            _ => "bram",
        };

        /// <summary>Whether a giver has something to say: a quest to hand in, or one to offer.</summary>
        public static bool HasSomething(QuestGiver giver, out bool turnIn)
        {
            var session = GameSession.Current;
            turnIn = session.Quests.TurnInAt(giver) != null;
            return turnIn || session.Quests.OfferFrom(giver, session.DeepestDepth) != null;
        }

        /// <summary>
        /// Opens the giver's conversation when there is one (a hand-in first, then an offer) and returns true; false
        /// when there is nothing to say, so the caller opens its trade. With <paramref name="always"/> (a giver with no
        /// trade), a quest under way is spoken of instead.
        /// </summary>
        public static bool TryTalk(QuestGiver giver, bool always = false)
        {
            if (DialoguePanel.IsOpen)
                return true;
            var session = GameSession.Current;
            var quests = session.Quests;
            var name = QuestRules.GiverName(giver);
            var character = Character(giver);

            var turnIn = quests.TurnInAt(giver);
            if (turnIn != null)
            {
                DialoguePanel.Show(name, character, turnIn.Complete, "Farewell", () =>
                {
                    if (quests.TurnIn(turnIn.Id))
                        HintBanner.Current?.Show($"Quest done: {turnIn.Title}. {turnIn.Gift}");
                });
                return true;
            }

            var offer = quests.OfferFrom(giver, session.DeepestDepth);
            if (offer != null)
            {
                DialoguePanel.Show(name, character, offer.Offer, "Accept", () =>
                {
                    quests.Accept(offer.Id);
                    HintBanner.Current?.Show($"New quest: {offer.Title}. {quests.CurrentStep(offer.Id)?.Log}");
                }, "Not now");
                return true;
            }

            if (!always)
                return false;
            var underWay = quests.UnderWayFrom(giver);
            var words = underWay != null ? new[] { underWay.Progress } : new[] { Idle(giver) };
            DialoguePanel.Show(name, character, words, "Farewell", null);
            return true;
        }

        // What a giver with no trade says when there is no quest to talk about.
        static string Idle(QuestGiver giver) => giver switch
        {
            QuestGiver.Aldis => GameSession.Current.Quests.IsDone("first_watchman")
                ? "The Vigil burns. Rest, child. You have earned that much."
                : "Keep the ember close. It is the last warm thing in this land.",
            QuestGiver.Ivy => "I pray for you. Mostly. Sometimes I pray he burns.",
            QuestGiver.Pellam => "Light is a craft, not a gift. Mind your lantern.",
            _ => "Fair water today. Fair as it gets down there.",
        };
    }

    /// <summary>
    /// The gold mark over a giver's head (2026-10-08): <b>!</b> for a quest to offer, <b>?</b> for one to hand in,
    /// nothing otherwise. Checked a few times a second.
    /// </summary>
    public class QuestMarker : MonoBehaviour
    {
        QuestGiver giver;
        TextMesh mark;
        float timer;

        public static void Attach(Transform parent, QuestGiver giver, float height = 2.0f)
        {
            var go = new GameObject("Quest Marker", typeof(TextMesh));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            var mesh = go.GetComponent<TextMesh>();
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.characterSize = 0.12f;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = new Color(1f, 0.82f, 0.3f);
            go.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
            var marker = go.AddComponent<QuestMarker>();
            marker.giver = giver;
            marker.mark = mesh;
            marker.Refresh();
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            timer = 0.3f;
            Refresh();
        }

        void Refresh()
        {
            mark.text = QuestTalk.HasSomething(giver, out var turnIn) ? (turnIn ? "?" : "!") : "";
            // A gentle bob, so it catches the eye.
            mark.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 4f));
        }
    }

    /// <summary>The town's quest people with no trade of their own: Mother Aldis, and those she brings back.</summary>
    public enum QuestPerson
    {
        Aldis,
        Tobin,
        Pellam,
        Bram,
        Ivy,
    }

    /// <summary>
    /// A quest person in town (2026-10-08): Mother Aldis by the Vigil fire from the start; Tobin, Pellam, Bram and Ivy
    /// once rescued. Walking up talks (<see cref="QuestTalk"/>); Bram, with nothing to say, rows her to her body when one
    /// lies in the dungeon.
    /// </summary>
    public class QuestNpc : WalkOnTrigger
    {
        QuestPerson who;

        public static string Name(QuestPerson who) => who switch
        {
            QuestPerson.Aldis => "Mother Aldis",
            QuestPerson.Tobin => "Tobin",
            QuestPerson.Pellam => "Pellam",
            QuestPerson.Bram => "Bram",
            _ => "Sister Ivy",
        };

        public static string Character(QuestPerson who) => who.ToString().ToLowerInvariant();

        /// <summary>Where each stands in town (clear of the town's pieces; checked by a test).</summary>
        public static Vector2Int Cell(QuestPerson who) => who switch
        {
            QuestPerson.Aldis => new Vector2Int(2, 10),
            QuestPerson.Tobin => new Vector2Int(-6, -1),
            QuestPerson.Pellam => new Vector2Int(5, -6),
            QuestPerson.Bram => new Vector2Int(1, -11),
            _ => new Vector2Int(-3, 12),
        };

        /// <summary>The rescue that brings each to town ("" for Aldis, who is there from the start).</summary>
        public static string RescueTarget(QuestPerson who) => who == QuestPerson.Aldis ? "" : who.ToString().ToLowerInvariant();

        public static bool InTown(QuestPerson who) => who == QuestPerson.Aldis || GameSession.Current.Quests.IsRescued(RescueTarget(who));

        static QuestGiver? GiverOf(QuestPerson who) => who switch
        {
            QuestPerson.Aldis => QuestGiver.Aldis,
            QuestPerson.Pellam => QuestGiver.Pellam,
            QuestPerson.Bram => QuestGiver.Bram,
            QuestPerson.Ivy => QuestGiver.Ivy,
            _ => null,
        };

        public static QuestNpc Create(QuestPerson who, Transform parent)
        {
            var go = new GameObject(Name(who), typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(Cell(who)));
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.9f;
            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;
            NpcFigure.Create(go.transform, Character(who), new Color(0.6f, 0.55f, 0.45f));
            TravelArt.Label(go.transform, Name(who), LootColors.Gold, 1.5f);
            var giver = GiverOf(who);
            if (giver.HasValue)
                QuestMarker.Attach(go.transform, giver.Value);
            var npc = go.AddComponent<QuestNpc>();
            npc.who = who;
            return npc;
        }

        protected override void OnWalkedOn()
        {
            var bag = InventoryScreen.Current;
            if ((bag != null && bag.IsOpen) || PetVendorScreen.IsOpen || TownSheets.AnyOpen || DialoguePanel.IsOpen)
                return;
            var giver = GiverOf(who);
            if (who == QuestPerson.Bram && !QuestTalk.TryTalk(QuestGiver.Bram))
            {
                Ferry();
                return;
            }
            if (giver.HasValue)
            {
                QuestTalk.TryTalk(giver.Value, true);
                return;
            }
            DialoguePanel.Show(Name(who), Character(who), new[] { TobinLine() }, "Farewell", null);
        }

        static string TobinLine() => "Odo says I am not to go below again. I am going below again. Not yet, though.";

        // Bram rows her to the level where her body lies (2026-10-08), arriving at its waypoint.
        static void Ferry()
        {
            var session = GameSession.Current;
            var depth = 0;
            foreach (var corpse in session.Corpses)
                if (DungeonRules.IsDungeonKey(corpse.LevelId) && int.TryParse(corpse.LevelId.Substring(corpse.LevelId.LastIndexOf(' ') + 1), out var d))
                    depth = Mathf.Max(depth, d);
            if (depth == 0)
            {
                DialoguePanel.Show(Name(QuestPerson.Bram), "bram", new[] { "Nobody of yours down there to fetch. Good. Keep it that way." }, "Farewell", null);
                return;
            }
            DialoguePanel.Show(Name(QuestPerson.Bram), "bram",
                new[] { $"Your body is on the {Ordinal(depth)} level. I know the water down there. Want a ride?" },
                "Row me there", () => SceneTravel.ToDepth(depth, Arrival.AtWaypoint), "Not now");
        }

        static string Ordinal(int n) => n % 100 is 11 or 12 or 13 ? n + "th" : (n % 10) switch { 1 => n + "st", 2 => n + "nd", 3 => n + "rd", _ => n + "th" };
    }

    /// <summary>
    /// Emberwatch's Vigil fire in the square (2026-10-08): dead until the first ember comes back, then burning, its
    /// light growing with each ember (the main quest's four steps home).
    /// </summary>
    public static class VigilFire
    {
        public static readonly Vector2Int Cell = new Vector2Int(0, 9);

        /// <summary>How many embers have come home: the main quests handed in.</summary>
        public static int Embers(QuestState quests)
        {
            var count = 0;
            foreach (var quest in QuestRules.All)
                if (quest.Main && quests.IsDone(quest.Id))
                    count++;
            return count;
        }

        public static void Create(Transform parent)
        {
            var embers = Embers(GameSession.Current.Quests);
            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(Cell));
            var fire = WorldArt.Place(embers > 0 ? "vigil_fire_lit" : "vigil_fire_unlit", parent, new Vector3(world.x, world.y, 0f));
            if (fire == null)
                return;
            WorldArt.Lit(fire);
            if (embers > 0)
            {
                var light = WorldLights.Add(fire.transform, new Color(1f, 0.6f, 0.3f), 1.0f + 0.25f * embers, 0.3f, 5f + 1.5f * embers, 0.6f);
                if (light != null)
                    fire.gameObject.AddComponent<FlickerLight>().Init(light, 3.1f);
            }
        }
    }
}
