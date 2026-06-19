using Raylib_cs;
using System.Numerics;
using Vector.Core.Location;
using Vector.Core.Runners;

namespace Vector.Core.Node
{
    public class ModelNode : Vector3Line, IDrawable
    {
        private string _name;

        private int _bothIndex;

        private string _bothName;

        private readonly Vector3 _defaultPosition;

        private QuadRunner _Data;

        private Vector3 _timeStepVector = new Vector3();

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                string[] array = _name.Split('_');
                _bothName = _name.Split('_')[0];
                if (array.Length == 2 && int.TryParse(array[1], out int val))
                {
                    _bothIndex = val;
                }
                else
                {
                    _bothIndex = 0;
                }
            }
        }

        public int Id
        {
            get;
            set;
        }

        public bool IsFixed
        {
            get;
            set;
        }

        public bool IsNodeFixed => !IsFixed && IsType;

        public int Radius
        {
            get;
            set;
        }

        public bool IsPhysics
        {
            get;
            set;
        }

        public bool IsNodePhysics => IsPhysics && IsType;

        public float Weight
        {
            get;
            set;
        }

        public float Attenuation
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        public bool IsType => Type == "Node";

        public MacroNode MacroNode
        {
            get;
            set;
        }

        public bool IsCollisible
        {
            get;
            set;
        }

        public bool IsDetector
        {
            get;
            set;
        }

        public int BothIndex => _bothIndex;

        public string BothName => _bothName;

        public QuadRunner Data
        {
            get => _Data;
            set => _Data = value;
        }

        public float Z { get; set; } = -100;

        public ModelNode(Vector3 position, MacroNode macroNode = null) : base(position, position)
        {
            _defaultPosition = position;
            MacroNode = macroNode;
            _Data = null;

            //GlobalObjects.AddDrawable(this);
        }

        public void Draw()
        {
            var vs = Sets.Current.Containers[1];
            Vector2 start = vs.TransformPoint(Start);
            Vector2 end = vs.TransformPoint(End);

            Raylib.DrawCircleV(start, 1.2f, Color.Green);

            Raylib_cs.Raylib.DrawLineV(start, end, Color.Green);
        }

        public void Reset()
        {
            Start = Start.Set(_defaultPosition);
            End = End.Set(_defaultPosition);
            _Data = null;
        }

        public void Position(Vector3 end, Vector3 start)
        {
            End = End.Set(end);
            Start = Start.Set(start);
        }

        public void PositionStart(Vector3 vector)
        {
            Start = Start.Set(vector);
        }

        public void PositionStart(float x, float y, float z)
        {
            Start = Start.Set(x, y, z);
        }

        public void PositionStartNoRound(float x, float y, float z)
        {
            Start = Start.Set(x, y, z);
        }

        public void PositionEnd(Vector3 vector)
        {
            End = End.Set(vector);
        }

        public void PositionEnd(float x, float y, float z)
        {
            End = End.Set(x, y, z);
        }

        public void EndAssignStart()
        {
            End.X = Start.X;
            End.Y = Start.Y;
            End.Z = Start.Z;
        }

        public void MacroNodeCompute()
        {
            if (MacroNode != null)
            {
                Start = Start.Reset();
                End = End.Reset();
                for (int i = 0; i < MacroNode.ChildNode.Count; i++)
                {
                    Start = Start.Add(MacroNode.ChildNode[i].Start, MacroNode.LCC[i]);
                    End = End.Add(MacroNode.ChildNode[i].End, MacroNode.LCC[i]);
                }
            }
        }

        public void TimeStep(float gravity)
        {
            _timeStepVector = _timeStepVector.Set(Start.X - End.X, Start.Y - End.Y, Start.Z - End.Z);
            if (IsPhysics)
            {
                _timeStepVector = _timeStepVector.Multiply(1f - Attenuation);
            }
            _timeStepVector = _timeStepVector.Add(Start);
            _timeStepVector.Y += gravity;
            EndAssignStart();
            PositionStart(_timeStepVector);
        }
    }
}
