using System.Numerics;

namespace Vector.Core.Result
{
    public struct Cross
    {
        private int _Index;

        private Vector3 _Point;

        public int Index => _Index;

        public Vector3 Point => _Point;

        public Cross(Vector3 point, int index)
        {
            _Index = index;
            _Point = point;
        }
    }
}
