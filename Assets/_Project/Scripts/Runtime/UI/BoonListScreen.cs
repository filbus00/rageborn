namespace ARPG
{
    /// <summary>
    /// The Bag's Boons tab in the roguelite (2026-10-17, the owner: "make the menus better organized"; it was the old
    /// talent trees, with points and a Learn button the roguelite does not use): the boons taken this run with their
    /// icons, ranks and what they do, then the boons still to be found (which need another first, which are rare), and
    /// how many more Sylthara's Hidden Lore would open. A scrolling sheet (<see cref="TownSheets"/>).
    /// </summary>
    public class BoonListScreen : TownSheets
    {
        static BoonListScreen current;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        protected override bool FullPage => true;

        public static void Open()
        {
            if (current == null)
                current = Make<BoonListScreen>("Boons");
            current.Show();
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        protected override void Render()
        {
            var session = GameSession.Current;
            var talents = session.Talents;
            var maxTier = session.Meta.MaxBoonTier;
            var taken = 0;
            foreach (var node in TalentRules.Nodes)
                if (talents.RankOf(node.Id) > 0)
                    taken++;
            var level = UnityEngine.Object.FindAnyObjectByType<DungeonLevel>();
            subtitle.text = level != null
                ? $"Depth {level.Depth}.  {taken} boon{(taken == 1 ? "" : "s")} this run; each new depth offers more."
                : taken > 0 ? $"{taken} boon{(taken == 1 ? "" : "s")} from this run." : "Each new depth of a run offers three boons to choose from.";

            Heading(taken > 0 ? "This run" : "This run: none yet");
            foreach (var node in TalentRules.Nodes)
            {
                var rank = talents.RankOf(node.Id);
                if (rank <= 0)
                    continue;
                SheetKit.Card(content, $"<b>{node.Name}</b>{SheetKit.Pips(rank, node.MaxRanks)}\n<size=27><color=#CCCCCC>{TalentRules.Describe(node, rank)}</color></size>",
                    null, false, null, 110f, 0f, false, BoonBar.SpriteFor(node), BoonIconArt.TreeColor(node.Tree));
            }

            Heading("Still to find");
            var hidden = 0;
            foreach (var node in TalentRules.Nodes)
            {
                var rank = talents.RankOf(node.Id);
                if (rank > 0)
                    continue;
                if (node.Tier >= maxTier)
                {
                    hidden++;
                    continue;
                }
                var needs = node.Requires != null && talents.RankOf(node.Requires) <= 0 ? $"  <color=#C07050>needs {TalentRules.Get(node.Requires)?.Name}</color>" : "";
                var rare = node.IsRare ? "  <color=#E0B040>RARE</color>" : "";
                SheetKit.Card(content, $"<color=#B8AC9C><b>{node.Name}</b></color><size=24>{rare}{needs}</size>\n" +
                                       $"<size=25><color=#8C8478>{TalentRules.Describe(node, 1)}</color></size>",
                    null, false, null, 96f, 0f, false, BoonBar.SpriteFor(node), SheetKit.CardEdge);
            }
            if (hidden > 0)
                SheetKit.Note(content, $"{hidden} more boons are kept secret until Sylthara teaches Hidden Lore.", 28);
        }
    }
}
