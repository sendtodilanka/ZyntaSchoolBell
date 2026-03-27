using System;
using System.Drawing;
using System.Windows.Forms;
using ZyntaSchoolBell.Models;
using ZyntaSchoolBell.Services;

namespace ZyntaSchoolBell.UI
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _scheduleLabel;
        private ToolStripMenuItem _nextBellLabel;
        private ToolStripMenuItem _muteItem;
        private bool _disposed;

        public event EventHandler OpenWindowRequested;
        public event EventHandler ExitRequested;
        public event EventHandler<bool> MuteToggled;

        public NotifyIcon NotifyIcon => _notifyIcon;

        public TrayManager()
        {
            _contextMenu = new ContextMenuStrip();

            _scheduleLabel = new ToolStripMenuItem("Active Schedule: (none)") { Enabled = false };
            _nextBellLabel = new ToolStripMenuItem("Next Bell: --") { Enabled = false };
            _muteItem = new ToolStripMenuItem("Mute Today");
            _muteItem.Click += (s, e) =>
            {
                _muteItem.Checked = !_muteItem.Checked;
                MuteToggled?.Invoke(this, _muteItem.Checked);
            };
            var openItem = new ToolStripMenuItem("Open Window");
            openItem.Click += (s, e) => OpenWindowRequested?.Invoke(this, EventArgs.Empty);
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

            _contextMenu.Items.Add(_scheduleLabel);
            _contextMenu.Items.Add(_nextBellLabel);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_muteItem);
            _contextMenu.Items.Add(openItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "ZyntaSchoolBell",
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => OpenWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SetIcon(Icon icon)
        {
            if (icon != null)
                _notifyIcon.Icon = icon;
        }

        public void UpdateScheduleName(string name)
        {
            _scheduleLabel.Text = $"Active Schedule: {name}";
        }

        public void UpdateNextBell(AlarmEntry alarm)
        {
            if (alarm == null)
            {
                _nextBellLabel.Text = "Next Bell: --";
                _notifyIcon.Text = "ZyntaSchoolBell";
            }
            else
            {
                string time12 = FormatTime12(alarm.Time);
                _nextBellLabel.Text = $"Next Bell: {time12} - {alarm.Label}";
                string tooltip = $"ZyntaSchoolBell — Next: {time12}";
                _notifyIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            }
        }

        public void UpdateMuteState(bool muted)
        {
            _muteItem.Checked = muted;
            if (muted)
            {
                _notifyIcon.Text = "ZyntaSchoolBell — Muted Today";
            }
        }

        public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        private static string FormatTime12(string time24)
        {
            if (TimeSpan.TryParse(time24, out var ts))
            {
                var dt = DateTime.Today.Add(ts);
                return dt.ToString("hh:mm tt");
            }
            return time24;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
        }
    }
}
