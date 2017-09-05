using System.Drawing;

using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.UI.Components
{
    abstract class UIComponent
    {
        protected const int LINE_SPACING = 12;
        protected const int BOLD_LINE_SPACING = 14;

        protected Rectangle area;

        public int x {
            get { return area.X; }
            set { area.X = value; }
        }
        public int y {
            get { return area.Y; }
            set { area.Y = value; }
        }
        public int h {
            get { return area.Height; }
            set { area.Height = value; }
        }
        public int w {
            get { return area.Width; }
            set { area.Width = value; }
        }

        private Color currentColor = Color.White;

        public UIComponent(Rectangle area)
        {
            this.area = area;
        }

        protected void SetColor(Color newcolor)
        {
            currentColor = newcolor;
        }

        /* Draw the component using this ui */
        public abstract void Draw(IRogueUI ui);

        public void DrawLine(IRogueUI ui,
                             int x1, int y1, int x2, int y2)
        {
            DrawLine(ui, currentColor, x1, y1, x2, y2);
        }

        public void DrawLine(IRogueUI ui, Color color,
                             int x1, int y1, int x2, int y2)
        {
            ui.UI_DrawLine(color,
                           x1 + x, y1 + y,
                           x2 + x, y2 + y
            );
        }

        public void DrawString(IRogueUI ui, string message,
                               int x0, int y0)
        {
            DrawString(ui, currentColor, message, x0, y0);
        }

        public void DrawString(IRogueUI ui, Color color, string message,
                               int x0, int y0)
        {
            ui.UI_DrawString(color, message, x0 + x, y0 + y);
        }
    }
}