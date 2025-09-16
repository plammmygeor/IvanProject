using System.Drawing;

namespace ShapesApp.Models
{
    public class Triangle : Shape
    {
        public Vec2 P1 { get; set; }
        public Vec2 P2 { get; set; }
        public Vec2 P3 { get; set; }

        public override void Draw(Graphics g, bool selected)
        {
            using var brush = new SolidBrush(FillColor);
            using var pen = new Pen(StrokeColor, selected ? 3 : 1);
            g.FillPolygon(brush, new[] { P1.ToPointF(), P2.ToPointF(), P3.ToPointF() });
            g.DrawPolygon(pen, new[] { P1.ToPointF(), P2.ToPointF(), P3.ToPointF() });
        }

        public override void Move(Vec2 delta)
        {
            P1 = new Vec2(P1.X + delta.X, P1.Y + delta.Y);
            P2 = new Vec2(P2.X + delta.X, P2.Y + delta.Y);
            P3 = new Vec2(P3.X + delta.X, P3.Y + delta.Y);
        }

        public override void Resize(float factor)
        {
            var centerX = (P1.X + P2.X + P3.X) / 3;
            var centerY = (P1.Y + P2.Y + P3.Y) / 3;

            P1 = new Vec2(centerX + (P1.X - centerX) * factor, centerY + (P1.Y - centerY) * factor);
            P2 = new Vec2(centerX + (P2.X - centerX) * factor, centerY + (P2.Y - centerY) * factor);
            P3 = new Vec2(centerX + (P3.X - centerX) * factor, centerY + (P3.Y - centerY) * factor);
        }

        public override Shape Clone()
        {
            return new Triangle
            {
                P1 = new Vec2(P1.X, P1.Y),
                P2 = new Vec2(P2.X, P2.Y),
                P3 = new Vec2(P3.X, P3.Y),
                FillColor = FillColor,
                StrokeColor = StrokeColor
            };
        }
    }
}
