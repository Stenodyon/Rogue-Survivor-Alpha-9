using System;
using System.Drawing;
using System.Windows.Forms;

using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame handles options */
    partial class RogueGame
    {
        void HandleOptions(bool ingame)
        {
            GameOptions prevOptions = s_Options;

            #region
            GameOptions.IDs[] list = new GameOptions.IDs[]
            {
                    // display & sounds
                   GameOptions.IDs.UI_MUSIC,
                   GameOptions.IDs.UI_MUSIC_VOLUME,
                   GameOptions.IDs.UI_ANIM_DELAY,
                   GameOptions.IDs.UI_SHOW_MINIMAP,
                   GameOptions.IDs.UI_SHOW_PLAYER_TAG_ON_MINIMAP,
                   // helpers
                   GameOptions.IDs.UI_ADVISOR,
                   GameOptions.IDs.UI_COMBAT_ASSISTANT,
                   GameOptions.IDs.UI_SHOW_PLAYER_TARGETS,
                   GameOptions.IDs.UI_SHOW_TARGETS,
                   // sim
                   GameOptions.IDs.GAME_SIM_THREAD,
                   GameOptions.IDs.GAME_SIMULATE_DISTRICTS,
                   GameOptions.IDs.GAME_SIMULATE_SLEEP,
                   // death
                   GameOptions.IDs.GAME_DEATH_SCREENSHOT,
                   GameOptions.IDs.GAME_PERMADEATH,
                   // maps
                   GameOptions.IDs.GAME_CITY_SIZE,
                   GameOptions.IDs.GAME_DISTRICT_SIZE,
                   GameOptions.IDs.GAME_REVEAL_STARTING_DISTRICT,
                   // living
                   GameOptions.IDs.GAME_MAX_CIVILIANS,
                   GameOptions.IDs.GAME_ZOMBIFICATION_CHANCE,
                   GameOptions.IDs.GAME_AGGRESSIVE_HUNGRY_CIVILIANS,
                   GameOptions.IDs.GAME_NPC_CAN_STARVE_TO_DEATH,
                   GameOptions.IDs.GAME_STARVED_ZOMBIFICATION_CHANCE,
                   // undeads
                   GameOptions.IDs.GAME_MAX_UNDEADS,
                   GameOptions.IDs.GAME_ALLOW_UNDEADS_EVOLUTION,
                   GameOptions.IDs.GAME_DAY_ZERO_UNDEADS_PERCENT,
                   GameOptions.IDs.GAME_ZOMBIE_INVASION_DAILY_INCREASE,
                   GameOptions.IDs.GAME_UNDEADS_UPGRADE_DAYS,
                   GameOptions.IDs.GAME_SHAMBLERS_UPGRADE,
                   GameOptions.IDs.GAME_SKELETONS_UPGRADE,
                   GameOptions.IDs.GAME_RATS_UPGRADE,
                   // events
                   GameOptions.IDs.GAME_NATGUARD_FACTOR,
                   GameOptions.IDs.GAME_SUPPLIESDROP_FACTOR,
                   // reinc
                   GameOptions.IDs.GAME_MAX_REINCARNATIONS,
                   GameOptions.IDs.GAME_REINC_LIVING_RESTRICTED,
                   GameOptions.IDs.GAME_REINCARNATE_AS_RAT,
                   GameOptions.IDs.GAME_REINCARNATE_TO_SEWERS
            };
            #endregion

            string[] menuEntries = new string[list.Length];
            string[] values = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
                menuEntries[i] = GameOptions.Name(list[i]);

            bool loop = true;
            int selected = 0;
            do
            {
                for (int i = 0; i < list.Length; i++)
                    values[i] = s_Options.DescribeValue(m_Session.GameMode, list[i]);

                int gx, gy;
                gx = gy = 0;
                m_UI.UI_Clear(Color.Black);
                DrawHeader();
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("[{0}] - Options", Session.DescGameMode(m_Session.GameMode)), 0, gy);
                gy += 2 * BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGreen, values, gx, ref gy, 400);

                // caution.
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(Color.Red, "* Caution : increasing these values makes the game runs slower and saving/loading longer.", gx, gy);
                gy += BOLD_LINE_SPACING;

                // difficulty rating.               
                gy += BOLD_LINE_SPACING;
                int diffForSurvivor = (int)(100 * Scoring.ComputeDifficultyRating(s_Options, DifficultySide.FOR_SURVIVOR, 0));
                int diffforUndead = (int)(100 * Scoring.ComputeDifficultyRating(s_Options, DifficultySide.FOR_UNDEAD, 0));
                m_UI.UI_DrawStringBold(Color.Yellow, String.Format("Difficulty Rating : {0}% as survivor / {1}% as undead.", diffForSurvivor, diffforUndead), gx, gy);
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(Color.White, "Difficulty used for scoring automatically decrease with each reincarnation.", gx, gy);
                gy += 2 * BOLD_LINE_SPACING;

                // footnote.
                DrawFootnote(Color.White, "cursor to move and change values, R to restore previous values, ESC to save and leave");
                m_UI.UI_Repaint();

                // handle
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

                    case Keys.R:        // restore previous.
                        s_Options = prevOptions;
                        break;

                    case Keys.Escape:   // validate and leave
                        loop = false;
                        break;

                    case Keys.Left:
                        switch ((GameOptions.IDs)list[selected])
                        {
                            case GameOptions.IDs.GAME_DISTRICT_SIZE: s_Options.DistrictSize -= 5; break;
                            case GameOptions.IDs.UI_MUSIC: s_Options.PlayMusic = !s_Options.PlayMusic; break;
                            case GameOptions.IDs.UI_MUSIC_VOLUME: s_Options.MusicVolume -= 5; break;
                            case GameOptions.IDs.UI_ANIM_DELAY: s_Options.IsAnimDelayOn = !s_Options.IsAnimDelayOn; break;
                            case GameOptions.IDs.UI_SHOW_MINIMAP: s_Options.IsMinimapOn = !s_Options.IsMinimapOn; break;
                            case GameOptions.IDs.UI_SHOW_PLAYER_TAG_ON_MINIMAP: s_Options.ShowPlayerTagsOnMinimap = !s_Options.ShowPlayerTagsOnMinimap; break;
                            case GameOptions.IDs.UI_ADVISOR: s_Options.IsAdvisorEnabled = !s_Options.IsAdvisorEnabled; break;
                            case GameOptions.IDs.UI_COMBAT_ASSISTANT: s_Options.IsCombatAssistantOn = !s_Options.IsCombatAssistantOn; break;
                            case GameOptions.IDs.UI_SHOW_TARGETS: s_Options.ShowTargets = !s_Options.ShowTargets; break;
                            case GameOptions.IDs.UI_SHOW_PLAYER_TARGETS: s_Options.ShowPlayerTargets = !s_Options.ShowPlayerTargets; break;
                            case GameOptions.IDs.GAME_MAX_CIVILIANS: s_Options.MaxCivilians -= 5; break;
                            case GameOptions.IDs.GAME_MAX_DOGS: --s_Options.MaxDogs; break;
                            case GameOptions.IDs.GAME_MAX_UNDEADS: s_Options.MaxUndeads -= 10; break;
                            case GameOptions.IDs.GAME_DAY_ZERO_UNDEADS_PERCENT: s_Options.DayZeroUndeadsPercent -= 5; break;
                            case GameOptions.IDs.GAME_ZOMBIE_INVASION_DAILY_INCREASE: --s_Options.ZombieInvasionDailyIncrease; break;
                            case GameOptions.IDs.GAME_CITY_SIZE: s_Options.CitySize -= 1; break;
                            case GameOptions.IDs.GAME_NPC_CAN_STARVE_TO_DEATH: s_Options.NPCCanStarveToDeath = !s_Options.NPCCanStarveToDeath; break;
                            case GameOptions.IDs.GAME_STARVED_ZOMBIFICATION_CHANCE: s_Options.StarvedZombificationChance -= 5; break;
                            case GameOptions.IDs.GAME_SIMULATE_DISTRICTS:
                                if (s_Options.SimulateDistricts != GameOptions.SimRatio.OFF)
                                    s_Options.SimulateDistricts = (GameOptions.SimRatio)(s_Options.SimulateDistricts - 1);
                                break;
                            case GameOptions.IDs.GAME_SIMULATE_SLEEP: s_Options.SimulateWhenSleeping = !s_Options.SimulateWhenSleeping; break;
                            case GameOptions.IDs.GAME_SIM_THREAD: s_Options.SimThread = !s_Options.SimThread; break;
                            case GameOptions.IDs.GAME_ZOMBIFICATION_CHANCE: s_Options.ZombificationChance -= 5; break;
                            case GameOptions.IDs.GAME_REVEAL_STARTING_DISTRICT: s_Options.RevealStartingDistrict = !s_Options.RevealStartingDistrict; break;
                            case GameOptions.IDs.GAME_ALLOW_UNDEADS_EVOLUTION:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.AllowUndeadsEvolution = false;
                                else
                                    s_Options.AllowUndeadsEvolution = !s_Options.AllowUndeadsEvolution; 
                               break;
                            case GameOptions.IDs.GAME_UNDEADS_UPGRADE_DAYS:
                                if (s_Options.ZombifiedsUpgradeDays != GameOptions.ZupDays._FIRST)
                                    s_Options.ZombifiedsUpgradeDays = (GameOptions.ZupDays)(s_Options.ZombifiedsUpgradeDays - 1);
                                break;
                            case GameOptions.IDs.GAME_MAX_REINCARNATIONS: --s_Options.MaxReincarnations; break;
                            case GameOptions.IDs.GAME_REINCARNATE_AS_RAT: s_Options.CanReincarnateAsRat = !s_Options.CanReincarnateAsRat; break;
                            case GameOptions.IDs.GAME_REINCARNATE_TO_SEWERS: s_Options.CanReincarnateToSewers = !s_Options.CanReincarnateToSewers; break;
                            case GameOptions.IDs.GAME_REINC_LIVING_RESTRICTED: s_Options.IsLivingReincRestricted = !s_Options.IsLivingReincRestricted; break;
                            case GameOptions.IDs.GAME_PERMADEATH: s_Options.IsPermadeathOn = !s_Options.IsPermadeathOn; break;
                            case GameOptions.IDs.GAME_DEATH_SCREENSHOT: s_Options.IsDeathScreenshotOn = !s_Options.IsDeathScreenshotOn; break;
                            case GameOptions.IDs.GAME_AGGRESSIVE_HUNGRY_CIVILIANS: s_Options.IsAggressiveHungryCiviliansOn = !s_Options.IsAggressiveHungryCiviliansOn; break;
                            case GameOptions.IDs.GAME_NATGUARD_FACTOR: s_Options.NatGuardFactor -= 10; break;
                            case GameOptions.IDs.GAME_SUPPLIESDROP_FACTOR: s_Options.SuppliesDropFactor -= 10; break;
                            case GameOptions.IDs.GAME_RATS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.RatsUpgrade = false;
                                else
                                    s_Options.RatsUpgrade = !s_Options.RatsUpgrade; 
                                break;
                            case GameOptions.IDs.GAME_SHAMBLERS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.ShamblersUpgrade = false;
                                else
                                    s_Options.ShamblersUpgrade = !s_Options.ShamblersUpgrade; 
                                break;
                            case GameOptions.IDs.GAME_SKELETONS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.SkeletonsUpgrade = false;
                                else
                                    s_Options.SkeletonsUpgrade = !s_Options.SkeletonsUpgrade; 
                                break;                            
                        }
                        break;
                    case Keys.Right:
                        switch ((GameOptions.IDs)list[selected])
                        {
                            case GameOptions.IDs.GAME_DISTRICT_SIZE: s_Options.DistrictSize += 5; break;
                            case GameOptions.IDs.UI_MUSIC: s_Options.PlayMusic = !s_Options.PlayMusic; break;
                            case GameOptions.IDs.UI_MUSIC_VOLUME: s_Options.MusicVolume += 5; break;
                            case GameOptions.IDs.UI_ANIM_DELAY: s_Options.IsAnimDelayOn = !s_Options.IsAnimDelayOn; break;
                            case GameOptions.IDs.UI_SHOW_MINIMAP: s_Options.IsMinimapOn = !s_Options.IsMinimapOn; break;
                            case GameOptions.IDs.UI_SHOW_PLAYER_TAG_ON_MINIMAP: s_Options.ShowPlayerTagsOnMinimap = !s_Options.ShowPlayerTagsOnMinimap; break;
                            case GameOptions.IDs.UI_ADVISOR: s_Options.IsAdvisorEnabled = !s_Options.IsAdvisorEnabled; break;
                            case GameOptions.IDs.UI_COMBAT_ASSISTANT: s_Options.IsCombatAssistantOn = !s_Options.IsCombatAssistantOn; break;
                            case GameOptions.IDs.UI_SHOW_TARGETS: s_Options.ShowTargets = !s_Options.ShowTargets; break;
                            case GameOptions.IDs.UI_SHOW_PLAYER_TARGETS: s_Options.ShowPlayerTargets = !s_Options.ShowPlayerTargets; break;
                            case GameOptions.IDs.GAME_MAX_CIVILIANS: s_Options.MaxCivilians += 5; break;
                            case GameOptions.IDs.GAME_MAX_DOGS: ++s_Options.MaxDogs; break;
                            case GameOptions.IDs.GAME_MAX_UNDEADS: s_Options.MaxUndeads += 10; break;
                            case GameOptions.IDs.GAME_DAY_ZERO_UNDEADS_PERCENT: s_Options.DayZeroUndeadsPercent += 5; break;
                            case GameOptions.IDs.GAME_ZOMBIE_INVASION_DAILY_INCREASE: ++s_Options.ZombieInvasionDailyIncrease; break;
                            case GameOptions.IDs.GAME_CITY_SIZE: s_Options.CitySize += 1; break;
                            case GameOptions.IDs.GAME_NPC_CAN_STARVE_TO_DEATH: s_Options.NPCCanStarveToDeath = !s_Options.NPCCanStarveToDeath; break;
                            case GameOptions.IDs.GAME_STARVED_ZOMBIFICATION_CHANCE: s_Options.StarvedZombificationChance += 5; break;
                            case GameOptions.IDs.GAME_SIMULATE_DISTRICTS:
                                if (s_Options.SimulateDistricts != GameOptions.SimRatio.FULL)
                                {
                                    s_Options.SimulateDistricts = (GameOptions.SimRatio)(s_Options.SimulateDistricts + 1);
                                }
                                break;
                            case GameOptions.IDs.GAME_SIMULATE_SLEEP: s_Options.SimulateWhenSleeping = !s_Options.SimulateWhenSleeping; break;
                            case GameOptions.IDs.GAME_SIM_THREAD: s_Options.SimThread = !s_Options.SimThread; break;
                            case GameOptions.IDs.GAME_ZOMBIFICATION_CHANCE: s_Options.ZombificationChance += 5; break;
                            case GameOptions.IDs.GAME_REVEAL_STARTING_DISTRICT: s_Options.RevealStartingDistrict = !s_Options.RevealStartingDistrict; break;
                            case GameOptions.IDs.GAME_ALLOW_UNDEADS_EVOLUTION:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.AllowUndeadsEvolution = false;
                                else
                                    s_Options.AllowUndeadsEvolution = !s_Options.AllowUndeadsEvolution; 
                                break;
                            case GameOptions.IDs.GAME_UNDEADS_UPGRADE_DAYS: 
                                if (s_Options.ZombifiedsUpgradeDays != GameOptions.ZupDays._COUNT-1)
                                    s_Options.ZombifiedsUpgradeDays = (GameOptions.ZupDays)(s_Options.ZombifiedsUpgradeDays + 1);
                                break;
                            case GameOptions.IDs.GAME_MAX_REINCARNATIONS: ++s_Options.MaxReincarnations; break;
                            case GameOptions.IDs.GAME_REINCARNATE_AS_RAT: s_Options.CanReincarnateAsRat = !s_Options.CanReincarnateAsRat; break;
                            case GameOptions.IDs.GAME_REINCARNATE_TO_SEWERS: s_Options.CanReincarnateToSewers = !s_Options.CanReincarnateToSewers; break;
                            case GameOptions.IDs.GAME_REINC_LIVING_RESTRICTED: s_Options.IsLivingReincRestricted = !s_Options.IsLivingReincRestricted; break;
                            case GameOptions.IDs.GAME_PERMADEATH: s_Options.IsPermadeathOn = !s_Options.IsPermadeathOn; break;
                            case GameOptions.IDs.GAME_DEATH_SCREENSHOT: s_Options.IsDeathScreenshotOn = !s_Options.IsDeathScreenshotOn; break;
                            case GameOptions.IDs.GAME_AGGRESSIVE_HUNGRY_CIVILIANS: s_Options.IsAggressiveHungryCiviliansOn = !s_Options.IsAggressiveHungryCiviliansOn; break;
                            case GameOptions.IDs.GAME_NATGUARD_FACTOR: s_Options.NatGuardFactor += 10; break;
                            case GameOptions.IDs.GAME_SUPPLIESDROP_FACTOR: s_Options.SuppliesDropFactor += 10; break;
                            case GameOptions.IDs.GAME_RATS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.RatsUpgrade = false;
                                else
                                    s_Options.RatsUpgrade = !s_Options.RatsUpgrade; 
                                break;
                            case GameOptions.IDs.GAME_SHAMBLERS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.ShamblersUpgrade = false;
                                else
                                    s_Options.ShamblersUpgrade = !s_Options.ShamblersUpgrade; 
                                break;
                            case GameOptions.IDs.GAME_SKELETONS_UPGRADE:
                                if (m_Session.GameMode == GameMode.GM_VINTAGE)
                                    s_Options.SkeletonsUpgrade = false;
                                    s_Options.SkeletonsUpgrade = !s_Options.SkeletonsUpgrade; 
                                break;    
                        }
                        break;
                }

                // force some options combinations.
                if (s_Options.SimThread)
                    s_Options.SimulateWhenSleeping = false;
                // apply options.
                ApplyOptions(false);
            }
            while (loop);

            // save.
            SaveOptions();
        }

        void HandleRedefineKeys()
        {
            bool loop = true;
            int selected = 0;
            bool conflict = false;
            do
            {
                // check for conflict.
                conflict = s_KeyBindings.CheckForConflict();

                // draw
                string[] menuEntries = new string[]
                {
                    "Move N",
                    "Move NE",
                    "Move E",
                    "Move SE",
                    "Move S",
                    "Move SW",
                    "Move W",
                    "Move NW",
                    "Wait",
                    "Wait 1 hour",
                    "Abandon Game",
                    "Advisor Hint",
                    "Barricade",
                    "Break",
                    "Build Large Fortification",
                    "Build Small Fortification",
                    "City Info",
                    "Close",
                    "Fire",
                    "Give",
                    "Help",
                    "Hints screen",
                    "Initiate Trade",
                    "Item 1 slot",
                    "Item 2 slot",
                    "Item 3 slot",
                    "Item 4 slot",
                    "Item 5 slot",
                    "Item 6 slot",
                    "Item 7 slot",
                    "Item 8 slot",
                    "Item 9 slot",
                    "Item 10 slot",
                    "Lead",
                    "Load Game",
                    "Mark Enemies",
                    "Messages Log",
                    "Options",
                    "Order",
                    "Push",
                    "Quit Game",
                    "Redefine Keys",
                    "Run",
                    "Save Game",
                    "Screenshot",
                    "Shout",
                    "Sleep",
                    "Switch Place",
                    "Use Exit",
                    "Use Spray",
                };
                const int O_MOVE_N = 0;
                const int O_MOVE_NE = 1;
                const int O_MOVE_E = 2;
                const int O_MOVE_SE = 3;
                const int O_MOVE_S = 4;
                const int O_MOVE_SW = 5;
                const int O_MOVE_W = 6;
                const int O_MOVE_NW = 7;
                const int O_WAIT = 8;
                const int O_WAIT_LONG = 9;
                const int O_ABANDON = 10;
                const int O_ADVISOR = 11;
                const int O_BARRICADE = 12;
                const int O_BREAK = 13;
                const int O_BUILD_LARGE_F = 14;
                const int O_BUILD_SMALL_F = 15;
                const int O_CITYINFO = 16;
                const int O_CLOSE = 17;
                const int O_FIRE = 18;
                const int O_GIVE = 19;
                const int O_HELP = 20;
                const int O_HINTS_SCREEN = 21;
                const int O_INIT_TRADE = 22;
                const int O_ITEM_1 = 23;
                const int O_ITEM_2 = 24;
                const int O_ITEM_3 = 25;
                const int O_ITEM_4 = 26;
                const int O_ITEM_5 = 27;
                const int O_ITEM_6 = 28;
                const int O_ITEM_7 = 29;
                const int O_ITEM_8 = 30;
                const int O_ITEM_9 = 31;
                const int O_ITEM_10 = 32;
                const int O_LEAD = 33;
                const int O_LOAD = 34;
                const int O_MARKENEMY = 35;
                const int O_LOG = 36;
                const int O_OPTIONS = 37;
                const int O_ORDER = 38;
                const int O_PUSH = 39;
                const int O_QUIT = 40;
                const int O_REDEFKEYS = 41;
                const int O_RUN = 42;
                const int O_SAVE = 43;
                const int O_SCREENSHOT = 44;
                const int O_SHOUT = 45;
                const int O_SLEEP = 46;
                const int O_SWITCH = 47;
                const int O_USE_EXIT = 48;
                const int O_USE_SPRAY= 49;
                string[] values = new string[]
                {
                    s_KeyBindings.Get(PlayerCommand.MOVE_N).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_NE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_E).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_SE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_S).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_SW).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_W).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MOVE_NW).ToString(),
                    s_KeyBindings.Get(PlayerCommand.WAIT_OR_SELF).ToString(),
                    s_KeyBindings.Get(PlayerCommand.WAIT_LONG).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ABANDON_GAME).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ADVISOR).ToString(),
                    s_KeyBindings.Get(PlayerCommand.BARRICADE_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.BREAK_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.BUILD_LARGE_FORTIFICATION).ToString(),
                    s_KeyBindings.Get(PlayerCommand.BUILD_SMALL_FORTIFICATION).ToString(),
                    s_KeyBindings.Get(PlayerCommand.CITY_INFO).ToString(),
                    s_KeyBindings.Get(PlayerCommand.CLOSE_DOOR).ToString(),
                    s_KeyBindings.Get(PlayerCommand.FIRE_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.GIVE_ITEM).ToString(),
                    s_KeyBindings.Get(PlayerCommand.HELP_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.HINTS_SCREEN_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.INITIATE_TRADE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_0).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_1).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_2).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_3).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_4).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_5).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_6).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_7).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_8).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ITEM_SLOT_9).ToString(),
                    s_KeyBindings.Get(PlayerCommand.LEAD_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.LOAD_GAME).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MARK_ENEMIES_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.MESSAGE_LOG).ToString(),
                    s_KeyBindings.Get(PlayerCommand.OPTIONS_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.ORDER_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.PUSH_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.QUIT_GAME).ToString(),
                    s_KeyBindings.Get(PlayerCommand.KEYBINDING_MODE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.RUN_TOGGLE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.SAVE_GAME).ToString(),
                    s_KeyBindings.Get(PlayerCommand.SCREENSHOT).ToString(),
                    s_KeyBindings.Get(PlayerCommand.SHOUT).ToString(),
                    s_KeyBindings.Get(PlayerCommand.SLEEP).ToString(),
                    s_KeyBindings.Get(PlayerCommand.SWITCH_PLACE).ToString(),
                    s_KeyBindings.Get(PlayerCommand.USE_EXIT).ToString(),
                    s_KeyBindings.Get(PlayerCommand.USE_SPRAY).ToString(),
                };

                int gx, gy;
                gx = gy = 0;
                m_UI.UI_Clear(Color.Black);
                DrawHeader();
                gy += BOLD_LINE_SPACING;
                m_UI.UI_DrawStringBold(Color.Yellow, "Redefine keys", 0, gy);
                gy += BOLD_LINE_SPACING;
                DrawMenuOrOptions(selected, Color.White, menuEntries, Color.LightGreen, values, gx, ref gy);
                if (conflict)
                {
                    m_UI.UI_DrawStringBold(Color.Red, "Conflicting keys. Please redefine the keys so the commands don't overlap.", gx, gy);
                    gy += BOLD_LINE_SPACING;
                }
                DrawFootnote(Color.White, "cursor to move, ENTER to rebind a key, ESC to save and leave");
                m_UI.UI_Repaint();

                // handle
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

                    case Keys.Escape:   // leave.
                        if (!conflict)
                        {
                            loop = false;
                        }
                        break;

                    case Keys.Enter: // rebind
                        // say.
                        m_UI.UI_DrawStringBold(Color.Yellow, String.Format("rebinding {0}, press the new key.", menuEntries[selected]), gx, gy);
                        m_UI.UI_Repaint();

                        // read new key.
                        bool loopNewKey = true;
                        Keys newKeyData = Keys.None;
                        do
                        {
                            KeyEventArgs newKey = m_UI.UI_WaitKey();
                            // ignore Shift and Control alone.
                            if (newKey.KeyCode == Keys.ShiftKey || newKey.KeyCode == Keys.ControlKey)
                                continue;
                            // always ignore Alt.
                            if (newKey.Alt)
                                continue;
                            // done!
                            newKeyData = newKey.KeyData;
                            loopNewKey = false;
                        }
                        while (loopNewKey);

                        // get command.
                        PlayerCommand command;
                        switch (selected)
                        {
                            case O_MOVE_N: command = PlayerCommand.MOVE_N; break;
                            case O_MOVE_NE: command = PlayerCommand.MOVE_NE; break;
                            case O_MOVE_E: command = PlayerCommand.MOVE_E; break;
                            case O_MOVE_SE: command = PlayerCommand.MOVE_SE; break;
                            case O_MOVE_S: command = PlayerCommand.MOVE_S; break;
                            case O_MOVE_SW: command = PlayerCommand.MOVE_SW; break;
                            case O_MOVE_W: command = PlayerCommand.MOVE_W; break;
                            case O_MOVE_NW: command = PlayerCommand.MOVE_NW; break;
                            case O_WAIT: command = PlayerCommand.WAIT_OR_SELF; break;
                            case O_WAIT_LONG: command = PlayerCommand.WAIT_LONG; break;
                            case O_ABANDON: command = PlayerCommand.ABANDON_GAME; break;
                            case O_ADVISOR: command = PlayerCommand.ADVISOR; break;
                            case O_BARRICADE: command = PlayerCommand.BARRICADE_MODE; break;
                            case O_BREAK: command = PlayerCommand.BREAK_MODE; break;
                            case O_BUILD_LARGE_F: command = PlayerCommand.BUILD_LARGE_FORTIFICATION; break;
                            case O_BUILD_SMALL_F: command = PlayerCommand.BUILD_SMALL_FORTIFICATION; break;
                            case O_CITYINFO: command = PlayerCommand.CITY_INFO; break;
                            case O_CLOSE: command = PlayerCommand.CLOSE_DOOR; break;
                            case O_FIRE: command = PlayerCommand.FIRE_MODE; break;
                            case O_GIVE: command = PlayerCommand.GIVE_ITEM; break;
                            case O_HELP: command = PlayerCommand.HELP_MODE; break;
                            case O_HINTS_SCREEN: command = PlayerCommand.HINTS_SCREEN_MODE; break;
                            case O_INIT_TRADE: command = PlayerCommand.INITIATE_TRADE; break;
                            case O_ITEM_1: command = PlayerCommand.ITEM_SLOT_0; break;
                            case O_ITEM_2: command = PlayerCommand.ITEM_SLOT_1; break;
                            case O_ITEM_3: command = PlayerCommand.ITEM_SLOT_2; break;
                            case O_ITEM_4: command = PlayerCommand.ITEM_SLOT_3; break;
                            case O_ITEM_5: command = PlayerCommand.ITEM_SLOT_4; break;
                            case O_ITEM_6: command = PlayerCommand.ITEM_SLOT_5; break;
                            case O_ITEM_7: command = PlayerCommand.ITEM_SLOT_6; break;
                            case O_ITEM_8: command = PlayerCommand.ITEM_SLOT_7; break;
                            case O_ITEM_9: command = PlayerCommand.ITEM_SLOT_8; break;
                            case O_ITEM_10: command = PlayerCommand.ITEM_SLOT_9; break;
                            case O_LEAD: command = PlayerCommand.LEAD_MODE; break;
                            case O_LOAD: command = PlayerCommand.LOAD_GAME; break;
                            case O_MARKENEMY: command = PlayerCommand.MARK_ENEMIES_MODE; break;
                            case O_LOG: command = PlayerCommand.MESSAGE_LOG; break;
                            case O_OPTIONS: command = PlayerCommand.OPTIONS_MODE; break;
                            case O_ORDER: command = PlayerCommand.ORDER_MODE; break;
                            case O_PUSH: command = PlayerCommand.PUSH_MODE; break;
                            case O_QUIT: command = PlayerCommand.QUIT_GAME; break;
                            case O_REDEFKEYS: command = PlayerCommand.KEYBINDING_MODE; break;
                            case O_RUN: command = PlayerCommand.RUN_TOGGLE; break;
                            case O_SAVE: command = PlayerCommand.SAVE_GAME; break;
                            case O_SCREENSHOT: command = PlayerCommand.SCREENSHOT; break;
                            case O_SHOUT: command = PlayerCommand.SHOUT; break;
                            case O_SLEEP: command = PlayerCommand.SLEEP; break;
                            case O_SWITCH: command = PlayerCommand.SWITCH_PLACE; break;
                            case O_USE_EXIT: command = PlayerCommand.USE_EXIT; break;
                            case O_USE_SPRAY: command = PlayerCommand.USE_SPRAY; break;
                            default:
                                throw new InvalidOperationException("unhandled selected");
                        }

                        // bind it.                      
                        s_KeyBindings.Set(command, newKeyData);

                        break;

                }
            }
            while (loop);

            // Save.
            SaveKeybindings();
        }
    }
}