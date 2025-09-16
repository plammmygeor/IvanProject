using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ShapesApp.Models;
using ShapesApp.Commands;
using ShapesApp.Utils;

namespace ShapesApp.UI
{
    public class MainForm : Form
    {
        private readonly Scene _scene = new();
        private readonly CommandHistory _history = new();

        private Shape? _selected;
        private bool _isDragging;
        private Point _lastMousePos;
        private Shape? _copiedShape;

        // Resize state
        private Shape? _resizeTarget;
        private RectangleF? _activeHandle;
        private int _activeHandleIndex = -1;
        private Point _handleStartMouse;
        private Vec2 _handleStartSize;
        private Vec2 _anchorPoint;

        // Rotation state
        private bool _isRotating;
        private Vec2 _rotateCenter;
        private float _rotateStartAngleDeg;   // angle from center to mouse when rotation started
        private float _shapeStartRotationDeg; // for rect/ellipse
        private Vec2 _triP1Start, _triP2Start, _triP3Start;
        private Vec2 _lineStartStart, _lineEndStart;

        private const int HandleSize = 8;
        private const float RotationHandleRadius = 7f;
        private const float RotationHandleOffset = 22f; // distance above top edge

        public MainForm()
        {
            Text = "Shapes App";
            DoubleBuffered = true;
            Width = 900;
            Height = 600;

            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            MouseDown += MainForm_MouseDown;
            MouseUp += MainForm_MouseUp;
            MouseMove += MainForm_MouseMove;
            Paint += MainForm_Paint;
            MouseLeave += (_, __) => Cursor = Cursors.Default;

            InitializeMenu();
        }

