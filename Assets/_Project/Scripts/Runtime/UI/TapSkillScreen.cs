namespace ARPG
{
    /// <summary>
    /// The Bag's Skills page (2026-11-24, the owner: "show the skill in the bag menu. Prepare the menu so that the player can
    /// select which skill to use when they are learned. They will all be triggered by double tapping the move button"): every
    /// double-tap skill, the learned ones with a button to choose it (one at a time), the rest greyed with where their book
    /// comes from. A scrolling sheet like the Boons page (<see cref="TownSheets"/>).
    /// </summary>
    public class TapSkillScreen : TownSheets
    {
        static TapSkillScreen current;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        protected override bool FullPage => true;

        public static void Open()
        {
            if (current == null)
                current = Make<TapSkillScreen>("Skills");
            current.Show();
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        protected override void Render()
        {
            var company = GameSession.Current.Company;
            var learned = company.TapSkills.Count;
            subtitle.text = learned > 0
                ? "Double tap the move stick to use the chosen skill. Choose one below."
                : "No skills learned yet. Bosses drop skill books: walk over one to learn its skill.";

            Heading("Double tap skills");
            foreach (var skill in TapSkillRules.All)
            {
                var known = company.TapSkills.Contains(skill.Id);
                var chosen = known && company.ChosenTapSkill == skill.Id;
                if (!known)
                {
                    SheetKit.Card(content, $"<color=#B8AC9C><b>{skill.Name}</b></color>  <size=24><color=#C07050>not learned</color></size>\n" +
                                           $"<size=25><color=#8C8478>{skill.Text}  A boss drops its book.</color></size>",
                        null, false, null, 110f, 0f, false, null, SheetKit.CardEdge);
                    continue;
                }
                var evolved = skill.Id == TapSkillRules.WildVolley && WildVolleyRules.TierOf(company) >= WildVolleyRules.Evolved;
                var text = $"<b>{skill.Name}</b>{(chosen ? "  <size=24><color=#E8C66A>CHOSEN</color></size>" : "")}\n" +
                           $"<size=27><color=#CCCCCC>{skill.Text}</color></size>" +
                           (evolved ? $"\n<size=25><color=#9AD08A>{skill.EvolvedText}</color></size>" : "");
                var id = skill.Id;
                SheetKit.Card(content, text, chosen ? "In use" : "Use", !chosen, () =>
                {
                    if (company.ChooseTapSkill(id))
                    {
                        Sfx.Play(SoundId.Pickup);
                        SaveDirector.SaveNow();
                    }
                    Rerender();
                }, 130f, 220f, chosen);
            }
            SheetKit.Note(content, "More skills will come from other bosses' books.", 26);
        }
    }
}
