using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZyntaSchoolBell.Models;

namespace ZyntaSchoolBell.Services
{
    public class AlarmFiredEventArgs : EventArgs
    {
        public AlarmEntry Alarm { get; }
        public AlarmFiredEventArgs(AlarmEntry alarm) { Alarm = alarm; }
    }

    public class AlarmEngine : IDisposable
    {
        private readonly object _lock = new object();
        private Timer _timer;
        private HashSet<string> _firedTodaySet = new HashSet<string>();
        private DateTime _lastDate;
        private List<AlarmEntry> _alarms = new List<AlarmEntry>();
        private volatile bool _disposed;
        private volatile bool _isMuted;
        private string _lastCheckedMinute;

        public event EventHandler<AlarmFiredEventArgs> AlarmFired;
        public event EventHandler MidnightReset;

        public bool IsMuted
        {
            get { return _isMuted; }
            set { _isMuted = value; }
        }

        public void Start()
        {
            lock (_lock)
            {
                _lastDate = DateTime.Today;
                _lastCheckedMinute = DateTime.Now.ToString("HH:mm");
                MarkPastAlarmsAsFired();
            }
            _timer = new Timer(OnTick, null, 0, 1000);
            Logger.Info("AlarmEngine started");
        }

        public void UpdateAlarms(List<AlarmEntry> alarms)
        {
            lock (_lock)
            {
                // Defensive copy to prevent cross-thread mutation from UI
                _alarms = (alarms ?? new List<AlarmEntry>())
                    .Select(a => new AlarmEntry
                    {
                        Id = a.Id,
                        Time = a.Time,
                        Label = a.Label,
                        AudioKey = a.AudioKey,
                        Enabled = a.Enabled
                    })
                    .ToList();
                _firedTodaySet.Clear();
                MarkPastAlarmsAsFired();
                Logger.Info($"Alarms updated: {_alarms.Count} alarm(s) loaded");
            }
        }

        public AlarmEntry GetNextAlarm()
        {
            lock (_lock)
            {
                string now = DateTime.Now.ToString("HH:mm");
                return _alarms
                    .Where(a => a.Enabled
                        && string.Compare(a.Time, now, StringComparison.Ordinal) > 0
                        && !_firedTodaySet.Contains(a.Id))
                    .OrderBy(a => a.Time)
                    .FirstOrDefault();
            }
        }

        public void ClearFiredForSleepWindow(DateTime sleepTime, DateTime wakeTime)
        {
            lock (_lock)
            {
                // On wake, re-evaluate: if the clock rolled past midnight during sleep,
                // do a midnight reset. Otherwise, missed alarms stay fired (spec: don't re-fire).
                if (sleepTime.Date != wakeTime.Date)
                {
                    _firedTodaySet.Clear();
                    _lastDate = wakeTime.Date;
                    MarkPastAlarmsAsFired();
                    Logger.Info("Cross-midnight wake: reset fired set for new day");
                }
                else
                {
                    Logger.Info($"Wake detected. Missed alarms kept as fired.");
                }
            }
        }

        private void OnTick(object state)
        {
            if (_disposed) return;

            bool doMidnightReset = false;
            var alarmsToFire = new List<AlarmEntry>();

            lock (_lock)
            {
                DateTime now = DateTime.Now;

                // Midnight reset
                if (now.Date != _lastDate)
                {
                    _firedTodaySet.Clear();
                    _lastDate = now.Date;
                    doMidnightReset = true;
                    Logger.Info("Midnight reset: cleared fired alarms");
                }

                if (_isMuted)
                {
                    _lastCheckedMinute = now.ToString("HH:mm");
                    return;
                }

                string currentTime = now.ToString("HH:mm");

                // Use <= comparison with fired-set guard instead of exact equality.
                // This ensures alarms are never missed if the timer tick is delayed
                // (e.g., system load, GC pause, thread starvation).
                // The _firedTodaySet prevents any alarm from firing more than once.
                foreach (var alarm in _alarms)
                {
                    if (alarm.Enabled
                        && string.Compare(alarm.Time, currentTime, StringComparison.Ordinal) <= 0
                        && !_firedTodaySet.Contains(alarm.Id))
                    {
                        _firedTodaySet.Add(alarm.Id);
                        Logger.Info($"Alarm fired: {alarm.Time} - {alarm.Label} ({alarm.AudioKey})");
                        alarmsToFire.Add(alarm);
                    }
                }

                _lastCheckedMinute = currentTime;
            }

            // Raise events OUTSIDE the lock to prevent deadlock with Control.Invoke()
            if (doMidnightReset)
            {
                try { MidnightReset?.Invoke(this, EventArgs.Empty); }
                catch (Exception ex) { Logger.Error("Error in MidnightReset handler", ex); }
            }

            foreach (var alarm in alarmsToFire)
            {
                try
                {
                    AlarmFired?.Invoke(this, new AlarmFiredEventArgs(alarm));
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in AlarmFired handler", ex);
                }
            }
        }

        private void MarkPastAlarmsAsFired()
        {
            // Must be called while holding _lock
            string now = DateTime.Now.ToString("HH:mm");
            foreach (var alarm in _alarms)
            {
                if (alarm.Enabled
                    && string.Compare(alarm.Time, now, StringComparison.Ordinal) <= 0)
                {
                    _firedTodaySet.Add(alarm.Id);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer?.Dispose();
            _timer = null;
            Logger.Info("AlarmEngine disposed");
        }
    }
}