        private void InitializeMenu()
        {
            var menu = new MenuStrip();

            // File menu
            var fileMenu = new ToolStripMenuItem("File");
            var save = new ToolStripMenuItem("Save", null, (_, __) => SaveScene()) { ShortcutKeys = Keys.Control | Keys.S };
            var load = new ToolStripMenuItem("Load", null, (_, __) => LoadScene()) { ShortcutKeys = Keys.Control | Keys.O };
            fileMenu.DropDownItems.AddRange(new[] { save, load });

            // Shapes menu
            var shapesMenu = new ToolStripMenuItem("Shapes");
            var newCircle   = new ToolStripMenuItem("Add Circle",    null, (_, __) => AddCircle())    { ShortcutKeys = Keys.Control | Keys.Shift | Keys.C };
            var newRect     = new ToolStripMenuItem("Add Rectangle", null, (_, __) => AddRectangle()) { ShortcutKeys = Keys.Control | Keys.R };
            var newLine     = new ToolStripMenuItem("Add Line",      null, (_, __) => AddLine())      { ShortcutKeys = Keys.Control | Keys.L };
            var newEllipse  = new ToolStripMenuItem("Add Ellipse",   null, (_, __) => AddEllipse())   { ShortcutKeys = Keys.Control | Keys.E };
            var newTriangle = new ToolStripMenuItem("Add Triangle",  null, (_, __) => AddTriangle())  { ShortcutKeys = Keys.Control | Keys.T };
            shapesMenu.DropDownItems.AddRange(new[] { newCircle, newRect, newLine, newEllipse, newTriangle });

            // Edit menu (Increase/Decrease removed)
            var editMenu = new ToolStripMenuItem("Edit");
            var deleteShape = new ToolStripMenuItem("Delete Selected", null, (_, __) => DeleteSelected()) { ShortcutKeys = Keys.Delete };
            var copy   = new ToolStripMenuItem("Copy",  null, (_, __) => CopySelected())  { ShortcutKeys = Keys.Control | Keys.C };
            var paste  = new ToolStripMenuItem("Paste", null, (_, __) => PasteShape())    { ShortcutKeys = Keys.Control | Keys.V };
            var fill   = new ToolStripMenuItem("Fill Color", null, (_, __) => FillColorSelected()) { ShortcutKeys = Keys.Control | Keys.F };
            var undo   = new ToolStripMenuItem("Undo", null, (_, __) => { _history.Undo(); Invalidate(); }) { ShortcutKeys = Keys.Control | Keys.Z };
            var redo   = new ToolStripMenuItem("Redo", null, (_, __) => { _history.Redo(); Invalidate(); }) { ShortcutKeys = Keys.Control | Keys.Y };
            editMenu.DropDownItems.AddRange(new[] { deleteShape, copy, paste, fill, undo, redo });

            // Help menu
            var helpMenu = new ToolStripMenuItem("Help");
            var showHelp = new ToolStripMenuItem("Commands Info", null, (_, __) => ShowHelp()) { ShortcutKeys = Keys.Control | Keys.H };
            helpMenu.DropDownItems.Add(showHelp);

            // Add menus
            menu.Items.AddRange(new[] { fileMenu, shapesMenu, editMenu, helpMenu });
            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        #region Shape Actions

        private void AddCircle()
        {
            var circle = new Circle { Position = new Vec2(100, 100), Radius = 40 };
            _history.Execute(new CreateShapeCommand(_scene, circle));
            Invalidate();
        }

        private void AddRectangle()
        {
            var rect = new RectangleShape { Position = new Vec2(200, 150), Width = 100, Height = 60 };
            _history.Execute(new CreateShapeCommand(_scene, rect));
            Invalidate();
        }

        private void AddLine()
        {
            var line = new LineSegment { Position = new Vec2(300, 200), End = new Vec2(400, 300) };
            _history.Execute(new CreateShapeCommand(_scene, line));
            Invalidate();
        }

        private void AddEllipse()
        {
            var ellipse = new EllipseShape { Position = new Vec2(250, 200), Width = 120, Height = 80 };
            _history.Execute(new CreateShapeCommand(_scene, ellipse));
            Invalidate();
        }

        private void AddTriangle()
        {
            var tri = new Triangle
            {
                P1 = new Vec2(400, 300),
                P2 = new Vec2(450, 350),
                P3 = new Vec2(350, 350)
            };
            _history.Execute(new CreateShapeCommand(_scene, tri));
            Invalidate();
        }

        private void DeleteSelected()
        {
            if (_selected != null)
            {
                _history.Execute(new DeleteShapeCommand(_scene, _selected));
                _selected = null;
                Invalidate();
            }
        }

        private void CopySelected()
        {
            if (_selected != null)
                _copiedShape = _selected.Clone();
        }

        private void PasteShape()
        {
            if (_copiedShape != null)
            {
                var clone = _copiedShape.Clone();
                clone.Move(new Vec2(20, 20));
                _history.Execute(new CreateShapeCommand(_scene, clone));
                Invalidate();
            }
        }

        private void ResizeSelected(float factor)
        {
            if (_selected != null)
            {
                _selected.Resize(factor);
                Invalidate();
            }
        }

        #endregion

        #region Save / Load

        private void SaveScene()
        {
            using var dlg = new SaveFileDialog { Filter = "JSON Files|*.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
                SceneSerializer.Save(_scene, dlg.FileName);
        }

        private void LoadScene()
        {
            using var dlg = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var loaded = SceneSerializer.Load(dlg.FileName);
                if (loaded != null)
                {
                    _scene.Clear();
                    foreach (var s in loaded.Shapes)
                        _scene.Add(s);
                    _selected = null;
                    Invalidate();
                }
            }
        }

        #endregion

        #region Handles / Hit-Testing helpers

        private IEnumerable<RectangleF> GetResizeHandles(Shape shape)
        {
            switch (shape)
            {
                case RectangleShape r:
                    return new[]
                    {
                        new RectangleF(r.Position.X - HandleSize/2,               r.Position.Y - HandleSize/2,               HandleSize, HandleSize), // TL
                        new RectangleF(r.Position.X + r.Width - HandleSize/2,     r.Position.Y - HandleSize/2,               HandleSize, HandleSize), // TR
                        new RectangleF(r.Position.X - HandleSize/2,               r.Position.Y + r.Height - HandleSize/2,    HandleSize, HandleSize), // BL
                        new RectangleF(r.Position.X + r.Width - HandleSize/2,     r.Position.Y + r.Height - HandleSize/2,    HandleSize, HandleSize), // BR
                    };

                case Circle c:
                    return new[]
                    {
                        new RectangleF(c.Position.X + c.Radius - HandleSize/2,    c.Position.Y - HandleSize/2,               HandleSize, HandleSize), // Right
                        new RectangleF(c.Position.X - c.Radius - HandleSize/2,    c.Position.Y - HandleSize/2,               HandleSize, HandleSize), // Left
                        new RectangleF(c.Position.X - HandleSize/2,               c.Position.Y + c.Radius - HandleSize/2,    HandleSize, HandleSize), // Bottom
                        new RectangleF(c.Position.X - HandleSize/2,               c.Position.Y - c.Radius - HandleSize/2,    HandleSize, HandleSize), // Top
                    };

                case EllipseShape e:
                    return new[]
                    {
                        new RectangleF(e.Position.X - HandleSize/2,               e.Position.Y - HandleSize/2,               HandleSize, HandleSize), // TL
                        new RectangleF(e.Position.X + e.Width - HandleSize/2,     e.Position.Y - HandleSize/2,               HandleSize, HandleSize), // TR
                        new RectangleF(e.Position.X - HandleSize/2,               e.Position.Y + e.Height - HandleSize/2,    HandleSize, HandleSize), // BL
                        new RectangleF(e.Position.X + e.Width - HandleSize/2,     e.Position.Y + e.Height - HandleSize/2,    HandleSize, HandleSize), // BR
                    };

                case Triangle t:
                    return new[]
                    {
                        new RectangleF(t.P1.X - HandleSize/2, t.P1.Y - HandleSize/2, HandleSize, HandleSize),
                        new RectangleF(t.P2.X - HandleSize/2, t.P2.Y - HandleSize/2, HandleSize, HandleSize),
                        new RectangleF(t.P3.X - HandleSize/2, t.P3.Y - HandleSize/2, HandleSize, HandleSize)
                    };

                case LineSegment l:
                    return new[]
                    {
                        new RectangleF(l.Position.X - HandleSize/2, l.Position.Y - HandleSize/2, HandleSize, HandleSize), // Start
                        new RectangleF(l.End.X      - HandleSize/2, l.End.Y      - HandleSize/2, HandleSize, HandleSize), // End
                    };

                default:
                    return Enumerable.Empty<RectangleF>();
            }
        }

        private RectangleF GetBounds(Shape s)
        {
            switch (s)
            {
                case RectangleShape r:
                    return new RectangleF(r.Position.X, r.Position.Y, r.Width, r.Height);
                case EllipseShape e:
                    return new RectangleF(e.Position.X, e.Position.Y, e.Width, e.Height);
                case Circle c:
                    return new RectangleF(c.Position.X - c.Radius, c.Position.Y - c.Radius, c.Radius * 2, c.Radius * 2);
                case Triangle t:
                {
                    float minX = Math.Min(t.P1.X, Math.Min(t.P2.X, t.P3.X));
                    float minY = Math.Min(t.P1.Y, Math.Min(t.P2.Y, t.P3.Y));
                    float maxX = Math.Max(t.P1.X, Math.Max(t.P2.X, t.P3.X));
                    float maxY = Math.Max(t.P1.Y, Math.Max(t.P2.Y, t.P3.Y));
                    return new RectangleF(minX, minY, maxX - minX, maxY - minY);
                }
                case LineSegment l:
                {
                    float minX = Math.Min(l.Position.X, l.End.X);
                    float minY = Math.Min(l.Position.Y, l.End.Y);
                    float maxX = Math.Max(l.Position.X, l.End.X);
                    float maxY = Math.Max(l.Position.Y, l.End.Y);
                    return new RectangleF(minX, minY, maxX - minX, maxY - minY);
                }
                default:
                    return RectangleF.Empty;
            }
        }

        private Vec2 GetCenter(Shape s)
        {
            var b = GetBounds(s);
            return new Vec2(b.Left + b.Width / 2f, b.Top + b.Height / 2f);
        }

        private PointF GetRotationHandleCenter(Shape s)
        {
            var b = GetBounds(s);
            var topCenter = new PointF(b.Left + b.Width / 2f, b.Top);
            return new PointF(topCenter.X, topCenter.Y - RotationHandleOffset);
        }

        private bool IsOnRotationHandle(Shape s, Point mouse)
        {
            var hc = GetRotationHandleCenter(s);
            float dx = mouse.X - hc.X;
            float dy = mouse.Y - hc.Y;
            return (dx * dx + dy * dy) <= (RotationHandleRadius * RotationHandleRadius);
        }

        private int GetHandleIndexAt(Point mouse)
        {
            if (_selected == null) return -1;
            int i = 0;
            foreach (var h in GetResizeHandles(_selected))
            {
                if (h.Contains(mouse)) return i;
                i++;
            }
            return -1;
        }

        private Cursor GetCursorForHandle(Shape shape, int handleIndex)
        {
            switch (shape)
            {
                case RectangleShape:
                case EllipseShape:
                    // 0=TL,1=TR,2=BL,3=BR
                    return handleIndex switch
                    {
                        0 => Cursors.SizeNWSE,
                        1 => Cursors.SizeNESW,
                        2 => Cursors.SizeNESW,
                        3 => Cursors.SizeNWSE,
                        _ => Cursors.Default
                    };
                case Circle:
                    // 0=Right,1=Left,2=Bottom,3=Top
                    return handleIndex switch
                    {
                        0 => Cursors.SizeWE,
                        1 => Cursors.SizeWE,
                        2 => Cursors.SizeNS,
                        3 => Cursors.SizeNS,
                        _ => Cursors.Default
                    };
                case LineSegment:
                case Triangle:
                    return Cursors.SizeAll;
                default:
                    return Cursors.Default;
            }
        }

        private static float Atan2Deg(float y, float x) => (float)(Math.Atan2(y, x) * 180.0 / Math.PI);
        private static float NormalizeDeg(float a)
        {
            while (a <= -180f) a += 360f;
            while (a > 180f) a -= 360f;
            return a;
        }

        private static Vec2 RotatePoint(Vec2 p, Vec2 center, float angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            float dx = p.X - center.X;
            float dy = p.Y - center.Y;
            return new Vec2(
                (float)(center.X + dx * cos - dy * sin),
                (float)(center.Y + dx * sin + dy * cos)
            );
        }

        #endregion

        #region Keyboard / Mouse

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z) { _history.Undo(); Invalidate(); }
            else if (e.Control && e.KeyCode == Keys.Y) { _history.Redo(); Invalidate(); }
            else if (e.KeyCode == Keys.Delete) { DeleteSelected(); }
            else if (e.Control && e.KeyCode == Keys.C) { CopySelected(); }
            else if (e.Control && e.KeyCode == Keys.V) { PasteShape(); }
            else if (e.Control && e.KeyCode == Keys.F) { FillColorSelected(); }
        }

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            var p = new Vec2(e.X, e.Y);

