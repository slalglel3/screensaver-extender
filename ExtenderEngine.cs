using System;
using System.Windows.Forms;

namespace ScreensaverExtender
{
    public class ExtenderEngine : IDisposable
    {
        // Guard Boundaries
        public const int MinInterval = 1;      // 최소 주기 1분
        public const int MinCount = 1;         // 최소 횟수 1회
        public const int MaxCount = 100;       // 최대 횟수 100회 (오버플로우/장난 방지)

        // Windows ScreenSaver Settings
        public int ScreenSaverTimeoutMinutes { get; private set; } = 5;
        public int MaxInterval => Math.Max(MinInterval, ScreenSaverTimeoutMinutes - 1);

        // User Settings
        public int IntervalMinutes { get; private set; } = 3;
        public int MaxSignalCount { get; private set; } = 2;
        public bool IsEnabled { get; private set; } = true;

        // Runtime State
        public int SentSignalCount { get; private set; } = 0;
        public uint CurrentIdleSeconds { get; private set; } = 0;

        // Total Expected ScreenSaver Delay: (Count * Interval) + ScreenSaverTimeout
        public int TotalExpectedMinutes => (MaxSignalCount * IntervalMinutes) + ScreenSaverTimeoutMinutes;

        // Events for UI sync
        public event EventHandler? SettingsChanged;
        public event EventHandler? StateChanged;
        public event EventHandler<int>? SignalSent; // notifies sent count

        private readonly Timer _adaptiveTimer;
        private bool _disposed = false;

        public ExtenderEngine()
        {
            RefreshScreenSaverTimeout();

            // Set initial interval within safe bounds
            IntervalMinutes = Math.Min(3, MaxInterval);
            if (IntervalMinutes < MinInterval) IntervalMinutes = MinInterval;

            _adaptiveTimer = new Timer();
            _adaptiveTimer.Interval = 15000; // 15초 기본 체크 인터벌 (초경량 부하)
            _adaptiveTimer.Tick += AdaptiveTimer_Tick;
            _adaptiveTimer.Start();
        }

        /// <summary>
        /// Reads current screensaver timeout from Windows API and recalculates bounds.
        /// </summary>
        public void RefreshScreenSaverTimeout()
        {
            uint timeoutSec = Win32Api.GetScreenSaverTimeoutSeconds();
            int minutes = (int)(timeoutSec / 60);
            ScreenSaverTimeoutMinutes = Math.Max(1, minutes);

            // Ensure current interval does not exceed new MaxInterval
            if (IntervalMinutes > MaxInterval)
            {
                IntervalMinutes = MaxInterval;
            }
            if (IntervalMinutes < MinInterval)
            {
                IntervalMinutes = MinInterval;
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanIncreaseInterval => IntervalMinutes < MaxInterval;
        public bool CanDecreaseInterval => IntervalMinutes > MinInterval;
        public bool CanIncreaseCount => MaxSignalCount < MaxCount;
        public bool CanDecreaseCount => MaxSignalCount > MinCount;

        public void IncreaseInterval()
        {
            if (CanIncreaseInterval)
            {
                IntervalMinutes++;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DecreaseInterval()
        {
            if (CanDecreaseInterval)
            {
                IntervalMinutes--;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void IncreaseCount()
        {
            if (CanIncreaseCount)
            {
                MaxSignalCount++;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DecreaseCount()
        {
            if (CanDecreaseCount)
            {
                MaxSignalCount--;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
            if (!IsEnabled)
            {
                SentSignalCount = 0;
            }
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetStatusDescription()
        {
            if (!IsEnabled) return "기능 비활성화됨";
            if (SentSignalCount >= MaxSignalCount)
            {
                return $"연장 완료 ({SentSignalCount}/{MaxSignalCount}회 발송됨, 화면보호기 대기 중)";
            }
            if (CurrentIdleSeconds < 60)
            {
                return $"사용자 작업 중 (대기 0/{MaxSignalCount}회)";
            }
            return $"유휴 감지: 연장 진행 중 ({SentSignalCount}/{MaxSignalCount}회 완료)";
        }

        private void AdaptiveTimer_Tick(object? sender, EventArgs e)
        {
            if (_disposed) return;

            uint idleMillis = Win32Api.GetIdleTimeMillis();
            CurrentIdleSeconds = idleMillis / 1000;

            if (!IsEnabled)
            {
                _adaptiveTimer.Interval = 30000;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            // 1. If user was active recently (< 30 seconds idle), reset sent count
            if (CurrentIdleSeconds < 30)
            {
                if (SentSignalCount != 0)
                {
                    SentSignalCount = 0;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
                _adaptiveTimer.Interval = 15000; // Check again in 15 seconds
                return;
            }

            // 2. If all delay signals were already exhausted, sleep lightly until user returns
            if (SentSignalCount >= MaxSignalCount)
            {
                _adaptiveTimer.Interval = 20000;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            // 3. Calculate target idle time for the NEXT signal in milliseconds
            long nextSignalTargetMillis = (long)(SentSignalCount + 1) * IntervalMinutes * 60 * 1000;
            long remainingMillis = nextSignalTargetMillis - idleMillis;

            if (remainingMillis <= 0)
            {
                // Reached target idle time! Send pulse signal to reset OS screensaver timer
                Win32Api.SetThreadExecutionState(
                    Win32Api.EXECUTION_STATE.ES_SYSTEM_REQUIRED | 
                    Win32Api.EXECUTION_STATE.ES_DISPLAY_REQUIRED
                );

                SentSignalCount++;
                SignalSent?.Invoke(this, SentSignalCount);
                StateChanged?.Invoke(this, EventArgs.Empty);

                // Schedule next check
                if (SentSignalCount < MaxSignalCount)
                {
                    // Next signal is in IntervalMinutes
                    int nextIntervalMs = Math.Min(30000, IntervalMinutes * 60 * 1000);
                    _adaptiveTimer.Interval = Math.Max(1000, nextIntervalMs);
                }
                else
                {
                    // Exhausted, relax timer
                    _adaptiveTimer.Interval = 30000;
                }
            }
            else
            {
                // Not reached yet: sleep adaptively towards target, capped at 15-30s to detect user return
                int nextSleep = (int)Math.Min(remainingMillis, 15000);
                _adaptiveTimer.Interval = Math.Max(1000, nextSleep);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _adaptiveTimer.Stop();
                _adaptiveTimer.Dispose();
            }
        }
    }
}
