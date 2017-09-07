using System;
using System.Drawing;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;

namespace djack.RogueSurvivor.UI.Components
{
    /* This UI Component is responsible for displaying the stats of the player
    (health, stamina, etc) */
    class PlayerStatusPanel : UIComponent
    {
        RogueGame game;

        enum ValueType
        {
            HP,
            STAMINA,
            FOOD,
            SLEEP,
            SANITY,
            INFECTION
        }

        Color BaseColor(ValueType type)
        {
            switch(type)
            {
                case ValueType.HP: return Color.Red;
                case ValueType.STAMINA: return Color.Green;
                case ValueType.FOOD: return Color.Chocolate;
                case ValueType.SLEEP: return Color.Blue;
                case ValueType.SANITY: return Color.Orange;
                case ValueType.INFECTION: return Color.Purple;
                default:
                    throw new ArgumentException("Unsupported status type");
            }
        }

        Color LossColor(ValueType type)
        {
            switch(type)
            {
                case ValueType.HP: return Color.DarkRed;
                case ValueType.STAMINA: return Color.DarkGreen;
                case ValueType.FOOD: return Color.Brown;
                case ValueType.SLEEP: return Color.DarkBlue;
                case ValueType.SANITY: return Color.DarkOrange;
                case ValueType.INFECTION: return Color.Black;
                default:
                    throw new ArgumentException("Unsupported status type");
            }
        }

        Color GainColor(ValueType type)
        {
            switch(type)
            {
                case ValueType.HP: return Color.OrangeRed;
                case ValueType.STAMINA: return Color.LightGreen;
                case ValueType.FOOD: return Color.Beige;
                case ValueType.SLEEP: return Color.LightBlue;
                case ValueType.SANITY: return Color.OrangeRed;
                case ValueType.INFECTION: return Color.Black;
                default:
                    throw new ArgumentException("Unsupported status type");
            }
        }

        public PlayerStatusPanel(Rectangle area, RogueGame game)
            : base(area)
        {
            this.game = game;
        }

        public override void Draw(IRogueUI ui)
        {
            if(game.Player != null)
                DrawStatusPanel(ui);
        }

        private void DrawStatusPanel(IRogueUI ui)
        {
            int curY = 0;
            int col0 = 0;
            int col1 = BOLD_LINE_SPACING * 6 + 100;
            int col2 = BOLD_LINE_SPACING * 9 + 100;
            // 1. Name & occupation
            SetColor(Color.White);
            DrawStringBold(ui, String.Format("{0}, {1}", game.Player.Name, game.Player.Faction.MemberName), col0, curY);

            // 2. Bars: Health, Stamina, Food, Sleep, Infection.
            curY += BOLD_LINE_SPACING;
            DrawHealthStatus(ui, col0, col1, curY);

            curY += BOLD_LINE_SPACING;
            if (game.Player.Model.Abilities.CanTire)
                DrawStaminaStatus(ui, col0, col1, col2, curY);

            curY += BOLD_LINE_SPACING;
            if (game.Player.Model.Abilities.HasToEat)
                DrawFoodStatus(ui, col0, col1, col2, curY, false);
            else if (game.Player.Model.Abilities.IsRotting)
                DrawFoodStatus(ui, col0, col1, col2, curY, true);

            curY += BOLD_LINE_SPACING;
            if (game.Player.Model.Abilities.HasToSleep)
                DrawSleepStatus(ui, col0, col1, col2, curY);

            curY += BOLD_LINE_SPACING;
            if (game.Player.Model.Abilities.HasSanity)
                DrawSanityStatus(ui, col0, col1, col2, curY);

            if (Rules.HasInfection(game.Session.GameMode) && !game.Player.Model.Abilities.IsUndead)
            {
                curY += BOLD_LINE_SPACING;
                DrawInfectionStatus(ui, col0, col1, col2, curY);
            }

            // 3. Melee & Ranged Attacks.
            curY += BOLD_LINE_SPACING;
            DrawAttackValues(ui, col0, curY);
            
            // 4. (living)Def, Pro, Spd, FoV & Nb of followers / (undead)Def, Spd, Fov, Sml, Kills
            curY += 2 * BOLD_LINE_SPACING;
            DrawExtraStats(ui, col0, curY);
        }

#region Status Bars
        void DrawHealthStatus(IRogueUI ui, int col0, int col1, int y)
        {
            int maxHP = game.Rules.ActorMaxHPs(game.Player);
            DrawStringBold(ui, String.Format("HP  {0}", game.Player.HitPoints), col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.HitPoints, game.Player.PreviousHitPoints, 0, maxHP,
                ValueType.HP, box
            );
            DrawStringBold(ui, String.Format("{0}", maxHP), col1, y);
        }

