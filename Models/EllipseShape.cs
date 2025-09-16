using System.Drawing;

namespace ShapesApp.Models
{
    public class EllipseShape : Shape
    {
        public float Width { get; set; } = 100;
        public float Height { get; set; } = 60;

        public override void Draw(Graphics g, bool selected)
        {
            using var brush = new SolidBrush(FillColor);
            using var pen = new Pen(StrokeColor, selected ? 3 : 1);

            // Draw rotated around center using Graphics transform
            float cx = Position.X + Width / 2f;
            float cy = Position.Y + Height / 2f;

            var state = g.Save();
            g.TranslateTransform(cx, cy);
            if (Rotation != 0f) g.RotateTransform(Rotation);
            g.FillEllipse(brush, -Width / 2f, -Height / 2f, Width, Height);
            g.DrawEllipse(pen, -Width / 2f, -Height / 2f, Width, Height);
            g.Restore(state);
        }

        public override void Move(Vec2 delta)
        {
            Position = new Vec2(Position.X + delta.X, Position.Y + delta.Y);
        }

        public override void Resize(float factor)
        {
            Width *= factor;
            Height *= factor;
        }

        public override Shape Clone()
        {
            return new EllipseShape
            {
                Position = new Vec2(Position.X, Position.Y),
                Width = Width,
                Height = Height,
                FillColor = FillColor,
                StrokeColor = StrokeColor,
                Rotation = Rotation
            };
        }
    }
}
