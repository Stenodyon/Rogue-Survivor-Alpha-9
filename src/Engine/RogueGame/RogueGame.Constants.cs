using System.Drawing;

using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame provides game constants */
    partial class RogueGame
    {
#region Constants
        public const int MAP_MAX_HEIGHT = 100;
        public const int MAP_MAX_WIDTH = 100;

        public const int TILE_SIZE = 32;
        public const int ACTOR_SIZE = 32;
        public const int ACTOR_OFFSET = (TILE_SIZE - ACTOR_SIZE) / 2;
        public const int TILE_VIEW_WIDTH = 21;
        public const int TILE_VIEW_HEIGHT = 21;
        const int HALF_VIEW_WIDTH = 10;
        const int HALF_VIEW_HEIGHT = 10;

        public const int CANVAS_WIDTH = 1024;
        public const int CANVAS_HEIGHT = 768;

        const int DAMAGE_DX = 10;
        const int DAMAGE_DY = 10;

#region UI elements
        const int RIGHTPANEL_X = TILE_SIZE * TILE_VIEW_WIDTH + 4;
        const int RIGHTPANEL_Y = 0;
        const int RIGHTPANEL_TEXT_X = RIGHTPANEL_X + 4;
        const int RIGHTPANEL_TEXT_Y = RIGHTPANEL_Y + 4;

        const int INVENTORYPANEL_X = RIGHTPANEL_TEXT_X;
        const int INVENTORYPANEL_Y = RIGHTPANEL_TEXT_Y + 156;//142;
        const int GROUNDINVENTORYPANEL_Y = INVENTORYPANEL_Y + 64;
        const int CORPSESPANEL_Y = GROUNDINVENTORYPANEL_Y + 64;
        const int INVENTORY_SLOTS_PER_LINE = 10;

        const int SKILLTABLE_Y = CORPSESPANEL_Y + 64;
        const int SKILLTABLE_LINES = 10;

        const int LOCATIONPANEL_X = RIGHTPANEL_X;
        const int LOCATIONPANEL_Y = MESSAGES_Y;
        const int LOCATIONPANEL_TEXT_X = LOCATIONPANEL_X + 4;
        const int LOCATIONPANEL_TEXT_Y = LOCATIONPANEL_Y + 4;

        const int MESSAGES_X = 4;
        const int MESSAGES_Y = TILE_VIEW_HEIGHT * TILE_SIZE + 4;
        const int MESSAGES_SPACING = 12;
        const int MESSAGES_FADEOUT = 25;
        const int MAX_MESSAGES = 7;
        const int MESSAGES_HISTORY = 59;

        public const int MINITILE_SIZE = 2;
        const int MINIMAP_X = RIGHTPANEL_X + (CANVAS_WIDTH - RIGHTPANEL_X - MAP_MAX_WIDTH * MINITILE_SIZE) / 2;
        const int MINIMAP_Y = MESSAGES_Y - MINITILE_SIZE * MAP_MAX_HEIGHT - 1;
        const int MINI_TRACKER_OFFSET = 1;

        const int DELAY_SHORT = 250;
        const int DELAY_NORMAL = 500;
        const int DELAY_LONG = 1000;

        readonly Color POPUP_FILLCOLOR = Color.FromArgb(192, Color.CornflowerBlue);

        readonly string[] CLOSE_DOOR_MODE_TEXT = new string[] { "CLOSE MODE - directions to close, ESC cancels" };
        readonly string[] BARRICADE_MODE_TEXT = new string[] { "BARRICADE/REPAIR MODE - directions to barricade/repair, ESC cancels" };
        readonly string[] BREAK_MODE_TEXT = new string[] { "BREAK MODE - directions/wait to break an object, ESC cancels" };
        readonly string[] BUILD_LARGE_FORT_MODE_TEXT = new string[] { "BUILD LARGE FORTIFICATION MODE - directions to build, ESC cancels" };
        readonly string[] BUILD_SMALL_FORT_MODE_TEXT = new string[] { "BUILD SMALL FORTIFICATION MODE - directions to build, ESC cancels" };
        readonly string[] TRADE_MODE_TEXT = new string[] { "TRADE MODE - Y to accept the deal, N to refuse" };
        readonly string[] INITIATE_TRADE_MODE_TEXT = new string[] { "INITIATE TRADE MODE - directions to offer item to someone, ESC cancels" };
        readonly string[] UPGRADE_MODE_TEXT = new string[] { "UPGRADE MODE - follow instructions in the message panel" };
        readonly string[] FIRE_MODE_TEXT = new string[] { "FIRE MODE - F to fire, T next target, M toggle mode, ESC cancels" };
        readonly string[] SWITCH_PLACE_MODE_TEXT = new string[] { "SWITCH PLACE MODE - directions to switch place with a follower, ESC cancels" };
        readonly string[] TAKE_LEAD_MODE_TEXT = new string[] { "TAKE LEAD MODE - directions to recruit a follower, ESC cancels" };
        readonly string[] PUSH_MODE_TEXT = new string[] { "PUSH/SHOVE MODE - directions to push/shove, ESC cancels" };
        readonly string[] TAG_MODE_TEXT = new string[] { "TAG MODE - directions to tag a wall or on the floor, ESC cancels" };
        readonly string PUSH_OBJECT_MODE_TEXT = "PUSHING {0} - directions to push, ESC cancels";
        readonly string SHOVE_ACTOR_MODE_TEXT = "SHOVING {0} - directions to shove, ESC cancels";
        readonly string[] ORDER_MODE_TEXT = new string[] { "ORDER MODE - follow instructions in the message panel, ESC cancels" };
        readonly string[] GIVE_MODE_TEXT = new string[] { "GIVE MODE - directions to give item to someone, ESC cancels" };
        readonly string[] THROW_GRENADE_MODE_TEXT = new string[] { "THROW GRENADE MODE - directions to select, F to fire,  ESC cancels" };
        readonly string[] MARK_ENEMIES_MODE = new string[] { "MARK ENEMIES MODE - E to make enemy, T next actor, ESC cancels" };
        readonly Color MODE_TEXTCOLOR = Color.Yellow;
        readonly Color MODE_BORDERCOLOR = Color.Yellow;
        readonly Color MODE_FILLCOLOR = Color.FromArgb(192, Color.Gray);

        readonly Color PLAYER_ACTION_COLOR = Color.White;
        readonly Color OTHER_ACTION_COLOR = Color.Gray;
        readonly Color SAYOREMOTE_COLOR = Color.Brown;
        readonly Color PLAYER_AUDIO_COLOR = Color.Green;

        const int LINE_SPACING = 12;
        const int BOLD_LINE_SPACING = 14;
        const int CREDIT_CHAR_SPACING = 8;
        const int CREDIT_LINE_SPACING = LINE_SPACING;

        readonly Color NIGHT_COLOR = Color.Cyan;
        readonly Color DAY_COLOR = Color.Gold;

        const int TEXTFILE_CHARS_PER_LINE = 120;
        const int TEXTFILE_LINES_PER_PAGE = 50;
#endregion

#region Notable Zone names
        public const string NAME_SUBWAY_STATION = "Subway Station";
        public const string NAME_SEWERS_MAINTENANCE = "Sewers Maintenance";
        public const string NAME_SUBWAY_RAILS = "rails";
        public const string NAME_POLICE_STATION_JAILS_CELL = "jail";
#endregion

#region Events
        const int SPAWN_DISTANCE_TO_PLAYER = 10;

#region Zombie invasion

#endregion

#region Sewers invasion
        const int SEWERS_INVASION_CHANCE = 1;
        public const float SEWERS_UNDEADS_FACTOR = 0.50f;  // 1.0 for as much as surface undead spawning.
#endregion

#region DISABLED Subway invasion 
    #if false 
        const int SUBWAY_INVASION_CHANCE = 1;
        public const float SUBWAY_UNDEADS_FACTOR = 0.25f;  // 1.0 for as much as surface undead spawning.
    #endif
#endregion

#region Refugees
        /// <summary>
        /// How many refugees in each wave, as ratio of max civilians.
        /// </summary>
        const float REFUGEES_WAVE_SIZE = 0.20f;

        /// <summary>
        /// How many random items each new refugee will carry.
        /// </summary>
        const int REFUGEES_WAVE_ITEMS = 3;


        /// <summary>
        /// Chance to spawn on the surface vs sewers/subway.
        /// </summary>
        const int REFUGEE_SURFACE_SPAWN_CHANCE = 80;
#endregion

#region Unique NPC refugees
        const int UNIQUE_REFUGEE_CHECK_CHANCE = 10;
#endregion

#region National Guard Squad
        /// <summary>
        /// Date at which natguard can intervene.
        /// </summary>
        public const int NATGUARD_DAY = 3;

        /// <summary>
        /// Date at which natguard will stop coming.
        /// </summary>
        const int NATGUARD_END_DAY = 10;

        /// <summary>
        /// Date at which the natguard leader will bring Z-Trackers.
        /// </summary>
        const int NATGUARD_ZTRACKER_DAY = NATGUARD_DAY + 3;

        /// <summary>
        /// How many soldiers in each national guard squad.
        /// </summary>
        const int NATGUARD_SQUAD_SIZE = 5;

        /// <summary>
        /// By how many times the undeads must outnumber the livings for the nat guard to intervene.
        /// Factored by option.
        /// </summary>
        const float NATGUARD_INTERVENTION_FACTOR = 5;

        /// <summary>
        /// How many chance per turn the nat guard intervene (if other conditions are met).
        /// </summary>
        const int NATGUARD_INTERVENTION_CHANCE = 1;
#endregion

#region Army drop supplies
        /// <summary>
        /// Date at which army can drop supplies.
        /// </summary>
        const int ARMY_SUPPLIES_DAY = 4;

        /// <summary>
        /// Ratio total map food items nutrition / livings below which the army drop supplies event can fire.
        /// Factored by option.
        /// </summary>
        const float ARMY_SUPPLIES_FACTOR = 0.20f * Rules.FOOD_BASE_POINTS;

        /// <summary>
        /// Chances per turn the army will drop supply (if other conditions are met).
        /// </summary>
        const int ARMY_SUPPLIES_CHANCE = 2;

        /// <summary>
        /// Radius in which supplies items are dropped.
        /// One item is dropped per suitable tile in radius.
        /// </summary>
        const int ARMY_SUPPLIES_SCATTER = 1;

#endregion

#region Bikers raid
        /// <summary>
        /// Date at which bikers will start to raid.
        /// </summary>
        public const int BIKERS_RAID_DAY = 2;

        /// <summary>
        /// Date at which bikers will stop coming.
        /// </summary>
        const int BIKERS_END_DAY = 14;

        /// <summary>
        /// Number of bikers in the raid.
        /// </summary>
        const int BIKERS_RAID_SIZE = 6;

        /// <summary>
        /// Raid chance per turn (if others conditions are met).
        /// </summary>
        const int BIKERS_RAID_CHANCE_PER_TURN = 1;

        /// <summary>
        /// Number of days between each bikers raid.
        /// </summary>
        const int BIKERS_RAID_DAYS_GAP = 2;
#endregion

#region Gangstas raid
        /// <summary>
        /// Date at which gangsta will start to raid.
        /// </summary>
        public const int GANGSTAS_RAID_DAY = 7;

        /// <summary>
        /// Date at which gangstas will stop coming.
        /// </summary>
        const int GANGSTAS_END_DAY = 21;

        /// <summary>
        /// Number of gangstas in the raid.
        /// </summary>
        const int GANGSTAS_RAID_SIZE = 6;

        /// <summary>
        /// Raid chance per turn (if others conditions are met).
        /// </summary>
        const int GANGSTAS_RAID_CHANCE_PER_TURN = 1;

        /// <summary>
        /// Number of days between each gangsta raid.
        /// </summary>
        const int GANGSTAS_RAID_DAYS_GAP = 3;
#endregion

#region BlackOps raid
        /// <summary>
        /// Date at which blackops will start to raid.
        /// </summary>
        const int BLACKOPS_RAID_DAY = 14;

        /// <summary>
        /// Number of blackops in the raid.
        /// </summary>
        const int BLACKOPS_RAID_SIZE = 3;

        /// <summary>
        /// Raid chances per turn (if others conditions are met).
        /// </summary>
        const int BLACKOPS_RAID_CHANCE_PER_TURN = 1;

        /// <summary>
        /// Delay between each raid.
        /// </summary>
        const int BLACKOPS_RAID_DAY_GAP = 5;
#endregion

#region Band of Survivors 
        const int SURVIVORS_BAND_DAY = 21;
        const int SURVIVORS_BAND_SIZE = 5;
        const int SURVIVORS_BAND_CHANCE_PER_TURN = 1;
        const int SURVIVORS_BAND_DAY_GAP = 5;
#endregion

#endregion

#region Undeads evolution
        const int ZOMBIE_LORD_EVOLUTION_MIN_DAY = 7;
        const int DISCIPLE_EVOLUTION_MIN_DAY = 7;
#endregion

#region Map color tints for day phases
        readonly Color TINT_DAY = Color.White;
        readonly Color TINT_SUNSET = Color.FromArgb(235, 235, 235);
        readonly Color TINT_EVENING = Color.FromArgb(215, 215, 215);
        readonly Color TINT_MIDNIGHT = Color.FromArgb(195, 195, 195);
        readonly Color TINT_NIGHT = Color.FromArgb(205, 205, 205);
        readonly Color TINT_SUNRISE = Color.FromArgb(225, 225, 225);
#endregion

#region Hearing chances - avoid spamming messages.
        const int PLAYER_HEAR_FIGHT_CHANCE = 25;
        const int PLAYER_HEAR_SCREAMS_CHANCE = 10;
        const int PLAYER_HEAR_PUSH_CHANCE = 25;
        const int PLAYER_HEAR_BASH_CHANCE = 25;
        const int PLAYER_HEAR_BREAK_CHANCE = 50;
        const int PLAYER_HEAR_EXPLOSION_CHANCE = 100;
#endregion

#region Blood splatting
        const int BLOOD_WALL_SPLAT_CHANCE = 20;
#endregion

#region NPC player sleeping snoring message chance
        public const int MESSAGE_NPC_SLEEP_SNORE_CHANCE = 10;
#endregion

#region Weather
        const int WEATHER_CHANGE_CHANCE = 33;
#endregion

#region World Gen
        const int DISTRICT_EXIT_CHANCE_PER_TILE = 15;
#endregion

#region Common verbs
        readonly Verb VERB_ACCEPT_THE_DEAL = new Verb("accept the deal", "accepts the deal");
        readonly Verb VERB_ACTIVATE = new Verb("activate");
        readonly Verb VERB_AVOID = new Verb("avoid");
        readonly Verb VERB_BARRICADE = new Verb("barricade");
        readonly Verb VERB_BASH = new Verb("bash", "bashes");
        readonly Verb VERB_BE = new Verb("are", "is");
        readonly Verb VERB_BUILD = new Verb("build");
        readonly Verb VERB_BREAK = new Verb("break");
        readonly Verb VERB_BUTCHER = new Verb("butcher");
        readonly Verb VERB_CATCH = new Verb("catch", "catches");
        readonly Verb VERB_CHAT_WITH = new Verb("chat with", "chats with");
        readonly Verb VERB_CLOSE = new Verb("close");
        readonly Verb VERB_COLLAPSE = new Verb("collapse");
        readonly Verb VERB_CRUSH = new Verb("crush", "crushes");
        readonly Verb VERB_DESACTIVATE = new Verb("desactivate");
        readonly Verb VERB_DESTROY = new Verb("destroy");
        readonly Verb VERB_DIE = new Verb("die");
        readonly Verb VERB_DIE_FROM_STARVATION = new Verb("die from starvation", "dies from starvation");
        readonly Verb VERB_DISCARD = new Verb("discard");
        readonly Verb VERB_DRAG = new Verb("drag");
        readonly Verb VERB_DROP = new Verb("drop");
        readonly Verb VERB_EAT = new Verb("eat");
        readonly Verb VERB_ENJOY = new Verb("enjoy");
        readonly Verb VERB_ENTER = new Verb("enter");
        readonly Verb VERB_ESCAPE = new Verb("escape");
        readonly Verb VERB_FAIL = new Verb("fail");
        readonly Verb VERB_FEAST_ON = new Verb("feast on", "feasts on");
        readonly Verb VERB_FEEL = new Verb("feel");
        readonly Verb VERB_GIVE = new Verb("give");
        readonly Verb VERB_GRAB = new Verb("grab");
        readonly Verb VERB_EQUIP = new Verb("equip");
        readonly Verb VERB_HAVE = new Verb("have", "has");
        readonly Verb VERB_HELP = new Verb("help");
        readonly Verb VERB_PERSUADE = new Verb("persuade");
        readonly Verb VERB_HEAL_WITH = new Verb("heal with", "heals with");
        readonly Verb VERB_JUMP_ON = new Verb("jump on", "jumps on");
        readonly Verb VERB_KILL = new Verb("kill");
        readonly Verb VERB_LEAVE = new Verb("leave");
        readonly Verb VERB_MISS = new Verb("miss", "misses");
        readonly Verb VERB_MURDER = new Verb("murder");
        readonly Verb VERB_OFFER = new Verb("offer");
        readonly Verb VERB_OPEN = new Verb("open");
        readonly Verb VERB_ORDER = new Verb("order");
        readonly Verb VERB_PUSH = new Verb("push", "pushes");
        readonly Verb VERB_RAISE_ALARM = new Verb("raise the alarm", "raises the alarm");
        readonly Verb VERB_REFUSE_THE_DEAL = new Verb("refuse the deal", "refuses the deal");
        readonly Verb VERB_RELOAD = new Verb("reload");
        readonly Verb VERB_RECHARGE = new Verb("recharge");
        readonly Verb VERB_REPAIR = new Verb("repair");
        readonly Verb VERB_REVIVE = new Verb("revive");
        readonly Verb VERB_SEE = new Verb("see");
        readonly Verb VERB_SHOUT = new Verb("shout");
        readonly Verb VERB_SHOVE = new Verb("shove");
        readonly Verb VERB_SNORE = new Verb("snore");
        readonly Verb VERB_SPRAY = new Verb("spray");
        readonly Verb VERB_START = new Verb("start");
        readonly Verb VERB_STOP = new Verb("stop");
        readonly Verb VERB_STUMBLE = new Verb("stumble");
        readonly Verb VERB_SWITCH = new Verb("switch", "switches");
        readonly Verb VERB_SWITCH_PLACE_WITH = new Verb("switch place with", "switches place with");
        readonly Verb VERB_TAKE = new Verb("take");
        readonly Verb VERB_THROW = new Verb("throw");
        readonly Verb VERB_TRANSFORM_INTO = new Verb("transform into", "transforms into");
        readonly Verb VERB_UNEQUIP = new Verb("unequip");
        readonly Verb VERB_VOMIT = new Verb("vomit");
        readonly Verb VERB_WAIT = new Verb("wait");
        readonly Verb VERB_WAKE_UP = new Verb("wake up", "wakes up");
#endregion

#endregion
    }
}