        void DrawStaminaStatus(IRogueUI ui, int col0, int col1, int col2, int y)
        {
            int maxSTA = game.Rules.ActorMaxSTA(game.Player);
            DrawStringBold(
                ui, Color.White,
                String.Format(
                    "STA {0}", game.Player.StaminaPoints),
                col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.StaminaPoints, game.Player.PreviousStaminaPoints,
                Rules.STAMINA_MIN_FOR_ACTIVITY, maxSTA,
                ValueType.STAMINA, box
            );
            DrawStringBold(
                ui, Color.White,
                String.Format("{0}", maxSTA),
                col1, y);
            if (game.Player.IsRunning)
                DrawStringBold(ui, Color.LightGreen, "RUNNING!", col2, y);
            else if (game.Rules.CanActorRun(game.Player))
                DrawStringBold(ui, Color.Green, "can run", col2, y);
            else if (game.Rules.IsActorTired(game.Player))
                DrawStringBold(ui, Color.Gray, "TIRED", col2, y);
        }

        void DrawFoodStatus(IRogueUI ui, int col0, int col1, int col2, int y,
                            bool zombie)
        {
            int maxFood = zombie ? game.Rules.ActorMaxRot(game.Player)
                                 : game.Rules.ActorMaxFood(game.Player);
            string format = zombie ? "ROT {0}" : "FOO {0}";
            int refValue = zombie ? Rules.ROT_HUNGRY_LEVEL
                                  : Rules.FOOD_HUNGRY_LEVEL;
            DrawStringBold(
                ui, Color.White,
                String.Format(format, game.Player.FoodPoints),
                col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.FoodPoints, game.Player.PreviousFoodPoints,
                refValue, maxFood,
                ValueType.FOOD, box);
            //DrawBar(ui, game.Player.FoodPoints, game.Player.PreviousFoodPoints, maxFood, Rules.FOOD_HUNGRY_LEVEL, 100, BOLD_LINE_SPACING, gx + BOLD_LINE_SPACING * 5, gy, Color.Chocolate, Color.Brown, Color.Beige, Color.Gray);
            DrawStringBold(
                ui, Color.White,
                String.Format("{0}", maxFood),
                col1, y);
            if(zombie ? game.Rules.IsRottingActorHungry(game.Player)
                      : game.Rules.IsActorHungry(game.Player))
            {
                if (zombie ? game.Rules.IsRottingActorStarving(game.Player)
                           : game.Rules.IsActorStarving(game.Player))
                    DrawStringBold(
                        ui, Color.Red, "STARVING!",
                        col2, y);
                else
                    DrawStringBold(
                        ui, Color.Yellow, "Hungry",
                        col2, y);
            }
            else
            {
                int timeTillHungry = zombie ? RogueGame.FoodToHoursUntilRotHungry(game.Player.FoodPoints)
                                            : RogueGame.FoodToHoursUntilHungry(game.Player.FoodPoints);
                DrawStringBold(
                    ui, Color.White,
                    String.Format("{0}h", timeTillHungry),
                    col2, y);
            }
        }

