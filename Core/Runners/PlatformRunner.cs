using Raylib_cs;
using Vector.Core.Location;

namespace Vector.Core.Runners
{
    public class PlatformRunner : QuadRunner
    {
        public PlatformRunner(string name, float x, float y, float width, float height, bool sticky) : base(x, y, width, height, sticky, 0, name)
        {
            Z = -11;
            //GlobalObjects.AddDrawable(this);
        }

        public override void Draw()
        {
            if (!IsEnabled)
                return;

            var vs = Sets.Current.Containers[1];
            var pos = vs.TransformPoint(rectangle.Origin);
            Raylib.DrawRectangleV(pos, rectangle.Size * WorldScale, new Color(0, 0, 255, 51));
        }
    }
}
