using System;
using System.Threading;
using System.Windows.Forms;
using ZyntaSchoolBell.Services;
using ZyntaSchoolBell.UI;

namespace ZyntaSchoolBell
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(true, "ZyntaSchoolBell_SingleInstance", out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "ZyntaSchoolBell is already running.",
                        "ZyntaSchoolBell",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Logger.Info("ZyntaSchoolBell starting");
                Application.Run(new MainForm());
                Logger.Info("ZyntaSchoolBell exiting");
            }
        }
    }
}
