using System.Drawing;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        public class Block
        {
            /// Rectangle enclosing the whole block.
            public Rectangle Rectangle { get; set; }

            /// Rectangle enclosing the building : the blocks minus the walkway ring.
            public Rectangle BuildingRect { get; set; }

            /// Rectangle enclosing the inside of the building : the building minus the walls ring.
            public Rectangle InsideRect { get; set; }

            /// CTOR
            public Block(Rectangle rect)
            {
                ResetRectangle(rect);
            }

            /// COPY CTOR
            public Block(Block copyFrom)
            {
                this.Rectangle = copyFrom.Rectangle;
                this.BuildingRect = copyFrom.BuildingRect;
                this.InsideRect = copyFrom.InsideRect;
            }

            public void ResetRectangle(Rectangle rect)
            {
                this.Rectangle = rect;
                this.BuildingRect = new Rectangle(rect.Left + 1, rect.Top + 1, rect.Width - 2, rect.Height - 2);
                this.InsideRect = new Rectangle(this.BuildingRect.Left + 1, this.BuildingRect.Top + 1, this.BuildingRect.Width - 2, this.BuildingRect.Height - 2);
            }
        }
    }
}