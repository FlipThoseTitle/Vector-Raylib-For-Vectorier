using Raylib_cs;
using System.Numerics;
using Vector.Core.Location;
using Vector.Core.Node;

namespace Vector.Core.Geometry
{
    public class Capsule
    {
        public ModelLine Base;

        public float Stroke = 1;

        public string Name => Base.Name;

        public bool IsVisible = false;

        public void Init(ModelLine modelLine)
        {
            Base = modelLine;
            Stroke = Base.Stroke;
        }

        public void Draw(Color color)
        {
            if (!IsVisible)
            {
                return;
            }

            var vs = Sets.Current.Containers[1];
            Vector2 start = vs.TransformPoint(Base.Start.Start);
            Vector2 start2 = vs.TransformPoint(Base.End.Start);
            float num = start.X - start2.X;
            float num2 = start.Y - start2.Y;
            float num3 = start.X - num * Base.Margin1;
            float num4 = start.Y - num2 * Base.Margin1;
            float num5 = start2.X + num * Base.Margin2;
            float num6 = start2.Y + num2 * Base.Margin2;

            var pos1 = new Vector2((float)num3, (float)num4);
            var pos2 = new Vector2((float)num5, (float)num6);

            Raylib.DrawCircleV(pos1, Stroke * vs.FrameScale, color);
            Raylib.DrawCircleV(pos2, Stroke * vs.FrameScale, color);

            Raylib.DrawLineEx(pos1, pos2, (Stroke * 2) * vs.FrameScale, color);
        }
    }
}
