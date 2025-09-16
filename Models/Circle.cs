using System.Drawing;

namespace ShapesApp.Models
{
    public class Circle : Shape
    {
        public float Radius { get; set; } = 50;

        public override void Draw(Graphics g, bool selected)
        {
            using var brush = new SolidBrush(FillColor);
            using var pen = new Pen(StrokeColor, selected ? 3 : 1);
            g.FillEllipse(brush, Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);
            g.DrawEllipse(pen, Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2);
        }

        public override void Move(Vec2 delta)
        {
            Position = new Vec2(Position.X + delta.X, Position.Y + delta.Y);
        }

        public override void Resize(float factor)
        {
            Radius *= factor;
        }

        public override Shape Clone()
        {
            return new Circle
            {
                Position = new Vec2(Position.X, Position.Y),
                Radius = Radius,
                FillColor = FillColor,
                StrokeColor = StrokeColor
            };
        }
    }
}
