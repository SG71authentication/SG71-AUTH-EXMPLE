using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SG71AuthExample;

namespace exmple
{
    internal static class Program
    {
        public static bool JustUpdated { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            JustUpdated = args != null && args.Contains(SelfUpdater.UpdatedFlagArg, StringComparer.OrdinalIgnoreCase);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
