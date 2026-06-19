using Raylib_cs;
using System.Numerics;
using Vector.Core.Location;
using Vector.Core.Node;

namespace Vector.Core
{
    public class Triangle
    {
        public ModelNode Node0;

        public ModelNode Node1;

        public ModelNode Node2;

        public bool IsVisible;

        public void Draw(Color color)
        {
            if (!IsVisible) return;

            var vs = Sets.Current.Containers[1];

            Vector2 a = vs.TransformPoint(Node0.Start);
            Vector2 b = vs.TransformPoint(Node1.Start);
            Vector2 c = vs.TransformPoint(Node2.Start);

            Rlgl.DisableBackfaceCulling();
            Rlgl.Begin(DrawMode.Triangles);

            Rlgl.Color4ub(color.R, color.G, color.B, color.A);

            Rlgl.Vertex2f(a.X, a.Y);
            Rlgl.Vertex2f(b.X, b.Y);
            Rlgl.Vertex2f(c.X, c.Y);

            Rlgl.End();
        }
    }
}
