using System.Drawing;

namespace ShapesApp.Models
{
    public class LineSegment : Shape
    {
        public Vec2 End { get; set; }

        public override void Draw(Graphics g, bool selected)
        {
            using var pen = new Pen(StrokeColor, selected ? 3 : 2);
            g.DrawLine(pen, Position.X, Position.Y, End.X, End.Y);
        }

        public override void Move(Vec2 delta)
        {
            Position = new Vec2(Position.X + delta.X, Position.Y + delta.Y);
            End = new Vec2(End.X + delta.X, End.Y + delta.Y);
        }

        public override void Resize(float factor)
        {
            var dx = End.X - Position.X;
            var dy = End.Y - Position.Y;
            End = new Vec2(Position.X + dx * factor, Position.Y + dy * factor);
        }

        public override Shape Clone()
        {
            return new LineSegment
            {
                Position = new Vec2(Position.X, Position.Y),
                End = new Vec2(End.X, End.Y),
                StrokeColor = StrokeColor
            };
        }
    }
}
