using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;

namespace djack.RogueSurvivor.UI.Components
{
    /* This UI component is responsible for displaying messages */
    class MessagePanel : UIComponent
    {
        MessageManager manager;
        RogueGame game;

        int fadeOut;
        int spacing;

        public MessagePanel(Rectangle area, RogueGame game, int fadeOut, int spacing)
            : base(area)
        {
            this.game = game;
            this.manager = game.MessageManager;

            this.fadeOut = fadeOut;
            this.spacing = spacing;
        }

        public override void Draw(IRogueUI ui)
        {
            int freshMessagesTurn = game.Session.LastTurnPlayerActed;
            int curY = 0;
            Message[] messages = new List<Message>(manager.Messages).ToArray();
            for(int index = 0; index < messages.Length; index++)
            {
                Message msg = messages[index];

                int alpha = Math.Max(64, 255 - fadeOut * (messages.Length - 1 - index));
                bool isLatest = (msg.Turn >= freshMessagesTurn);
                Color dimmedColor = Color.FromArgb(alpha, msg.Color);

                if(isLatest)
                    DrawStringBold(ui, dimmedColor, msg.Text, 0, curY);
                else
                    DrawString(ui, dimmedColor, msg.Text, 0, curY);

                curY += spacing;
            }
        }
    }
}