        void DrawSleepStatus(IRogueUI ui, int col0, int col1, int col2, int y)
        {
            int maxSleep = game.Rules.ActorMaxSleep(game.Player);
            DrawStringBold(
                ui, Color.White,
                String.Format("SLP {0}", game.Player.SleepPoints),
                col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.SleepPoints, game.Player.PreviousSleepPoints,
                Rules.SLEEP_SLEEPY_LEVEL, maxSleep,
                ValueType.SLEEP, box);
            //DrawBar(ui, game.Player.SleepPoints, game.Player.PreviousSleepPoints, maxSleep, Rules.SLEEP_SLEEPY_LEVEL, 100, BOLD_LINE_SPACING, gx + BOLD_LINE_SPACING * 5, gy, Color.Blue, Color.DarkBlue, Color.LightBlue, Color.Gray);
            DrawStringBold(
                ui, Color.White,
                String.Format("{0}", maxSleep),
                col1, y);
            if (game.Rules.IsActorSleepy(game.Player))
            {
                if (game.Rules.IsActorExhausted(game.Player))
                    DrawStringBold(
                        ui, Color.Red, "EXHAUSTED!",
                        col2, y);
                else
                    DrawStringBold(
                        ui, Color.Yellow, "Sleepy",
                        col2, y);
            }
            else
            {
                int timeTillSleepy =
                    game.Rules.SleepToHoursUntilSleepy(
                        game.Player.SleepPoints,
                        game.Session.WorldTime.IsNight);
                DrawStringBold(
                    ui, Color.White,
                    String.Format("{0}h", timeTillSleepy),
                    col2, y);
            }
        }

        void DrawSanityStatus(IRogueUI ui, int col0, int col1, int col2, int y)
        {
            int maxSan = game.Rules.ActorMaxSanity(game.Player);
            int refSan = game.Rules.ActorDisturbedLevel(game.Player);
            DrawStringBold(
                ui, Color.White,
                String.Format("SAN {0}", game.Player.Sanity),
                col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.Sanity, game.Player.PreviousSanity, refSan, maxSan,
                ValueType.SANITY, box);
            DrawStringBold(
                ui, Color.White,
                String.Format("{0}", maxSan),
                col1, y);
            if (game.Rules.IsActorDisturbed(game.Player))
            {
                if (game.Rules.IsActorInsane(game.Player))
                    DrawStringBold(
                        ui, Color.Red, "INSANE!",
                        col2, y);
                else
                    DrawStringBold(
                        ui, Color.Yellow, "Disturbed",
                        col2, y);
            }
            else
            {
                int timeTillUnstable =
                    game.Rules.SanityToHoursUntilUnstable(game.Player);
                DrawStringBold(
                    ui, Color.White,
                    String.Format("{0}h", timeTillUnstable),
                    col2, y);
            }
        }

        void DrawInfectionStatus(IRogueUI ui, int col0, int col1, int col2, int y)
        {
            int maxInf = game.Rules.ActorInfectionHPs(game.Player);
            int refInf = (Rules.INFECTION_LEVEL_1_WEAK * maxInf) / 100;
            DrawStringBold(
                ui, Color.White,
                String.Format("INF {0}", game.Player.Infection),
                col0, y);
            Rectangle box = MakeStatusBarBox(col0, y);
            DrawStatBar(
                ui,
                game.Player.Infection, game.Player.Infection, refInf, maxInf,
                ValueType.INFECTION, box);
            int infectionPercent = game.Rules.ActorInfectionPercent(game.Player);
            DrawStringBold(
                ui, Color.White,
                String.Format("{0}%", infectionPercent),
                col1, y);
        }

        Rectangle MakeStatusBarBox(int x, int y)
        {
            return new Rectangle(
                x + BOLD_LINE_SPACING * 5, y, 100, BOLD_LINE_SPACING
            );
        }

        /* Draws a status bar for the given status
        \param value Value of the status
        \param prevValue Previous value of the status
        \param refValue Reference value for the status
        \param type Type of status */
        void DrawStatBar(IRogueUI ui,
                         int value, int prevValue, int refValue, int maxValue,
                         ValueType type, Rectangle area)
        {
            Color color1 = BaseColor(type);
            Color color2 = prevValue > value ? LossColor(type) : GainColor(type);

            int barLength = (int)(area.Width * ((float)value / (float)maxValue));
            int prevBarLength = (int)(area.Width * ((float)prevValue / (float)maxValue));

            DrawSplitBar(ui, area, barLength, prevBarLength,
                         color1, color2, Color.Gray);

            // reference line.
            int refLength = (int)(area.Width * (float)refValue / (float)maxValue);
            DrawLine(ui, Color.White,
                     area.X + refLength, area.Y,
                     area.X + refLength, area.Y + area.Height);
        }

