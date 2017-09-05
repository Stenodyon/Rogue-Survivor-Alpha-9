using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.UI.Components
{
    /* Layout of the game UI, contains all the components
    and renders them */
    class GameLayout : UIComponent
    {
        public const int CANVAS_WIDTH = 1024;
        public const int CANVAS_HEIGHT = 768;

        public const int TILE_VIEW_WIDTH = 21;
        public const int TILE_VIEW_HEIGHT = 21;

        const int RIGHTPANEL_X = RogueGame.TILE_SIZE * TILE_VIEW_WIDTH + 4;
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
        const int MESSAGES_Y = TILE_VIEW_HEIGHT * RogueGame.TILE_SIZE + 4;
        const int MESSAGES_SPACING = 12;
        const int MESSAGES_FADEOUT = 25;
        const int MAX_MESSAGES = 7;
        const int MESSAGES_HISTORY = 59;

        List<UIComponent> children;

        public GameLayout(RogueGame game)
            : base(new Rectangle(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT))
        {
            children = new List<UIComponent>();

            children.Add(new LocationPanel(
                Rectangle.FromLTRB(
                    LOCATIONPANEL_X, LOCATIONPANEL_Y, CANVAS_WIDTH, CANVAS_HEIGHT
                ), game
            ));
            children.Add(new PlayerStatusPanel(
                Rectangle.FromLTRB(
                    RIGHTPANEL_TEXT_X, RIGHTPANEL_TEXT_Y, CANVAS_WIDTH, INVENTORYPANEL_Y
                ), game
            ));
        }

        public override void Draw(IRogueUI ui)
        {
            foreach(UIComponent component in children)
                component.Draw(ui);
        }
    }
}