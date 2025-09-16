using ShapesApp.Models;

namespace ShapesApp.Commands
{
    public class CreateShapeCommand : ICommand
    {
        private readonly Scene _scene;
        private readonly Shape _shape;

        public CreateShapeCommand(Scene scene, Shape shape)
        {
            _scene = scene;
            _shape = shape;
        }

        public void Execute() => _scene.Add(_shape);

        public void Undo() => _scene.Remove(_shape);
    }
}
