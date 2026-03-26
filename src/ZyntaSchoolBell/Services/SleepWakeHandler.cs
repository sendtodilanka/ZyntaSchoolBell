using System;
using Microsoft.Win32;

namespace ZyntaSchoolBell.Services
{
    public class SleepWakeHandler : IDisposable
    {
        private readonly AlarmEngine _engine;
        private DateTime _sleepTime = DateTime.Now;
        private bool _disposed;

        public event EventHandler SystemResumed;

        public SleepWakeHandler(AlarmEngine engine)
        {
            _engine = engine;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            Logger.Info("SleepWakeHandler initialized");
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    _sleepTime = DateTime.Now;
                    Logger.Info("System entering sleep/hibernate");
                    break;

                case PowerModes.Resume:
                    DateTime wakeTime = DateTime.Now;
                    Logger.Info($"System resumed. Sleep duration: {(wakeTime - _sleepTime).TotalMinutes:F1} minutes");

                    // Do NOT fire missed alarms — keep them as fired
                    _engine.ClearFiredForSleepWindow(_sleepTime, wakeTime);

                    SystemResumed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            Logger.Info("SleepWakeHandler disposed");
        }
    }
}
