using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Gameplay;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame handles character creation */
    partial class RogueGame
    {
#region Character creation
        bool HandleNewCharacter()
        {
            DiceRoller roller = new DiceRoller();

            /////////////////
            // Reset session
            /////////////////
            m_Session.Reset();

            ///////////////
            // Game Mode //
            ///////////////
            if (!HandleNewGameMode())
                return false;

            ////////////////////////
            // Choose living/undead 
            ////////////////////////
            bool isUndead;
            if (!HandleNewCharacterRace(roller, out isUndead))
                return false;
            m_CharGen.IsUndead = isUndead;

            /////////////////////////////
            // Choose gender/undead type
            /////////////////////////////
            if (isUndead)
            {
                GameActors.IDs modelID;
                if (!HandleNewCharacterUndeadType(roller, out modelID))
                    return false;
                m_CharGen.UndeadModel = modelID;
            }
            else
            {
                bool isMale;
                if (!HandleNewCharacterGender(roller, out isMale))
                    return false;
                m_CharGen.IsMale = isMale;
            }

            /////////////////////////////
            // Choose skill (living only)
            /////////////////////////////
            if (!isUndead)
            {
                Skills.IDs skID;
                if (!HandleNewCharacterSkill(roller, out skID))
                    return false;
                m_CharGen.StartingSkill = skID;
                // scoring : starting skill.
                m_Session.Scoring.StartingSkill = skID;
            }
            else
            {
                // udead.
            }

            // done
            return true;
        }

        bool HandleNewGameMode()
        {
            string[] menuEntries = new string[]
            {
                Session.DescGameMode(GameMode.GM_STANDARD),
                Session.DescGameMode(GameMode.GM_CORPSES_INFECTION),
                Session.DescGameMode(GameMode.GM_VINTAGE)
            };
            string[] descs = new string[]
            {
                "Rogue Survivor standard game.",
                "Don't get a cold. Keep an eye on your deceased diseased friends.",
                "The classic zombies next door."
            };

            bool loop = true;
            bool choiceDone = false;
            int selected = 0;
            do
            {
                // display.
                m_UI.UI_Clear(Color.Black);
                int gx, gy;
                gx = gy = 0;
                m_UI.UI_DrawStringBold(Color.Yellow, "New Game - Choose Game Mode", gx, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGray, descs, gx, ref gy);
                gy += 2 * BOLD_LINE_SPACING;

                string[] descMode = { };
                switch (selected)
                {
                    case 0:
                        descMode = new string[] {
                            "This is the standard game setting:",
                            "- All the kinds of undeads.",
                            "- Undeads can evolve.", 
                            "- Livings can zombify instantly when dead."
                        };
                        break;
                    case 1:
                        descMode = new string[] {
                            "This is the standard game setting with corpses and infection: ",
                            "- All the kinds of undeads.",
                            "- Undeads can evolve.",
                            "- Some undeads can infect livings when damaging them.",
                            "- Livings become corpses when dead.",
                            "- Corpses will slowy rot... but may rise as undead if infected."
                        };
                        break;
                    case 2:
                        descMode = new string[] {
                            "This is the zombie game for classic hardcore fans: ",
                            "- Undeads are only zombified men and women.",
                            "- Undeads don't evolve.", 
                            "- Some undeads can infect livings when damaging them.",
                            "- Livings become corpses when dead.",
                            "- Corpses will slowy rot... but may rise as undead if infected.",
                            "",
                            "NOTE:",
                            "This mode force some options OFF.",
                            "Remember to set them back ON again when you play other modes!"
                        };
                        break;
                }
                foreach (String str in descMode)
                {
                    m_UI.UI_DrawStringBold(Color.Gray, str, gx, gy);
                    gy += BOLD_LINE_SPACING;
                }


                DrawFootnote(Color.White, "cursor to move, ENTER to select, ESC to cancel");
                m_UI.UI_Repaint();

                // get menu action.
                KeyEventArgs key = m_UI.UI_WaitKey();
                switch (key.KeyCode)
                {
                    case Keys.Up:       // move up
                        if (selected > 0) --selected;
                        else selected = menuEntries.Length - 1;
                        break;
                    case Keys.Down:     // move down
                        selected = (selected + 1) % menuEntries.Length;
                        break;

                    case Keys.Escape:
                        choiceDone = false;
                        loop = false;
                        break;

                    case Keys.Enter:    // validate
                        {
                            switch (selected)
                            {
                                case 0: // standard
                                    m_Session.GameMode = GameMode.GM_STANDARD;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 1: // corpses & infection
                                    m_Session.GameMode = GameMode.GM_CORPSES_INFECTION;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 2: // vintage
                                    m_Session.GameMode = GameMode.GM_VINTAGE;
                                    
                                    // force some options off.
                                    s_Options.AllowUndeadsEvolution = false;
                                    s_Options.ShamblersUpgrade = false;
                                    s_Options.RatsUpgrade = false;
                                    s_Options.SkeletonsUpgrade = false;
                                    ApplyOptions(false);

                                    choiceDone = true;
                                    loop = false;
                                    break;
                            }
                            break;
                        }
                }

            }
            while (loop);

            // done.
            return choiceDone;
        }

        bool HandleNewCharacterRace(DiceRoller roller, out bool isUndead)
        {
            string[] menuEntries = new string[]
            {
                "*Random*",
                "Living",
                "Undead"
            };
            string[] descs = new string[]
            {
                "(picks a race at random for you)",
                "Try to survive.",
                "Eat brains and die again."
            };

            isUndead = false;
            bool loop = true;
            bool choiceDone = false;
            int selected = 0;
            do
            {
                // display.
                m_UI.UI_Clear(Color.Black);
                int gx, gy;
                gx = gy = 0;
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("[{0}] New Character - Choose Race", Session.DescGameMode(m_Session.GameMode)), gx, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGray, descs, gx, ref gy);
                gy += 2 * BOLD_LINE_SPACING;

                DrawFootnote(Color.White, "cursor to move, ENTER to select, ESC to cancel");
                m_UI.UI_Repaint();

                // get menu action.
                KeyEventArgs key = m_UI.UI_WaitKey();
                switch (key.KeyCode)
                {
                    case Keys.Up:       // move up
                        if (selected > 0) --selected;
                        else selected = menuEntries.Length - 1;
                        break;
                    case Keys.Down:     // move down
                        selected = (selected + 1) % menuEntries.Length;
                        break;

                    case Keys.Escape:
                        choiceDone = false;
                        loop = false;
                        break;

                    case Keys.Enter:    // validate
                        {
                            switch (selected)
                            {
                                case 0: // random
                                    isUndead = roller.RollChance(50);

                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.White, String.Format("Race : {0}.", isUndead ? "Undead" : "Living"), gx, gy);
                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.Yellow, "Is that OK? Y to confirm, N to cancel.", gx, gy);
                                    m_UI.UI_Repaint();
                                    if (WaitYesOrNo())
                                    {
                                        choiceDone = true;
                                        loop = false;
                                    }
                                    break;

                                case 1: // living
                                    isUndead = false;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 2: // undead
                                    isUndead = true;
                                    choiceDone = true;
                                    loop = false;
                                    break;
                            }
                            break;
                        }
                }

            }
            while (loop);

            // done.
            return choiceDone;
        }

        bool HandleNewCharacterGender(DiceRoller roller, out bool isMale)
        {
            ActorModel maleModel = GameActors.MaleCivilian;
            ActorModel femaleModel = GameActors.FemaleCivilian;

            string[] menuEntries = new string[]
            {
                "*Random*",
                "Male",
                "Female"
            };
            string[] descs = new string[]
            {
                "(picks a gender at random for you)",
                String.Format("HP:{0:D2}  Def:{1:D2}  Dmg:{2:D1}", maleModel.StartingSheet.BaseHitPoints, maleModel.StartingSheet.BaseDefence.Value,  maleModel.StartingSheet.UnarmedAttack.DamageValue),
                String.Format("HP:{0:D2}  Def:{1:D2}  Dmg:{2:D1}", femaleModel.StartingSheet.BaseHitPoints, femaleModel.StartingSheet.BaseDefence.Value, femaleModel.StartingSheet.UnarmedAttack.DamageValue),
            };

            isMale = true;
            bool loop = true;
            bool choiceDone = false;
            int selected = 0;
            do
            {
                // display.
                m_UI.UI_Clear(Color.Black);
                int gx, gy;
                gx = gy = 0;
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("[{0}] New Living - Choose Gender", Session.DescGameMode(m_Session.GameMode)), gx, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGray, descs, gx, ref gy);
                DrawFootnote(Color.White, "cursor to move, ENTER to select, ESC to cancel");
                m_UI.UI_Repaint();

                // get menu action.
                KeyEventArgs key = m_UI.UI_WaitKey();
                switch (key.KeyCode)
                {
                    case Keys.Up:       // move up
                        if (selected > 0) --selected;
                        else selected = menuEntries.Length - 1;
                        break;
                    case Keys.Down:     // move down
                        selected = (selected + 1) % menuEntries.Length;
                        break;

                    case Keys.Escape:
                        choiceDone = false;
                        loop = false;
                        break;

                    case Keys.Enter:    // validate
                        {
                            switch (selected)
                            {
                                case 0: // random
                                    isMale = roller.RollChance(50);

                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.White, String.Format("Gender : {0}.", isMale ? "Male" : "Female"), gx, gy);
                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.Yellow, "Is that OK? Y to confirm, N to cancel.", gx, gy);
                                    m_UI.UI_Repaint();
                                    if (WaitYesOrNo())
                                    {
                                        choiceDone = true;
                                        loop = false;
                                    }
                                    break;

                                case 1: // male
                                    isMale = true;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 2: // female
                                    isMale = false;
                                    choiceDone = true;
                                    loop = false;
                                    break;
                            }
                            break;
                        }
                }
                
            }
            while (loop);

            // done.
            return choiceDone;
        }

        string DescribeUndeadModelStatLine(ActorModel m)
        {
            return String.Format("HP:{0:D3}  Spd:{1:F2}  Atk:{2:D2}  Def:{3:D2}  Dmg:{4:D2}  FoV:{5:D1}  Sml:{6:F2}",
                m.StartingSheet.BaseHitPoints, m.DollBody.Speed / 100f,
                m.StartingSheet.UnarmedAttack.HitValue, m.StartingSheet.BaseDefence.Value, m.StartingSheet.UnarmedAttack.DamageValue,
                m.StartingSheet.BaseViewRange, m.StartingSheet.BaseSmellRating);
        }

        bool HandleNewCharacterUndeadType(DiceRoller roller, out GameActors.IDs modelID)
        {
            ActorModel skeletonModel = GameActors.Skeleton;
            ActorModel shamblerModel = GameActors.Zombie;
            ActorModel maleModel = GameActors.MaleZombified;
            ActorModel femaleModel = GameActors.FemaleZombified;
            ActorModel masterModel = GameActors.ZombieMaster;

            string[] menuEntries = new string[]
            {
                "*Random*",
                skeletonModel.Name,
                shamblerModel.Name,
                maleModel.Name,
                femaleModel.Name,
                masterModel.Name,
            };
            string[] descs = new string[]
            {
                "(picks a type at random for you)",
                DescribeUndeadModelStatLine(skeletonModel),
                DescribeUndeadModelStatLine(shamblerModel),
                DescribeUndeadModelStatLine(maleModel),
                DescribeUndeadModelStatLine(femaleModel),
                DescribeUndeadModelStatLine(masterModel)
            };

            modelID = GameActors.IDs.UNDEAD_MALE_ZOMBIFIED;
            bool loop = true;
            bool choiceDone = false;
            int selected = 0;
            do
            {
                // display.
                m_UI.UI_Clear(Color.Black);
                int gx, gy;
                gx = gy = 0;
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("[{0}] New Undead - Choose Type", Session.DescGameMode(m_Session.GameMode)), gx, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGray, descs, gx, ref gy);
                DrawFootnote(Color.White, "cursor to move, ENTER to select, ESC to cancel");
                m_UI.UI_Repaint();

                // get menu action.
                KeyEventArgs key = m_UI.UI_WaitKey();
                switch (key.KeyCode)
                {
                    case Keys.Up:       // move up
                        if (selected > 0) --selected;
                        else selected = menuEntries.Length - 1;
                        break;
                    case Keys.Down:     // move down
                        selected = (selected + 1) % menuEntries.Length;
                        break;

                    case Keys.Escape:
                        choiceDone = false;
                        loop = false;
                        break;

                    case Keys.Enter:    // validate
                        {
                            switch (selected)
                            {
                                case 0: // random
                                    selected = roller.Roll(0, 5);
                                    switch (selected)
                                    {
                                        case 0: modelID = GameActors.IDs.UNDEAD_SKELETON; break;
                                        case 1: modelID = GameActors.IDs.UNDEAD_ZOMBIE; break;
                                        case 2: modelID = GameActors.IDs.UNDEAD_MALE_ZOMBIFIED; break;
                                        case 3: modelID = GameActors.IDs.UNDEAD_FEMALE_ZOMBIFIED; break;
                                        case 4: modelID = GameActors.IDs.UNDEAD_ZOMBIE_MASTER; break;
                                        default:
                                            throw new ArgumentOutOfRangeException("unhandled select "+selected);
                                    }

                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.White, String.Format("Type : {0}.", GameActors[modelID].Name), gx, gy);
                                    gy += BOLD_LINE_SPACING;
                                    m_UI.UI_DrawStringBold(Color.Yellow, "Is that OK? Y to confirm, N to cancel.", gx, gy);
                                    m_UI.UI_Repaint();
                                    if (WaitYesOrNo())
                                    {
                                        choiceDone = true;
                                        loop = false;
                                    }
                                    break;

                                case 1: // skeleton
                                    modelID = GameActors.IDs.UNDEAD_SKELETON;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 2: // shambler
                                    modelID = GameActors.IDs.UNDEAD_ZOMBIE;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 3: // male zombified
                                    modelID = GameActors.IDs.UNDEAD_MALE_ZOMBIFIED;
                                    m_CharGen.IsMale = true;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 4: // female zombified
                                    modelID = GameActors.IDs.UNDEAD_FEMALE_ZOMBIFIED;
                                    m_CharGen.IsMale = false;
                                    choiceDone = true;
                                    loop = false;
                                    break;

                                case 5: // zm
                                    modelID = GameActors.IDs.UNDEAD_ZOMBIE_MASTER;
                                    choiceDone = true;
                                    loop = false;
                                    break;
                            }
                            break;
                        }
                }

            }
            while (loop);

            // done.
            return choiceDone;
        }

        bool HandleNewCharacterSkill(DiceRoller roller, out Skills.IDs skID)
        {
            /////////////////////////////
            // Make table of all skills.
            /////////////////////////////
            Skills.IDs[] allSkills = new Skills.IDs[(int)Skills.IDs._LAST_LIVING + 1];
            string[] menuEntries = new string[allSkills.Length + 1];
            string[] skillDesc = new string[allSkills.Length + 1];
            menuEntries[0] = "*Random*";
            skillDesc[0] = "(picks a skill at random for you)";
            for (int i = (int)Skills.IDs._FIRST_LIVING; i < (int)Skills.IDs._LAST_LIVING + 1; i++)
            {
                allSkills[i] = (Skills.IDs)i;
                menuEntries[i + 1] = Skills.Name(allSkills[i]);
                skillDesc[i + 1] = String.Format("{0} max - {1}", Skills.MaxSkillLevel(i), DescribeSkillShort(allSkills[i]));
            }

            //////////////////////////
            // Loop until choice done
            //////////////////////////
            skID = Skills.IDs._FIRST;
            bool loop = true;
            bool choiceDone = false;
            int selected = 0;
            do
            {
                // display.
                m_UI.UI_Clear(Color.Black);
                int gx, gy;
                gx = gy = 0;
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("[{0}] New {1} Character - Choose Starting Skill", 
                    Session.DescGameMode(m_Session.GameMode),
                    m_CharGen.IsMale ? "Male" : "Female"), gx, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGray, skillDesc, gx, ref gy);
                DrawFootnote(Color.White, "cursor to move, ENTER to select, ESC to cancel");
                m_UI.UI_Repaint();

                // get menu action.
                KeyEventArgs key = m_UI.UI_WaitKey();
                switch (key.KeyCode)
                {
                    case Keys.Up:       // move up
                        if (selected > 0) --selected;
                        else selected = menuEntries.Length - 1;
                        break;
                    case Keys.Down:     // move down
                        selected = (selected + 1) % menuEntries.Length;
                        break;

                    case Keys.Escape:
                        choiceDone = false;
                        loop = false;
                        break;

                    case Keys.Enter:    // validate
                        if (selected == 0) // random
                            skID = Skills.RollLiving(roller);
                        else
                            skID = (Skills.IDs)(selected - 1 + (int)Skills.IDs._FIRST);

                        gy += BOLD_LINE_SPACING;
                        m_UI.UI_DrawStringBold(Color.White, String.Format("Skill : {0}.", Skills.Name(skID)), gx, gy);
                        gy += BOLD_LINE_SPACING;
                        m_UI.UI_DrawStringBold(Color.Yellow, "Is that OK? Y to confirm, N to cancel.", gx, gy);
                        m_UI.UI_Repaint();
                        if (WaitYesOrNo())
                        {
                            choiceDone = true;
                            loop = false;
                        }
                        break;
                }
            }
            while (loop);

            // done.
            return choiceDone;
        }
#endregion
    }
}