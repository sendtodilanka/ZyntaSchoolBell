using System;
using System.Threading;
using System.Windows.Forms;

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
                // Application.Run(new MainForm()); // Uncomment in Phase 3
            }
        }
    }
}
