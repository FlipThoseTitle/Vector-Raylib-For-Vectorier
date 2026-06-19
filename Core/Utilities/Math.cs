namespace Vector.Core.Utilities
{
    public static class Math
    {
        public static double Round(double Value, double Pow)
        {
            return System.Math.Floor(Value * Pow + 0.5) / Pow;
        }

        public static float Round(float Value, float Pow)
        {
            return MathF.Floor(Value * Pow + 0.5f) / Pow;
        }
    }
}