            // Start rotation if clicked on rotation handle
            if (_selected != null && IsOnRotationHandle(_selected, e.Location))
            {
                _isRotating = true;
                _rotateCenter = GetCenter(_selected);
                _rotateStartAngleDeg = Atan2Deg(e.Y - _rotateCenter.Y, e.X - _rotateCenter.X);

                switch (_selected)
                {
                    case RectangleShape rs:
                        _shapeStartRotationDeg = rs.Rotation;
                        break;
                    case EllipseShape es:
                        _shapeStartRotationDeg = es.Rotation;
                        break;
                    case Triangle t:
                        _triP1Start = t.P1;
                        _triP2Start = t.P2;
                        _triP3Start = t.P3;
                        break;
                    case LineSegment l:
                        _lineStartStart = l.Position;
                        _lineEndStart = l.End;
                        break;
                }
                Cursor = Cursors.Hand;
                return;
            }

            // Check resize handles first
            if (_selected != null)
            {
                var handles = GetResizeHandles(_selected).ToList();
                for (int i = 0; i < handles.Count; i++)
                {
                    if (handles[i].Contains(e.Location))
                    {
                        _resizeTarget = _selected;
                        _activeHandle = handles[i];
                        _activeHandleIndex = i;
                        _handleStartMouse = e.Location;

                        switch (_resizeTarget)
                        {
                            case RectangleShape r:
                                _anchorPoint = i switch
                                {
                                    0 => new Vec2(r.Position.X + r.Width, r.Position.Y + r.Height), // anchor BR
                                    1 => new Vec2(r.Position.X,           r.Position.Y + r.Height), // anchor BL
                                    2 => new Vec2(r.Position.X + r.Width, r.Position.Y),           // anchor TR
                                    _ => new Vec2(r.Position.X,           r.Position.Y)            // anchor TL
                                };
                                _handleStartSize = new Vec2(r.Width, r.Height);
                                break;

                            case EllipseShape el:
                                _anchorPoint = i switch
                                {
                                    0 => new Vec2(el.Position.X + el.Width, el.Position.Y + el.Height), // anchor BR
                                    1 => new Vec2(el.Position.X,            el.Position.Y + el.Height), // anchor BL
                                    2 => new Vec2(el.Position.X + el.Width, el.Position.Y),            // anchor TR
                                    _ => new Vec2(el.Position.X,            el.Position.Y)              // anchor TL
                                };
                                _handleStartSize = new Vec2(el.Width, el.Height);
                                break;

                            case Circle c:
                                _handleStartSize = new Vec2(c.Radius, c.Radius);
                                _anchorPoint = c.Position; // not used for circle
                                break;

                            case Triangle t:
                                _triP1Start = t.P1;
                                _triP2Start = t.P2;
                                _triP3Start = t.P3;
                                _handleStartSize = new Vec2(0, 0);
                                _anchorPoint = new Vec2(0, 0);
                                break;

                            case LineSegment l:
                                _lineStartStart = l.Position;
                                _lineEndStart   = l.End;
                                break;
                        }
                        Cursor = GetCursorForHandle(_resizeTarget, _activeHandleIndex);
                        return; // start resizing
                    }
                }
            }

