using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Gameplay;

namespace djack.RogueSurvivor.UI.Components
{
    /* This UI component is responsible for displaying the list of skills
    of the player */
    class PlayerSkillsPanel : UIComponent
    {
        const int SKILLTABLE_LINES = 10;

        RogueGame game;

        public PlayerSkillsPanel(Rectangle area, RogueGame game)
            : base(area)
        {
            this.game = game;
        }

        public override void Draw(IRogueUI ui)
        {
            Actor player = game.Player;
            if (player != null && player.Sheet.SkillTable != null && player.Sheet.SkillTable.CountSkills > 0)
                DrawActorSkills(ui, player);
        }

        private void DrawActorSkills(IRogueUI ui, Actor actor)
        {
            SetColor(Color.White);
            DrawStringBold(ui, "Skills", 0, -BOLD_LINE_SPACING);
            IEnumerable<Skill> skills = actor.Sheet.SkillTable.Skills;

            int skill_index = 0;
            foreach(Skill skill in skills)
            {
                int line_x = 120 * (skill_index % SKILLTABLE_LINES);
                int line_y = LINE_SPACING * (skill_index / SKILLTABLE_LINES);

                DrawString(ui, String.Format("{0}-", skill.Level), line_x, line_y);
                DrawString(ui, Skills.Name(skill.ID), line_x + 16, line_y);
                skill_index++;
            }
        }
    }
}