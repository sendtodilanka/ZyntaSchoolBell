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
            // GUID-based mutex name to prevent name squatting by other processes
            using (var mutex = new Mutex(true, "Global\\{7A3F8B2E-1D4C-4E9A-B5F6-8C2D3E4F5A6B}", out bool createdNew))
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
