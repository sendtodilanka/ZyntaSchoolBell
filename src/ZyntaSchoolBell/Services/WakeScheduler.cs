using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZyntaSchoolBell.Services
{
    /// <summary>
    /// Schedules Windows waitable timers that can wake the PC from sleep/hibernate
    /// before the next alarm. Accounts for slow hibernate resume on low-end PCs.
    /// </summary>
    public class WakeScheduler : IDisposable
    {
        // Wake the PC this many minutes before the alarm.
        // Accounts for: S4 hibernate resume on HDD (~60-90s) + app startup (~30s) + margin.
        private const int WakeAheadMinutes = 3;

        private readonly AlarmEngine _engine;
        private readonly Thread _schedulerThread;
        private readonly ManualResetEvent _recalcSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private IntPtr _timerHandle;
        private volatile bool _disposed;
        private bool _supported = true;

        #region P/Invoke

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateWaitableTimer(
            IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetWaitableTimer(
            IntPtr hTimer,
            ref long pDueTime,
            int lPeriod,
            IntPtr pfnCompletionRoutine,
            IntPtr lpArgToCompletionRoutine,
            bool fResume);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelWaitableTimer(IntPtr hTimer);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        public WakeScheduler(AlarmEngine engine)
        {
            _engine = engine;

            _timerHandle = CreateWaitableTimer(IntPtr.Zero, true, null);
            if (_timerHandle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Warn($"WakeScheduler: CreateWaitableTimer failed (error {err}). Wake-from-sleep disabled.");
                _supported = false;
                return;
            }

            _schedulerThread = new Thread(SchedulerLoop)
            {
                IsBackground = true,
                Name = "WakeScheduler",
                Priority = ThreadPriority.BelowNormal
            };
            _schedulerThread.Start();
            Logger.Info($"WakeScheduler started (wake-ahead: {WakeAheadMinutes} min)");
        }

        /// <summary>
        /// Call when alarms change (profile switch, add/edit/delete) to recalculate the wake timer.
        /// </summary>
        public void Recalculate()
        {
            if (!_supported) return;
            _recalcSignal.Set();
        }

        private void SchedulerLoop()
        {
            while (!_disposed)
            {
                try
                {
                    ScheduleNextWake();

                    // Wait for recalculation signal, stop signal, or periodic re-check (60s).
                    // The 60s timeout ensures we pick up any time drift or newly relevant alarms.
                    WaitHandle.WaitAny(
                        new WaitHandle[] { _recalcSignal, _stopSignal },
                        TimeSpan.FromSeconds(60));

                    _recalcSignal.Reset();
                }
                catch (Exception ex)
                {
                    Logger.Error("WakeScheduler loop error", ex);
                    // Back off to avoid spin-looping on persistent errors
                    if (!_disposed) Thread.Sleep(5000);
                }
            }
        }

        private void ScheduleNextWake()
        {
            if (_timerHandle == IntPtr.Zero) return;

            var nextAlarm = _engine.GetNextAlarm();
            if (nextAlarm == null)
            {
                // No pending alarms — cancel any existing wake timer
                CancelWaitableTimer(_timerHandle);
                return;
            }

            if (!TimeSpan.TryParse(nextAlarm.Time, out var alarmTs))
            {
                Logger.Warn($"WakeScheduler: invalid alarm time format '{nextAlarm.Time}'");
                return;
            }

            DateTime alarmTime = DateTime.Today.Add(alarmTs);
            DateTime wakeTime = alarmTime.AddMinutes(-WakeAheadMinutes);

            if (wakeTime <= DateTime.Now)
            {
                // Wake time already passed — PC is awake, AlarmEngine will handle it
                return;
            }

            // SetWaitableTimer expects absolute time as a FILETIME (100ns intervals since 1601)
            // Negative values = relative time, positive = absolute UTC FILETIME
            long dueTime = wakeTime.ToFileTimeUtc();

            // fResume = true → wake the PC from S3 sleep or S4 hibernate
            bool success = SetWaitableTimer(
                _timerHandle,
                ref dueTime,
                0,                  // no periodic repeat
                IntPtr.Zero,        // no completion routine
                IntPtr.Zero,        // no arg
                true);              // fResume: WAKE THE PC

            if (success)
            {
                Logger.Info($"WakeScheduler: PC will wake at {wakeTime:HH:mm:ss} for alarm at {nextAlarm.Time} ({nextAlarm.Label})");
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Warn($"WakeScheduler: SetWaitableTimer failed (error {err}). Some hardware does not support timed wake.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopSignal.Set();

            if (_timerHandle != IntPtr.Zero)
            {
                CancelWaitableTimer(_timerHandle);
                CloseHandle(_timerHandle);
                _timerHandle = IntPtr.Zero;
            }

            // Wait for thread to exit gracefully
            _schedulerThread?.Join(3000);

            _recalcSignal.Dispose();
            _stopSignal.Dispose();

            Logger.Info("WakeScheduler disposed");
        }
    }
}
