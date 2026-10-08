using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What a quest step asks (2026-10-08, Docs/05 Quests).</summary>
    public enum QuestStepKind
    {
        /// <summary>Free someone held in a lair by walking up once their captors are gone.</summary>
        Rescue,

        /// <summary>Kill a unique named enemy (some carry an object to recover).</summary>
        NamedFoe,

        /// <summary>Take an object from a lectern, an altar or a body by walking up.</summary>
        Recover,

        /// <summary>Light a dead watch fire and keep near it while the dead come for it.</summary>
        HoldFire,

        /// <summary>Destroy things with arrows that call enemies while they stand.</summary>
        Break,

        /// <summary>Kill the depth's boss.</summary>
        Boss,

        /// <summary>Go back to the quest's giver in town.</summary>
        Return,
    }

    /// <summary>Who gives quests: the town's people, and those she brings back.</summary>
    public enum QuestGiver
    {
        Aldis,
        Merchant,
        Tamer,
        Healer,
        StashKeeper,
        Gambler,
        Trainer,
        Ivy,
        Pellam,
        Bram,
    }

    /// <summary>What a finished quest gives the town, and her (the owner: "Town grows").</summary>
    public enum QuestBoon
    {
        None,
        LifeBlessing,
        DamageBlessing,
        MerchantPrices,
        PetPower,
        Lantern,
        MapStairs,
        GambleLegendary,
        AttackSpeed,
        Potions,
        GreatLamp,
    }

    public sealed class QuestStep
    {
        public QuestStepKind Kind;

        /// <summary>The depth the step is done on (0 for a return to town).</summary>
        public int Depth;

        /// <summary>What the step places or names: a person, a foe, an object (see <see cref="QuestRules.TargetName"/>).</summary>
        public string Target;

        /// <summary>The quest log's line for the step.</summary>
        public string Log;

        /// <summary>Said in a banner when the step is done in the dungeon.</summary>
        public string DoneLine;
    }

    public sealed class QuestDefinition
    {
        public string Id;
        public string Title;
        public QuestGiver Giver;
        public bool Main;

        /// <summary>A quest that must be finished first, or null.</summary>
        public string After;

        /// <summary>The deepest depth reached before it is offered.</summary>
        public int FromDepth;

        public string[] Offer;
        public string Progress;
        public string[] Complete;
        public QuestStep[] Steps;
        public QuestBoon Boon;

        /// <summary>What the town gains, said on the finished quest in the log.</summary>
        public string Gift;
    }

    /// <summary>
    /// The quests (2026-10-08): four main quests from Mother Aldis and eight side quests from the town's people, each a
    /// few steps ending with a return to the giver. Content and numbers are Claude's, to review (Docs/05 Quests). Pure.
    /// </summary>
    public static class QuestRules
    {
        public static IReadOnlyList<QuestDefinition> All { get; } = Build();

        static readonly Dictionary<string, QuestDefinition> ById = Index();

        static Dictionary<string, QuestDefinition> Index()
        {
            var map = new Dictionary<string, QuestDefinition>();
            foreach (var quest in All)
                map[quest.Id] = quest;
            return map;
        }

        public static QuestDefinition Get(string id) => id != null && ById.TryGetValue(id, out var quest) ? quest : null;

        /// <summary>A giver's name as the dialogue shows it.</summary>
        public static string GiverName(QuestGiver giver) => giver switch
        {
            QuestGiver.Aldis => "Mother Aldis",
            QuestGiver.Merchant => "Odo the Merchant",
            QuestGiver.Tamer => "Kestra the Beast Tamer",
            QuestGiver.Healer => "Maud the Healer",
            QuestGiver.StashKeeper => "Wenn the Stash Keeper",
            QuestGiver.Gambler => "Silas the Gambler",
            QuestGiver.Trainer => "Hale the Trainer",
            QuestGiver.Ivy => "Sister Ivy",
            QuestGiver.Pellam => "Pellam the Lampwright",
            _ => "Bram the Ferryman",
        };

        /// <summary>The name of what a step's target is, as banners and the minimap say it.</summary>
        public static string TargetName(string target) => target switch
        {
            "watch_log" => "the Warden's Watch-Log",
            "bram" => "Bram the Ferryman",
            "ivy" => "Sister Ivy",
            "reliquary_keeper" => "the Reliquary Keeper",
            "rift_heart" => "the Rift Heart",
            "oath_stone" => "the Broken Oath-Stone",
            "tobin" => "Tobin",
            "gorrak" => "Gorrak the Gnawer",
            "pellam" => "Pellam",
            "cartographer_notes" => "the Cartographer's Notes",
            "vessa" => "Vessa the Knife",
            "aldric" => "Sir Aldric the Unquiet",
            "bone_pyres" => "the Bone Pyres",
            "great_lamp" => "the Great Lamp",
            "drowned_fire" => "the Drowned Watch's Fire",
            _ => target,
        };

        /// <summary>A target's name to stand alone (over its head): capitalised.</summary>
        public static string Title(string target)
        {
            var name = TargetName(target);
            return name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
        }

        static QuestStep Step(QuestStepKind kind, int depth, string target, string log, string done = null) =>
            new QuestStep { Kind = kind, Depth = depth, Target = target, Log = log, DoneLine = done };

        static QuestStep Back(string log) => new QuestStep { Kind = QuestStepKind.Return, Log = log };

        static List<QuestDefinition> Build() => new List<QuestDefinition>
        {
            // ---- The main quest: Mother Aldis, by Emberwatch's dead fire.
            new QuestDefinition
            {
                Id = "first_fire", Title = "The First Fire", Giver = QuestGiver.Aldis, Main = true, FromDepth = 0,
                Offer = new[]
                {
                    "You carry an ember. I can feel it from here, child.",
                    "This fire burned for four hundred years. It went out the night the dead came up from the crypts.",
                    "The Cinder Warden kept the first fire, below. Find his watch-log. Learn what became of him.",
                },
                Progress = "The Warden's log will be at his old watch post. Four levels down, if the old maps are right.",
                Complete = new[]
                {
                    "His ember. Still warm.",
                    "He would not let the fire die, so he became it. Give it here.",
                    "There. Emberwatch has a fire again. Stand near it and you will feel the old blessing.",
                },
                Steps = new[]
                {
                    Step(QuestStepKind.Recover, 4, "watch_log", "Find the Cinder Warden's watch-log (depth 4).",
                        "The log's last page: \"It will not die while I stand. I go into it.\""),
                    Step(QuestStepKind.Boss, 6, "warden", "Slay the Cinder Warden (depth 6).", "The Warden's ember is yours."),
                    Back("Bring the first ember to Mother Aldis."),
                },
                Boon = QuestBoon.LifeBlessing, Gift = "Emberwatch's fire burns: +5% life.",
            },
            new QuestDefinition
            {
                Id = "drowned_watch", Title = "The Drowned Watch", Giver = QuestGiver.Aldis, Main = true, After = "first_fire", FromDepth = 6,
                Offer = new[]
                {
                    "Below the Warden's halls the water came in. The watch there kept a fire too.",
                    "The watchmen chose the water over the Hollow. Light their fire, and the dead will remember what they swore.",
                    "And find Bram. He ferried the watch. If anyone lives down there, it is that stubborn fool.",
                },
                Progress = "The drowned fire first, then Bram. The Tidewife holds the second ember.",
                Complete = new[]
                {
                    "The second ember. She called them into the water, and you sent her after them.",
                    "The fire grows. So does the town. Bram has been telling everyone about you.",
                },
                Steps = new[]
                {
                    Step(QuestStepKind.HoldFire, 9, "drowned_fire", "Relight the Drowned Watch's fire and hold it (depth 9).",
                        "The fire catches. The drowned fall back from it."),
                    Step(QuestStepKind.Rescue, 11, "bram", "Find Bram the ferryman in the drowned's hold (depth 11).",
                        "Bram: \"About time. Go on, I know the way up.\""),
                    Step(QuestStepKind.Boss, 12, "tidewife", "Slay the Tidewife (depth 12).", "The Tidewife's ember is yours."),
                    Back("Bring the second ember to Mother Aldis."),
                },
                Boon = QuestBoon.DamageBlessing, Gift = "The fire grows: +5% damage. Bram stays in town.",
            },
            new QuestDefinition
            {
                Id = "saints_bargain", Title = "The Saint's Bargain", Giver = QuestGiver.Aldis, Main = true, After = "drowned_watch", FromDepth = 12,
                Offer = new[]
                {
                    "Saint Marrow kept the third fire. They say he kept the dead quiet. They do not say how.",
                    "A sister of his order went down to pray at his tomb and never came back. Ivy. Find her.",
                    "His reliquary holds his sermons. Bring it, and we will know what he bargained.",
                },
                Progress = "Ivy first. Then the reliquary. Then the saint himself.",
                Complete = new[]
                {
                    "He fed them the dead, and the Hollow kept him alive for it. Four hundred years of it.",
                    "The third ember. Ivy is praying by the fire. She will not say for whom.",
                },
                Steps = new[]
                {
                    Step(QuestStepKind.Rescue, 14, "ivy", "Find Sister Ivy, sealed in a tomb (depth 14).",
                        "Ivy: \"He is not a saint. He never was. I will find my own way up.\""),
                    Step(QuestStepKind.NamedFoe, 16, "reliquary_keeper", "Take Saint Marrow's reliquary from the Reliquary Keeper (depth 16).",
                        "The reliquary is yours. Inside, the saint's sermons, and a bargain in his hand."),
                    Step(QuestStepKind.Boss, 18, "saint_marrow", "Slay Saint Marrow (depth 18).", "Saint Marrow's ember is yours."),
                    Back("Bring the third ember to Mother Aldis."),
                },
                Boon = QuestBoon.LifeBlessing, Gift = "The fire roars: +5% life. Ivy stays in town.",
            },
            new QuestDefinition
            {
                Id = "first_watchman", Title = "The First Watchman", Giver = QuestGiver.Aldis, Main = true, After = "saints_bargain", FromDepth = 18,
                Offer = new[]
                {
                    "The first fire was his. The First Watchman. His failure is the night we live in.",
                    "Something feeds the deep. A rift, beating like a heart. Break it.",
                    "And find his oath-stone. A watchman's oath was cut in stone. I want to read what he broke.",
                },
                Progress = "The heart, the stone, then him. Come back with the last ember.",
                Complete = new[]
                {
                    "Four embers. Four fires. I did not think I would live to see it.",
                    "Stand back. ... There. The Vigil burns.",
                    "The Hollow will remember this. Let it.",
                },
                Steps = new[]
                {
                    Step(QuestStepKind.Break, 20, "rift_heart", "Break the rift heart that feeds the deep (depth 20).",
                        "The rift heart bursts. The deep goes quiet, for a moment."),
                    Step(QuestStepKind.Recover, 22, "oath_stone", "Recover the Broken Oath-Stone (depth 22).",
                        "The oath-stone: \"I will hold the dark.\" Below it, scratched later: \"I could not.\""),
                    Step(QuestStepKind.Boss, 24, "first_watchman", "Slay the First Watchman (depth 24).", "The last ember is yours."),
                    Back("Bring the last ember to Mother Aldis."),
                },
                Boon = QuestBoon.DamageBlessing, Gift = "The Vigil burns again: +5% damage.",
            },

            // ---- Side quests.
            new QuestDefinition
            {
                Id = "apprentice", Title = "The Apprentice", Giver = QuestGiver.Merchant, FromDepth = 1,
                Offer = new[]
                {
                    "My boy Tobin went down for salvage. Brass, nails, anything I can sell.",
                    "Two days now. The husks were feeding near the second stair, he said. He was proud of finding them.",
                    "Bring him back. I will pay you better for everything you sell me. My word on it.",
                },
                Progress = "Tobin. Second level. Please.",
                Complete = new[] { "Tobin! You stupid, brave boy.", "He minds the stall now, and I keep my word: better prices, for you." },
                Steps = new[]
                {
                    Step(QuestStepKind.Rescue, 2, "tobin", "Find Tobin, the merchant's apprentice (depth 2).", "Tobin: \"I am not crying. Let's go.\""),
                    Back("Tell Odo his apprentice is safe."),
                },
                Boon = QuestBoon.MerchantPrices, Gift = "Tobin minds the stall: the merchant pays 25% more.",
            },
            new QuestDefinition
            {
                Id = "gorrak", Title = "Gorrak the Gnawer", Giver = QuestGiver.Tamer, FromDepth = 1,
                Offer = new[]
                {
                    "Something came up through the cellar and took my hounds. Three of them. I found the drag marks.",
                    "A ghoul. A big one. The others follow it. They call it Gorrak, the ones who still talk.",
                    "Kill it. Then my pens are safe, and the beasts I sell you will be the stronger for it.",
                },
                Progress = "His pit is on the third level. You will smell it.",
                Complete = new[] { "Gorrak is dead? Then the pups can sleep.", "I will raise them hard. Every beast you take from me will be tougher." },
                Steps = new[]
                {
                    Step(QuestStepKind.NamedFoe, 3, "gorrak", "Kill Gorrak the Gnawer in his feeding pit (depth 3).", "Gorrak the Gnawer is dead."),
                    Back("Tell Kestra that Gorrak is dead."),
                },
                Boon = QuestBoon.PetPower, Gift = "Kestra's pens are safe: pets have 25% more life and damage.",
            },
            new QuestDefinition
            {
                Id = "lamp_in_the_dark", Title = "A Lamp in the Dark", Giver = QuestGiver.Healer, FromDepth = 4,
                Offer = new[]
                {
                    "My brother Pellam made the lamps for the watch. Good lamps. The cultists wanted them for their rites.",
                    "They took him, not the lamps. Fifth level, the shrine where the candles never go out.",
                    "Bring him home. He will light your way down there better than any torch.",
                },
                Progress = "Pellam. The cultists' shrine, fifth level.",
                Complete = new[] { "Pellam. Look at you.", "He is already at his bench. Take his lantern; it is the best he has made." },
                Steps = new[]
                {
                    Step(QuestStepKind.Rescue, 5, "pellam", "Find Pellam the lampwright, taken by cultists (depth 5).", "Pellam: \"My sister sent you? Of course she did.\""),
                    Back("Tell Maud her brother is safe."),
                },
                Boon = QuestBoon.Lantern, Gift = "Pellam's lantern: your light reaches 30% further below.",
            },
            new QuestDefinition
            {
                Id = "cartographer", Title = "The Cartographer's Notes", Giver = QuestGiver.StashKeeper, FromDepth = 2,
                Offer = new[]
                {
                    "The watch had a cartographer. Old Feln. He mapped every stair from here to the bottom.",
                    "He died in the tombs on the seventh level, with his notes. Nobody went back for them.",
                    "Bring them. I will pin them up, and you will always know where the way down is.",
                },
                Progress = "Feln's notes. The tombs, seventh level.",
                Complete = new[] { "Feln's hand. Still neat, after all this.", "I will copy them for you. You will see the stairs down the moment you arrive." },
                Steps = new[]
                {
                    Step(QuestStepKind.Recover, 7, "cartographer_notes", "Recover the watch cartographer's notes (depth 7).", "Feln's notes, every stair marked."),
                    Back("Bring the notes to Wenn."),
                },
                Boon = QuestBoon.MapStairs, Gift = "Feln's maps: the minimap shows the stairs down as you arrive.",
            },
            new QuestDefinition
            {
                Id = "vessa", Title = "Vessa the Knife", Giver = QuestGiver.Gambler, FromDepth = 8,
                Offer = new[]
                {
                    "I owe money. Not to a bank. To Vessa. She runs the knives on the ninth level.",
                    "She sends a man up every month to take a finger's worth. I am running out of fingers.",
                    "If she were to stop being alive, my luck would change. And luck is what I sell.",
                },
                Progress = "Vessa's camp. Ninth level. Mind the knives.",
                Complete = new[] { "She is dead? Truly? Then I am a free man.", "My luck is yours: my gambles will turn up legendaries twice as often." },
                Steps = new[]
                {
                    Step(QuestStepKind.NamedFoe, 9, "vessa", "Kill Vessa the Knife at her camp (depth 9).", "Vessa the Knife is dead."),
                    Back("Tell Silas his debt is settled."),
                },
                Boon = QuestBoon.GambleLegendary, Gift = "Silas's luck: a gambled Legendary is twice as likely.",
            },
            new QuestDefinition
            {
                Id = "unquiet_knight", Title = "The Unquiet Knight", Giver = QuestGiver.Trainer, FromDepth = 12,
                Offer = new[]
                {
                    "Sir Aldric trained me. Best blade in the watch. He died on the thirteenth level, holding a stair.",
                    "The men coming up say he still holds it. Bones and armour and nothing behind the eyes.",
                    "Put him down. Bring me his sword. I will teach you what he taught me.",
                },
                Progress = "Aldric. The thirteenth level. Bring the sword.",
                Complete = new[] { "His sword. He would hate how notched it is.", "Now. Again, faster. That is how he taught it." },
                Steps = new[]
                {
                    Step(QuestStepKind.NamedFoe, 13, "aldric", "Lay Sir Aldric to rest and take his sword (depth 13).", "Sir Aldric rests. His sword is yours."),
                    Back("Bring Aldric's sword to Hale."),
                },
                Boon = QuestBoon.AttackSpeed, Gift = "Hale's drills: +5% attack speed.",
            },
            new QuestDefinition
            {
                Id = "last_novice", Title = "The Last Novice", Giver = QuestGiver.Ivy, After = "saints_bargain", FromDepth = 14,
                Offer = new[]
                {
                    "The grave priests still keep the saint's rite. Three pyres of bone on the fifteenth level.",
                    "While they burn, the dead there cannot rest. I was a novice of that rite. I helped build them.",
                    "Break them. All three. I will pray for you, and my prayers are worth something now.",
                },
                Progress = "Three pyres. Fifteenth level.",
                Complete = new[] { "They are cold? Then it is over.", "Take my blessing on your draughts. They will mend more, and you will carry one more." },
                Steps = new[]
                {
                    Step(QuestStepKind.Break, 15, "bone_pyres", "Break the grave priests' three bone pyres (depth 15).", "The last pyre falls. The rite is broken."),
                    Back("Tell Sister Ivy the rite is broken."),
                },
                Boon = QuestBoon.Potions, Gift = "Ivy's blessing: potions heal 25% more, and one more charge.",
            },
            new QuestDefinition
            {
                Id = "great_lamp", Title = "The Great Lamp", Giver = QuestGiver.Pellam, After = "lamp_in_the_dark", FromDepth = 18,
                Offer = new[]
                {
                    "The watch's great lamp. I built it. Brass and glass taller than me. It lit the deep stair for a hundred years.",
                    "Demons dragged it into a nest on the nineteenth level. They like the light, I think. It hurts them and they like it.",
                    "Bring it home. I will hang it over the fire, and your lantern will draw on it.",
                },
                Progress = "The great lamp. Nineteenth level.",
                Complete = new[] { "Scratched, dented, still whole. Like the rest of us.", "There. Feel that? Your light goes further now." },
                Steps = new[]
                {
                    Step(QuestStepKind.Recover, 19, "great_lamp", "Recover the watch's great lamp from a demon nest (depth 19).", "The great lamp. Heavy, and still warm."),
                    Back("Bring the great lamp to Pellam."),
                },
                Boon = QuestBoon.GreatLamp, Gift = "The great lamp: your light reaches a further 30% below.",
            },
        };
    }

    /// <summary>One quest's place: its current step, or done.</summary>
    [Serializable]
    public struct QuestProgress
    {
        public int Step;
        public bool Done;
    }

    /// <summary>
    /// What the town and the player have from finished quests (the owner: "Town grows"). Multipliers are 1 when nothing
    /// applies; percents are fractions.
    /// </summary>
    public sealed class TownBoons
    {
        public float LifePercent;
        public float DamagePercent;
        public float SellPrice = 1f;
        public float PetPower = 1f;
        public float Light = 1f;
        public bool MapStairs;
        public float LegendaryGamble = 1f;
        public float AttackSpeed;
        public float PotionHeal = 1f;
        public int PotionCharges;

        public static TownBoons From(QuestState quests)
        {
            var boons = new TownBoons();
            foreach (var quest in QuestRules.All)
            {
                if (!quests.IsDone(quest.Id))
                    continue;
                switch (quest.Boon)
                {
                    case QuestBoon.LifeBlessing: boons.LifePercent += 0.05f; break;
                    case QuestBoon.DamageBlessing: boons.DamagePercent += 0.05f; break;
                    case QuestBoon.MerchantPrices: boons.SellPrice *= 1.25f; break;
                    case QuestBoon.PetPower: boons.PetPower *= 1.25f; break;
                    case QuestBoon.Lantern:
                    case QuestBoon.GreatLamp: boons.Light += 0.3f; break;
                    case QuestBoon.MapStairs: boons.MapStairs = true; break;
                    case QuestBoon.GambleLegendary: boons.LegendaryGamble *= 2f; break;
                    case QuestBoon.AttackSpeed: boons.AttackSpeed += 0.05f; break;
                    case QuestBoon.Potions:
                        boons.PotionHeal *= 1.25f;
                        boons.PotionCharges += 1;
                        break;
                }
            }
            return boons;
        }
    }

    /// <summary>
    /// The player's quests: which are taken, their current steps, which are done. Steps in the dungeon are marked done
    /// as they happen; a return step finishes when the giver is spoken to. Pure.
    /// </summary>
    public sealed class QuestState
    {
        readonly Dictionary<string, QuestProgress> progress = new Dictionary<string, QuestProgress>();
        TownBoons boons;

        public event Action Changed;

        public IEnumerable<KeyValuePair<string, QuestProgress>> All => progress;

        public bool IsActive(string id) => progress.TryGetValue(id, out var p) && !p.Done;
        public bool IsDone(string id) => progress.TryGetValue(id, out var p) && p.Done;
        public bool IsTaken(string id) => progress.ContainsKey(id);

        /// <summary>The current step of an active quest, or null.</summary>
        public QuestStep CurrentStep(string id)
        {
            var quest = QuestRules.Get(id);
            if (quest == null || !progress.TryGetValue(id, out var p) || p.Done)
                return null;
            return quest.Steps[Mathf.Clamp(p.Step, 0, quest.Steps.Length - 1)];
        }

        public int StepIndex(string id) => progress.TryGetValue(id, out var p) ? p.Step : 0;

        public TownBoons Boons => boons ??= TownBoons.From(this);

        /// <summary>Whether a quest can be offered now: not taken, its predecessor done, deep enough, its giver here.</summary>
        public bool CanOffer(QuestDefinition quest, int deepestDepth)
        {
            if (IsTaken(quest.Id) || deepestDepth < quest.FromDepth)
                return false;
            if (quest.After != null && !IsDone(quest.After))
                return false;
            return GiverPresent(quest.Giver, deepestDepth);
        }

        /// <summary>Whether a giver is in town: always for the first four, by arrival for the newcomers, once rescued for
        /// those she brings back.</summary>
        public bool GiverPresent(QuestGiver giver, int deepestDepth) => giver switch
        {
            QuestGiver.Healer => Newcomers.HasArrived(Newcomer.Healer, deepestDepth),
            QuestGiver.StashKeeper => Newcomers.HasArrived(Newcomer.StashKeeper, deepestDepth),
            QuestGiver.Gambler => Newcomers.HasArrived(Newcomer.Gambler, deepestDepth),
            QuestGiver.Trainer => Newcomers.HasArrived(Newcomer.Trainer, deepestDepth),
            QuestGiver.Ivy => IsRescued("ivy"),
            QuestGiver.Pellam => IsRescued("pellam"),
            QuestGiver.Bram => IsRescued("bram"),
            _ => true,
        };

        /// <summary>Whether someone held below has been freed (their rescue step passed).</summary>
        public bool IsRescued(string person)
        {
            foreach (var quest in QuestRules.All)
                for (var i = 0; i < quest.Steps.Length; i++)
                    if (quest.Steps[i].Kind == QuestStepKind.Rescue && quest.Steps[i].Target == person)
                        return progress.TryGetValue(quest.Id, out var p) && (p.Done || p.Step > i);
            return false;
        }

        /// <summary>The first quest a giver can offer now, or null.</summary>
        public QuestDefinition OfferFrom(QuestGiver giver, int deepestDepth)
        {
            foreach (var quest in QuestRules.All)
                if (quest.Giver == giver && CanOffer(quest, deepestDepth))
                    return quest;
            return null;
        }

        /// <summary>An active quest of this giver's waiting to be handed in, or null.</summary>
        public QuestDefinition TurnInAt(QuestGiver giver)
        {
            foreach (var quest in QuestRules.All)
                if (quest.Giver == giver && CurrentStep(quest.Id)?.Kind == QuestStepKind.Return)
                    return quest;
            return null;
        }

        /// <summary>An active quest of this giver's still under way (not yet to hand in), or null.</summary>
        public QuestDefinition UnderWayFrom(QuestGiver giver)
        {
            foreach (var quest in QuestRules.All)
                if (quest.Giver == giver && IsActive(quest.Id) && CurrentStep(quest.Id)?.Kind != QuestStepKind.Return)
                    return quest;
            return null;
        }

        public void Accept(string id)
        {
            if (QuestRules.Get(id) == null || IsTaken(id))
                return;
            progress[id] = new QuestProgress();
            Notify();
        }

        /// <summary>Marks a quest's current step done if it is the given kind and target; true when it was.</summary>
        public bool CompleteStep(string id, string target)
        {
            var step = CurrentStep(id);
            if (step == null || step.Target != target || step.Kind == QuestStepKind.Return)
                return false;
            var p = progress[id];
            p.Step++;
            progress[id] = p;
            Notify();
            return true;
        }

        /// <summary>Hands a quest in at its giver: done, and its boon applies.</summary>
        public bool TurnIn(string id)
        {
            if (CurrentStep(id)?.Kind != QuestStepKind.Return)
                return false;
            progress[id] = new QuestProgress { Step = QuestRules.Get(id).Steps.Length, Done = true };
            Notify();
            return true;
        }

        /// <summary>The quests whose current step is done on this depth, with the step (not returns).</summary>
        public List<(QuestDefinition quest, QuestStep step)> StepsAt(int depth)
        {
            var list = new List<(QuestDefinition, QuestStep)>();
            foreach (var quest in QuestRules.All)
            {
                var step = CurrentStep(quest.Id);
                if (step != null && step.Depth == depth && step.Kind != QuestStepKind.Return)
                    list.Add((quest, step));
            }
            return list;
        }

        /// <summary>The step to show on the tracker line: the main quest's, else the first side quest's.</summary>
        public (QuestDefinition quest, QuestStep step) Tracked()
        {
            foreach (var main in new[] { true, false })
                foreach (var quest in QuestRules.All)
                    if (quest.Main == main && IsActive(quest.Id))
                        return (quest, CurrentStep(quest.Id));
            return (null, null);
        }

        public void Restore(string id, int step, bool done)
        {
            if (QuestRules.Get(id) == null)
                return;
            progress[id] = new QuestProgress { Step = step, Done = done };
            boons = null;
        }

        void Notify()
        {
            boons = null;
            Changed?.Invoke();
        }
    }
}