        /* Draws a bar split into 3 segments */
        void DrawSplitBar(IRogueUI ui, Rectangle area, int split1, int split2,
                          Color color1, Color color2, Color? color3 = null)
        {
            if(color3 != null)
                FillRect(ui, color3.Value, area);
            if(split1 > split2)
                Swap(ref split1, ref split2);
            Clamp(ref split1, 0, area.Width);
            Clamp(ref split2, 0, area.Width);
            if(split2 > 0)
                FillRect(ui, color2, new Rectangle(
                    area.X, area.Y, split2, area.Height
                ));
            if(split1 > 0)
                FillRect(ui, color1, new Rectangle(
                    area.X, area.Y, split1, area.Height
                ));
        }
#endregion

        void DrawAttackValues(IRogueUI ui, int col0, int y)
        {
            Attack melee = game.Rules.ActorMeleeAttack(
                game.Player, game.Player.CurrentMeleeAttack, null);
            int dmgBonusVsUndead = game.Rules.ActorDamageBonusVsUndeads(game.Player);
            DrawStringBold(
                ui, Color.White,
                String.Format(
                    "Melee  Atk {0:D2}  Dmg {1:D2}/{2:D2}",
                    melee.HitValue, melee.DamageValue,
                    melee.DamageValue + dmgBonusVsUndead),
                col0, y);
            
            y += BOLD_LINE_SPACING;

            Attack ranged = game.Rules.ActorRangedAttack(
                game.Player, game.Player.CurrentRangedAttack,
                game.Player.CurrentRangedAttack.EfficientRange, null);
            ItemRangedWeapon rangedWeapon = game.Player.GetEquippedWeapon() as ItemRangedWeapon;
            int ammo, maxAmmo;
            ammo = maxAmmo=0;
            if (rangedWeapon != null)
            {
                ammo = rangedWeapon.Ammo;
                maxAmmo = (rangedWeapon.Model as ItemRangedWeaponModel).MaxAmmo;
                DrawStringBold(
                    ui, Color.White,
                    String.Format(
                        "Ranged Atk {0:D2}  Dmg {1:D2}/{2:D2} Rng {3}-{4} Amo {5}/{6}", 
                        ranged.HitValue, ranged.DamageValue,
                        ranged.DamageValue+dmgBonusVsUndead,
                        ranged.Range, ranged.EfficientRange, ammo, maxAmmo),
                    col0, y);
            }
        }

        void DrawExtraStats(IRogueUI ui, int col0, int y)
        {
            Defence defence = game.Rules.ActorDefence(game.Player, game.Player.CurrentDefence);

            if (game.Player.Model.Abilities.IsUndead)
            {
                DrawStringBold(
                    ui, Color.White,
                    String.Format(
                        "Def {0:D2} Spd {1:F2} FoV {2} Sml {3:F2} Kills {4}",
                        defence.Value,
                        (float)game.Rules.ActorSpeed(game.Player) / (float)Rules.BASE_SPEED, 
                        game.Rules.ActorFOV(game.Player, game.Session.WorldTime, game.Session.World.Weather),
                        game.Rules.ActorSmell(game.Player),
                        game.Player.KillsCount),
                    col0, y);
            }
            else
            {
                DrawStringBold(
                    ui, Color.White,
                    String.Format(
                        "Def {0:D2} Arm {1:D1}/{2:D1} Spd {3:F2} FoV {4} Fol {5}/{6}",
                        defence.Value, defence.Protection_Hit, defence.Protection_Shot, 
                        (float)game.Rules.ActorSpeed(game.Player) / (float)Rules.BASE_SPEED, game.Rules.ActorFOV(game.Player, game.Session.WorldTime, game.Session.World.Weather),
                        game.Player.CountFollowers, game.Rules.ActorMaxFollowers(game.Player)),
                    col0, y);
            }
        }

        /// UTILITY FUNCTIONS

        void Swap(ref int lhs, ref int rhs)
        {
            int temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        void Clamp(ref int value, int min, int max)
        {
            if(value < min)
                value = min;
            if(value > max)
                value = max;
        }
    }
}