// yep using forms to deliver the most painful experince

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VeryFunnyGraphs
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Process service;
            service = Process.Start("java", "-jar BFS.jar");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());

            service.Kill(true);
            service.WaitForExit();
            service.Dispose();
        }
    }
}
