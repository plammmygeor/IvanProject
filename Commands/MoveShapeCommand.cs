using ShapesApp.Models;

namespace ShapesApp.Commands
{
    public class MoveShapeCommand : ICommand
    {
        private readonly Shape _shape;
        private readonly Vec2 _delta;

        public MoveShapeCommand(Shape shape, Vec2 delta)
        {
            _shape = shape;
            _delta = delta;
        }

        public void Execute() => _shape.Move(_delta);

        public void Undo() => _shape.Move(new Vec2(-_delta.X, -_delta.Y));
    }
}