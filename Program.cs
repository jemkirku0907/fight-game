using System;
using System.Windows.Forms;

namespace FightingGame
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--ensure-sprites")
            {
                BallerSpriteGenerator.EnsureSprites();
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BattleForm());
        }
    }
}
