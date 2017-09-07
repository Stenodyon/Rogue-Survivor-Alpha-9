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

        public void DrawStringBold(IRogueUI ui, string message,
                                   int x0, int y0)
        {
            DrawStringBold(ui, currentColor, message, x0, y0);
        }

        public void DrawStringBold(IRogueUI ui, Color color, string message,
                                   int x0, int y0)
        {
            ui.UI_DrawStringBold(color, message, x0 + x, y0 + y);
        }

        public void FillRect(IRogueUI ui, Color color, Rectangle rect)
        {
            ui.UI_FillRect(color, new Rectangle(
                rect.X + x, rect.Y + y, rect.Width, rect.Height
            ));
        }

        public void DrawImage(IRogueUI ui, string image, int x0, int y0)
        {
            DrawImage(ui, image, x0, y0, Color.White);
        }

        public void DrawImage(IRogueUI ui, string image, int x0, int y0, Color tint)
        {
            ui.UI_DrawImage(image, x0 + x, y0 + y, tint);
        }

        public void DrawImageTransform(IRogueUI ui, string image, int x0, int y0,
                                        float rotation, float scale)
        {
            ui.UI_DrawImageTransform(image, x0 + x, y0 + y, rotation, scale);
        }

        /* Draws a bar split into 2 segments */
        protected void DrawSplitBar(IRogueUI ui, Rectangle area, int split,
                                    Color color1, Color color2)
        {
            FillRect(ui, color2, area);
            Clamp(ref split, 0, area.Width);
            if(split > 0)
            {
                FillRect(ui, color1, new Rectangle(
                    area.X, area.Y, split, area.Height
                ));
            }
        }

        /* Draws a bar split into 3 segments */
        protected void DrawSplitBar(IRogueUI ui, Rectangle area, int split1, int split2,
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

        /// UTILITY FUNCTIONS

        protected void Swap(ref int lhs, ref int rhs)
        {
            int temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        protected void Clamp(ref int value, int min, int max)
        {
            if(value < min)
                value = min;
            if(value > max)
                value = max;
        }
    }
}