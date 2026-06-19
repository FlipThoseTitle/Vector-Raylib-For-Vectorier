using System.Numerics;
using Vector.Core.Models;
using Vector.Core.Node;
using Vector.Core.Runners;

namespace Vector.Core.Result
{
    public class Collision
    {
        public ModelLine Edge;

        public Vector3 Point;

        public QuadRunner Platform;

        public Model Model;
    }
}
