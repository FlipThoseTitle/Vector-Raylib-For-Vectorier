using System.Numerics;

namespace Vector.Core.Node
{
    public class CameraNode
    {
        public ModelNode Node
        {
            get;
        }

        public Vector3 End => Node == null ? new Vector3() : Node.End;

        public Vector3 Start => Node == null ? new Vector3() : Node.Start;

        private void Redraw()
        {
            Node.Radius = 8;
        }

        public CameraNode(ModelNode node)
        {
            if (node == null)
            {
                return;
            }
            Node = node;
            Node.Radius = 8;
        }
    }
}
