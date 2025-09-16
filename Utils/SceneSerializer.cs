using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShapesApp.Models;

namespace ShapesApp.Utils
{
    public static class SceneSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static void Save(Scene scene, string path)
        {
            var json = JsonSerializer.Serialize(scene, Options);
            File.WriteAllText(path, json);
        }

        public static Scene? Load(string path)
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Scene>(json, Options);
        }
    }
}
