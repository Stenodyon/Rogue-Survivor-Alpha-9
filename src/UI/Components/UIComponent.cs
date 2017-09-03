using System.Drawing;

using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.UI.Components
{
    public abstract class UIComponent
    {
        public Rect area { get; protected set; }

        public int x { get { return area.x; } set { area.x = value; } }
        public int y { get { return area.y; } set { area.y = value; } }
        public int h { get { return area.h; } set { area.h = value; } }
        public int z { get { return area.w; } set { area.w = value; } }

        /* Draw the component using this ui */
        public abstract void Draw(IRogueUI ui);

        public void DrawLine(IRogueUI ui, Color color,
                             int x1, int y1, int x2, int y2)
        {
            ui.UI_DrawLine(color,
                           x1 - x, y1 - y,
                           x2 - x, y2 - y);
        }

        public void DrawString(IRogueUI ui, Color color, string message,
                               int x0, int y0)
        {
            ui.UI_DrawString(color, message, x0 - x, y0 - y);
        }
    }
}