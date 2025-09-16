using System.Drawing;
using Newtonsoft.Json;

namespace ShapesApp.Models
{
    public record ColorInfo(int A, int R, int G, int B)
    {
        [JsonIgnore]
        public Color ToColor => Color.FromArgb(A, R, G, B);

        public static ColorInfo FromColor(Color c) =>
            new ColorInfo(c.A, c.R, c.G, c.B);

        public int ToArgbInt() => (A << 24) | (R << 16) | (G << 8) | B;
    }
}
