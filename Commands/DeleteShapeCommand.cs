using ShapesApp.Models;

namespace ShapesApp.Commands
{
    public class DeleteShapeCommand : ICommand
    {
        private readonly Scene _scene;
        private readonly Shape _shape;

        public DeleteShapeCommand(Scene scene, Shape shape)
        {
            _scene = scene;
            _shape = shape;
        }

        public void Execute() => _scene.Remove(_shape);

        public void Undo() => _scene.Add(_shape);
    }
}
