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
        private bool _disposed;

        public event EventHandler<AlarmFiredEventArgs> AlarmFired;
        public event EventHandler MidnightReset;

        public bool IsMuted { get; set; }

        public void Start()
        {
            _lastDate = DateTime.Today;
            MarkPastAlarmsAsFired();
            _timer = new Timer(OnTick, null, 0, 1000);
            Logger.Info("AlarmEngine started");
        }

        public void UpdateAlarms(List<AlarmEntry> alarms)
        {
            lock (_lock)
            {
                _alarms = alarms ?? new List<AlarmEntry>();
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
                string sleepStr = sleepTime.ToString("HH:mm");
                string wakeStr = wakeTime.ToString("HH:mm");

                var toRemove = _alarms
                    .Where(a => a.Enabled
                        && string.Compare(a.Time, sleepStr, StringComparison.Ordinal) >= 0
                        && string.Compare(a.Time, wakeStr, StringComparison.Ordinal) <= 0
                        && _firedTodaySet.Contains(a.Id))
                    .Select(a => a.Id)
                    .ToList();

                // Actually we do NOT want to remove them — missed alarms should stay fired
                // so they don't re-fire. The spec says "do NOT fire any missed alarms".
                // So this method is intentionally a no-op for the firedTodaySet.
                Logger.Info($"Wake detected. {toRemove.Count} alarm(s) were in sleep window (kept as fired).");
            }
        }

        public void ReEvaluateFiredSet()
        {
            lock (_lock)
            {
                // On clock change, re-check: keep only alarms whose time is still past
                string now = DateTime.Now.ToString("HH:mm");
                var toRemove = new List<string>();

                foreach (var alarm in _alarms)
                {
                    if (_firedTodaySet.Contains(alarm.Id)
                        && string.Compare(alarm.Time, now, StringComparison.Ordinal) > 0)
                    {
                        // Alarm time is now in the future — it hasn't actually fired yet
                        toRemove.Add(alarm.Id);
                    }
                }

                foreach (string id in toRemove)
                {
                    _firedTodaySet.Remove(id);
                }

                if (toRemove.Count > 0)
                {
                    Logger.Info($"Clock change detected: {toRemove.Count} alarm(s) moved back to pending");
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

                if (IsMuted) return;

                string currentTime = now.ToString("HH:mm");

                foreach (var alarm in _alarms)
                {
                    if (alarm.Enabled
                        && alarm.Time == currentTime
                        && !_firedTodaySet.Contains(alarm.Id))
                    {
                        _firedTodaySet.Add(alarm.Id);
                        Logger.Info($"Alarm fired: {alarm.Time} - {alarm.Label} ({alarm.AudioKey})");
                        alarmsToFire.Add(alarm);
                    }
                }
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
