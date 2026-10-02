using System;
using System.Runtime.InteropServices;

namespace ScreensaverExtender
{
    public static class Win32Api
    {
        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED  = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_USER_PRESENT     = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS       = 0x80000000
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public const uint SPI_GETSCREENSAVETIMEOUT = 0x000E;
        public const uint SPI_GETSCREENSAVEACTIVE  = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SystemParametersInfo(uint uAction, uint uParam, ref uint lpvParam, uint fuWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SystemParametersInfo(uint uAction, uint uParam, ref bool lpvParam, uint fuWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        /// <summary>
        /// Gets the current idle time (time elapsed since last keyboard/mouse input) in milliseconds.
        /// Handles 32-bit tick count wraparound safely using unsigned arithmetic and unchecked block.
        /// </summary>
        public static uint GetIdleTimeMillis()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);

            if (GetLastInputInfo(ref lii))
            {
                uint currentTick = (uint)Environment.TickCount;
                unchecked
                {
                    return currentTick - lii.dwTime;
                }
            }

            return 0;
        }

        /// <summary>
        /// Reads the configured Windows screensaver timeout in seconds.
        /// If not set or disabled, falls back to a sensible default (300 seconds / 5 minutes).
        /// </summary>
        public static uint GetScreenSaverTimeoutSeconds()
        {
            uint timeoutSeconds = 0;
            if (SystemParametersInfo(SPI_GETSCREENSAVETIMEOUT, 0, ref timeoutSeconds, 0) && timeoutSeconds > 0)
            {
                return timeoutSeconds;
            }

            // Fallback default: 5 minutes (300 seconds)
            return 300;
        }

        /// <summary>
        /// Checks whether the Windows screensaver is enabled.
        /// </summary>
        public static bool IsScreenSaverActive()
        {
            bool isActive = false;
            if (SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, 0, ref isActive, 0))
            {
                return isActive;
            }
            return true;
        }
    }
}
