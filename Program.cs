using System;
using System.Windows.Forms;
using ShapesApp.UI;

namespace ShapesApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
