using System.Drawing;

namespace ShapesApp.Models
{
    public abstract class Shape
    {
        public Vec2 Position { get; set; } = new Vec2(0, 0);
        public Color FillColor { get; set; } = Color.LightGray;
        public Color StrokeColor { get; set; } = Color.Black;

        /// <summary>
        /// Rotation in degrees (used by shapes that support transform-based drawing).
        /// For shapes that don’t use this, you can ignore it.
        /// </summary>
        public float Rotation { get; set; } = 0f;

        public abstract void Draw(Graphics g, bool selected);
        public abstract void Move(Vec2 delta);
        public abstract void Resize(float factor);
        public abstract Shape Clone();
    }
}
