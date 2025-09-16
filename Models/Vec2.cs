using System.Drawing;

namespace ShapesApp.Models
{
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public PointF ToPointF() => new PointF(X, Y);
    }
}