            // Otherwise, normal selection/drag
            _selected = _scene.FindShapeAt(p);
            if (_selected != null && e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePos = e.Location;
            }

            Invalidate();
        }

        private void MainForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isDragging && _selected != null)
            {
                var dx = e.X - _lastMousePos.X;
                var dy = e.Y - _lastMousePos.Y;
                if (dx != 0 || dy != 0)
                    _history.Execute(new MoveShapeCommand(_selected, new Vec2(dx, dy)));
            }

            _resizeTarget = null;
            _activeHandle = null;
            _activeHandleIndex = -1;
            _isDragging = false;
            _isRotating = false;
            Cursor = Cursors.Default;
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            // Rotation
            if (_isRotating && _selected != null)
            {
                Cursor = Cursors.Hand;
                float curAngle = Atan2Deg(e.Y - _rotateCenter.Y, e.X - _rotateCenter.X);
                float delta = NormalizeDeg(curAngle - _rotateStartAngleDeg);

                switch (_selected)
                {
                    case RectangleShape rs:
                        rs.Rotation = NormalizeDeg(_shapeStartRotationDeg + delta);
                        break;
                    case EllipseShape es:
                        es.Rotation = NormalizeDeg(_shapeStartRotationDeg + delta);
                        break;
                    case Triangle t:
                        t.P1 = RotatePoint(_triP1Start, _rotateCenter, delta);
                        t.P2 = RotatePoint(_triP2Start, _rotateCenter, delta);
                        t.P3 = RotatePoint(_triP3Start, _rotateCenter, delta);
                        break;
                    case LineSegment l:
                        l.Position = RotatePoint(_lineStartStart, _rotateCenter, delta);
                        l.End      = RotatePoint(_lineEndStart,   _rotateCenter, delta);
                        break;
                }

                Invalidate();
                return;
            }

            // Resizing
            if (_resizeTarget != null && _activeHandle.HasValue && _activeHandleIndex >= 0)
            {
                Cursor = GetCursorForHandle(_resizeTarget, _activeHandleIndex);

                var mx = (float)e.X;
                var my = (float)e.Y;

                switch (_resizeTarget)
                {
                    case RectangleShape r:
                    {
                        const float minW = 10f, minH = 10f;
                        float w = Math.Max(minW, Math.Abs(mx - _anchorPoint.X));
                        float h = Math.Max(minH, Math.Abs(my - _anchorPoint.Y));
                        float newX = (mx >= _anchorPoint.X) ? _anchorPoint.X : _anchorPoint.X - w;
                        float newY = (my >= _anchorPoint.Y) ? _anchorPoint.Y : _anchorPoint.Y - h;
                        r.Position = new Vec2(newX, newY);
                        r.Width = w; r.Height = h;
                        break;
                    }

                    case EllipseShape el:
                    {
                        const float minW = 10f, minH = 10f;
                        float w = Math.Max(minW, Math.Abs(mx - _anchorPoint.X));
                        float h = Math.Max(minH, Math.Abs(my - _anchorPoint.Y));
                        float newX = (mx >= _anchorPoint.X) ? _anchorPoint.X : _anchorPoint.X - w;
                        float newY = (my >= _anchorPoint.Y) ? _anchorPoint.Y : _anchorPoint.Y - h;
                        el.Position = new Vec2(newX, newY);
                        el.Width = w; el.Height = h;
                        break;
                    }

                    case Circle c:
                    {
                        var dx = mx - _handleStartMouse.X;
                        c.Radius = Math.Max(5f, _handleStartSize.X + dx);
                        break;
                    }

                    case Triangle t:
                    {
                        var dx = mx - _handleStartMouse.X;
                        var dy = my - _handleStartMouse.Y;
                        if      (_activeHandleIndex == 0) t.P1 = new Vec2(_triP1Start.X + dx, _triP1Start.Y + dy);
                        else if (_activeHandleIndex == 1) t.P2 = new Vec2(_triP2Start.X + dx, _triP2Start.Y + dy);
                        else if (_activeHandleIndex == 2) t.P3 = new Vec2(_triP3Start.X + dx, _triP3Start.Y + dy);
                        break;
                    }

                    case LineSegment l:
                    {
                        var dx = mx - _handleStartMouse.X;
                        var dy = my - _handleStartMouse.Y;
                        if      (_activeHandleIndex == 0) l.Position = new Vec2(_lineStartStart.X + dx, _lineStartStart.Y + dy);
                        else if (_activeHandleIndex == 1) l.End      = new Vec2(_lineEndStart.X   + dx, _lineEndStart.Y   + dy);
                        break;
                    }
                }

                Invalidate();
                return;
            }

            // Dragging (move whole shape)
            if (_isDragging && _selected != null)
            {
                Cursor = Cursors.SizeAll;
                var dx = e.X - _lastMousePos.X;
                var dy = e.Y - _lastMousePos.Y;
                _selected.Move(new Vec2(dx, dy));
                _lastMousePos = e.Location;
                Invalidate();
                return;
            }

            // Hover: resize or rotate cursor
            if (_selected != null)
            {
                if (IsOnRotationHandle(_selected, e.Location))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
                int handleIdx = GetHandleIndexAt(e.Location);
                if (handleIdx >= 0)
                {
                    Cursor = GetCursorForHandle(_selected, handleIdx);
                    return;
                }
            }

            // Else, show move cursor when over any shape
            var hit = _scene.FindShapeAt(new Vec2(e.X, e.Y));
            Cursor = hit != null ? Cursors.SizeAll : Cursors.Default;
        }

        #endregion

        #region Paint

        private void MainForm_Paint(object? sender, PaintEventArgs e)
        {
            foreach (var shape in _scene.Shapes)
                shape.Draw(e.Graphics, shape == _selected);

            if (_selected != null)
            {
                // Draw resize handles
                using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                foreach (var handle in GetResizeHandles(_selected))
                {
                    e.Graphics.FillRectangle(Brushes.White, handle);
                    e.Graphics.DrawRectangle(pen, Rectangle.Round(handle));
                }

                // Draw rotation handle & stem
                var bounds = GetBounds(_selected);
                var hc = GetRotationHandleCenter(_selected);
                using var stemPen = new Pen(Color.Gray, 1);
                e.Graphics.DrawLine(stemPen, bounds.Left + bounds.Width / 2f, bounds.Top, hc.X, hc.Y);

                using var rhBrush = new SolidBrush(Color.White);
                using var rhPen = new Pen(Color.Black, 1);
                e.Graphics.FillEllipse(rhBrush, hc.X - RotationHandleRadius, hc.Y - RotationHandleRadius, RotationHandleRadius * 2, RotationHandleRadius * 2);
                e.Graphics.DrawEllipse(rhPen, hc.X - RotationHandleRadius, hc.Y - RotationHandleRadius, RotationHandleRadius * 2, RotationHandleRadius * 2);
            }
        }

        #endregion

        #region Misc

        private void FillColorSelected()
        {
            if (_selected == null) return;
            using var dlg = new ColorDialog();
            dlg.Color = _selected.FillColor;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _selected.FillColor = dlg.Color;
                Invalidate();
            }
        }

        private void ShowHelp()
        {
            string helpText = @"Commands and Shortcuts:

1. Delete - Select + Delete
2. Undo - Ctrl + Z
3. Redo - Ctrl + Y
4. Copy - Ctrl + C
5. Paste - Ctrl + V
6. Fill Color - Ctrl + F
7. Add Circle - Ctrl+Shift+C or via menu
8. Add Rectangle - Ctrl+R
9. Add Line - Ctrl+L
10. Add Ellipse - Ctrl+E
11. Add Triangle - Ctrl+T
12. Select/Move - Click and drag
13. Resize - Drag square handles
14. Rotate - Drag the small circle above the shape";

            MessageBox.Show(helpText, "Help - Commands", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
