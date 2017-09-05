using System;
using System.Drawing;

using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame handles hi scores */
    partial class RogueGame
    {
#region HiScore
        void HandleHiScores(bool saveToTextfile)
        {
            TextFile file = null;
            if (saveToTextfile)
                file = new TextFile();

            m_UI.UI_Clear(Color.Black);
            int gy = 0;
            DrawHeader();
            gy += BOLD_LINE_SPACING;
            m_UI.UI_DrawStringBold(Color.Yellow, "Hi Scores", 0, gy);
            gy += BOLD_LINE_SPACING;
            m_UI.UI_DrawStringBold(Color.White, "---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+", 0, gy);
            gy += BOLD_LINE_SPACING;

            // display.
            m_UI.UI_DrawStringBold(Color.White, "Rank | Name, Skills, Death       |  Score |Difficulty|Survival|  Kills |Achievm.|      Game Time | Playing time", 0, gy);
            gy += BOLD_LINE_SPACING;

            // text.
            if (saveToTextfile)
            {
                file.Append(String.Format("ROGUE SURVIVOR {0}", SetupConfig.GAME_VERSION));
                file.Append("Hi Scores");
                file.Append("Rank | Name, Skills, Death       |  Score |Difficulty|Survival|  Kills |Achievm.|      Game Time | Playing time");
            }

            // individual entries.
            for (int i = 0; i < m_HiScoreTable.Count; i++)
            {
                // display.
                Color rankColor = (i==0 ? Color.LightYellow: i == 1 ? Color.LightCyan : i == 2 ? Color.LightGreen : Color.DimGray);
                m_UI.UI_DrawStringBold(rankColor, "------------------------------------------------------------------------------------------------------------------------", 0, gy);
                gy += BOLD_LINE_SPACING;
                HiScore hi = m_HiScoreTable[i];
                string line = String.Format("{0,3}. | {1,-25} | {2,6} |     {3,3}% | {4,6} | {5,6} | {6,6} | {7,14} | {8}",
                    i + 1, TruncateString(hi.Name, 25),
                    hi.TotalPoints, hi.DifficultyPercent, hi.SurvivalPoints, hi.KillPoints, hi.AchievementPoints,
                    new WorldTime(hi.TurnSurvived).ToString(), TimeSpanToString(hi.PlayingTime));
                m_UI.UI_DrawStringBold(rankColor, line, 0, gy);
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(rankColor, String.Format("     | {0}.", hi.SkillsDescription), 0, gy);
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(rankColor, String.Format("     | {0}.", hi.Death), 0, gy);
                gy += BOLD_LINE_SPACING;

                // text.
                if (saveToTextfile)
                {
                    file.Append("------------------------------------------------------------------------------------------------------------------------");
                    file.Append(line);
                    file.Append(String.Format("     | {0}", hi.SkillsDescription));
                    file.Append(String.Format("     | {0}", hi.Death));
                }
            }

            // save.
            string textfilePath = GetUserHiScoreTextFilePath();
            if (saveToTextfile)              
                file.Save(textfilePath);
            
            // display.
            m_UI.UI_DrawStringBold(Color.White, "---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+---------+", 0, gy);
            gy += BOLD_LINE_SPACING;
            if (saveToTextfile)
            {
                m_UI.UI_DrawStringBold(Color.White, textfilePath, 0, gy);
                gy += BOLD_LINE_SPACING;
            }
            DrawFootnote(Color.White, "press ESC to leave");
            m_UI.UI_Repaint();
            WaitEscape();
        }

        void LoadHiScoreTable()
        {
            m_UI.UI_Clear(Color.Black);
            m_UI.UI_DrawStringBold(Color.White, "Loading hiscores table...", 0, 0);
            m_UI.UI_Repaint();

            m_HiScoreTable = HiScoreTable.Load(GetUserHiScoreFilePath());
            if (m_HiScoreTable == null)
            {
                m_HiScoreTable = new HiScoreTable(HiScoreTable.DEFAULT_MAX_ENTRIES);
                m_HiScoreTable.Clear();
            }

            m_UI.UI_Clear(Color.Black);
            m_UI.UI_DrawStringBold(Color.White, "Loading hiscores table... done!", 0, 0);
            m_UI.UI_Repaint();
        }

        void SaveHiScoreTable()
        {
            m_UI.UI_Clear(Color.Black);
            m_UI.UI_DrawStringBold(Color.White, "Saving hiscores table...", 0, 0);
            m_UI.UI_Repaint();

            HiScoreTable.Save(m_HiScoreTable, GetUserHiScoreFilePath());
            
            m_UI.UI_Clear(Color.Black);
            m_UI.UI_DrawStringBold(Color.White, "Saving hiscores table... done!", 0, 0);
            m_UI.UI_Repaint();
        }
#endregion
    }
}