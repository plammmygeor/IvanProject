using System;
using System.Collections.Generic;
using System.Drawing;

namespace ShapesApp.Models
{
    public class Scene
    {
        private readonly List<Shape> _shapes = new();

        public IEnumerable<Shape> Shapes => _shapes;

        public void Add(Shape shape) => _shapes.Add(shape);
        public void Remove(Shape shape) => _shapes.Remove(shape);
        public void Clear() => _shapes.Clear();

        // Tweak this to make edge selection easier/harder
        private const float EdgeTolerance = 6f;   // px
        private const float LineTolerance = 8f;   // px (for line segments)

        /// <summary>
        /// Returns the topmost shape at the given point (or null if none)
        /// </summary>
        public Shape? FindShapeAt(Vec2 point)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--) // Topmost first
            {
                var shape = _shapes[i];
                if (IsPointInShape(point, shape))
                    return shape;
            }
            return null;
        }

        private bool IsPointInShape(Vec2 p, Shape shape)
        {
            switch (shape)
            {
                case Circle c:
                {
                    var dx = p.X - c.Position.X;
                    var dy = p.Y - c.Position.Y;
                    return dx * dx + dy * dy <= c.Radius * c.Radius;
                }

                case RectangleShape r:
                {
                    // Inside rectangle (filled shapes)
                    if (p.X >= r.Position.X && p.X <= r.Position.X + r.Width &&
                        p.Y >= r.Position.Y && p.Y <= r.Position.Y + r.Height)
                        return true;

                    // Near edges (optional—comment out if you don't want it)
                    bool nearLeft   = Math.Abs(p.X - r.Position.X) <= EdgeTolerance &&
                                      p.Y >= r.Position.Y - EdgeTolerance && p.Y <= r.Position.Y + r.Height + EdgeTolerance;
                    bool nearRight  = Math.Abs(p.X - (r.Position.X + r.Width)) <= EdgeTolerance &&
                                      p.Y >= r.Position.Y - EdgeTolerance && p.Y <= r.Position.Y + r.Height + EdgeTolerance;
                    bool nearTop    = Math.Abs(p.Y - r.Position.Y) <= EdgeTolerance &&
                                      p.X >= r.Position.X - EdgeTolerance && p.X <= r.Position.X + r.Width + EdgeTolerance;
                    bool nearBottom = Math.Abs(p.Y - (r.Position.Y + r.Height)) <= EdgeTolerance &&
                                      p.X >= r.Position.X - EdgeTolerance && p.X <= r.Position.X + r.Width + EdgeTolerance;

                    return nearLeft || nearRight || nearTop || nearBottom;
                }

                case EllipseShape e:
                {
                    // Inside ellipse
                    var cx = e.Position.X + e.Width / 2f;
                    var cy = e.Position.Y + e.Height / 2f;
                    var rx = e.Width / 2f;
                    var ry = e.Height / 2f;

                    if (rx <= 0 || ry <= 0) return false;

                    var nx = (p.X - cx) / rx;
                    var ny = (p.Y - cy) / ry;
                    var v = nx * nx + ny * ny;

                    if (v <= 1f) return true;

                    // Near ellipse edge (approximate): |v - 1| <= epsilon
                    // Map pixel tolerance to normalized epsilon: ~ EdgeTolerance relative to radii
                    var eps = EdgeTolerance * (1f / Math.Max(rx, 1f) + 1f / Math.Max(ry, 1f));
                    return Math.Abs(v - 1f) <= eps;
                }

                case Triangle t:
                {
                    // Inside triangle
                    if (PointInTriangle(p, t.P1, t.P2, t.P3)) return true;

                    // Near any edge (for easier selection)
                    if (PointNearSegment(p, t.P1, t.P2, EdgeTolerance)) return true;
                    if (PointNearSegment(p, t.P2, t.P3, EdgeTolerance)) return true;
                    if (PointNearSegment(p, t.P3, t.P1, EdgeTolerance)) return true;

                    return false;
                }

                case LineSegment l:
                {
                    return PointNearSegment(p, l.Position, l.End, LineTolerance);
                }

                default:
                    return false;
            }
        }

        private bool PointInTriangle(Vec2 pt, Vec2 v1, Vec2 v2, Vec2 v3)
        {
            float d1 = Sign(pt, v1, v2);
            float d2 = Sign(pt, v2, v3);
            float d3 = Sign(pt, v3, v1);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private float Sign(Vec2 p1, Vec2 p2, Vec2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        private bool PointNearSegment(Vec2 pt, Vec2 start, Vec2 end, float tolerance)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            if (dx == 0 && dy == 0)
                return Distance(pt, start) <= tolerance;

            float t = ((pt.X - start.X) * dx + (pt.Y - start.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            var closest = new Vec2(start.X + t * dx, start.Y + t * dy);
            return Distance(pt, closest) <= tolerance;
        }

        private float Distance(Vec2 a, Vec2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
