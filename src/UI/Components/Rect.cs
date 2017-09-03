
namespace djack.RogueSurvivor.UI.Components
{
    /* Rectangle of pixels on the screen. Useful to delimit UI elements */
    public class Rect
    {
        public int x; // X position
        public int y; // Y position
        public int h; // Height in pixels
        public int w; // Width in pixels

        public Rect(int _x, int _y, int _h, int _w)
        {
            x = _x; y = _y; h = _h; w = _w;
        }
        public Rect(Rect copy)
        {
            x = copy.x; y = copy.y; h = copy.h; w = copy.w;
        }
    }
}