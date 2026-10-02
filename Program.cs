using System;
using System.Threading;
using System.Windows.Forms;

namespace ScreensaverExtender
{
    internal static class Program
    {
        private const string AppMutexName = "Global\\ScreensaverExtender_SingleInstance_Mutex_slalglel";

        [STAThread]
        private static void Main()
        {
            // Ensure single instance running
            using (Mutex mutex = new Mutex(true, AppMutexName, out bool isFirstInstance))
            {
                if (!isFirstInstance)
                {
                    MessageBox.Show(
                        "화면보호기 지연 프로그램이 이미 작업표시줄 우측 트레이에서 실행 중입니다.\n아이콘을 확인해 주세요.",
                        "스마트 화면보호기 지연기",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Run purely in tray context without showing a form at startup
                Application.Run(new TrayApplicationContext());
            }
        }
    }
}
