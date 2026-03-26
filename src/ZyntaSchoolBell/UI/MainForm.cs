using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZyntaSchoolBell.Models;
using ZyntaSchoolBell.Services;

namespace ZyntaSchoolBell.UI
{
    public class MainForm : Form
    {
        // Services
        private readonly IProfileService _profileService;
        private readonly IAudioPlayer _audioPlayer;
        private readonly AlarmEngine _alarmEngine;
        private readonly SleepWakeHandler _sleepWakeHandler;
        private readonly WakeScheduler _wakeScheduler;
        private readonly TrayManager _trayManager;

        // State
        private AppSettings _settings;
        private List<Profile> _profiles;
        private Profile _activeProfile;

        // UI Controls
        private ComboBox _profileCombo;
        private Button _newProfileBtn;
        private Button _renameProfileBtn;
        private Button _deleteProfileBtn;
        private Label _statusIndicator;
        private Label _statusText;
        private Label _nextBellLabel;
        private DataGridView _alarmGrid;
        private TrackBar _volumeSlider;
        private Label _volumeLabel;
        private CheckBox _muteCheckbox;
        private Button _addAlarmBtn;
        private System.Windows.Forms.Timer _uiTimer;
        private System.Windows.Forms.Timer _volumeSaveTimer;

        private bool _realExit;

        public MainForm()
        {
            string audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio");
            _profileService = new ProfileService();
            _audioPlayer = new AudioPlayer(audioPath);
            _alarmEngine = new AlarmEngine();
            _sleepWakeHandler = new SleepWakeHandler(_alarmEngine);
            _wakeScheduler = new WakeScheduler(_alarmEngine);
            _trayManager = new TrayManager();

            InitializeUI();
            WireEvents();
            LoadData();

            _alarmEngine.Start();
        }

        private void InitializeUI()
        {
            Text = "ZyntaSchoolBell";
            Size = new Size(700, 520);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;

            // === Top toolbar ===
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8, 8, 8, 4) };

            var scheduleLabel = new Label { Text = "Schedule:", AutoSize = true, Location = new Point(10, 14) };
            _profileCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(80, 10),
                Size = new Size(200, 25)
            };
            _newProfileBtn = new Button { Text = "New", Location = new Point(295, 9), Size = new Size(60, 27) };
            _renameProfileBtn = new Button { Text = "Rename", Location = new Point(360, 9), Size = new Size(70, 27) };
            _deleteProfileBtn = new Button { Text = "Delete", Location = new Point(435, 9), Size = new Size(65, 27) };

            topPanel.Controls.AddRange(new Control[] { scheduleLabel, _profileCombo, _newProfileBtn, _renameProfileBtn, _deleteProfileBtn });

            // === Status bar ===
            var statusPanel = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(8, 4, 8, 4) };

            _statusIndicator = new Label
            {
                Text = "\u25CF",
                ForeColor = Color.Green,
                Font = new Font(Font.FontFamily, 12f),
                AutoSize = true,
                Location = new Point(10, 3)
            };
            _statusText = new Label { Text = "ACTIVE", AutoSize = true, Location = new Point(30, 7), Font = new Font(Font.FontFamily, 9f, FontStyle.Bold) };
            _nextBellLabel = new Label { Text = "Next bell: --", AutoSize = true, Location = new Point(120, 7) };

            statusPanel.Controls.AddRange(new Control[] { _statusIndicator, _statusText, _nextBellLabel });

            // === Alarm grid ===
            _alarmGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            _alarmGrid.Columns.Add("Time", "Time");
            _alarmGrid.Columns.Add("Label", "Bell Name");
            _alarmGrid.Columns.Add("AudioKey", "Audio");

            var testCol = new DataGridViewButtonColumn { Name = "Test", HeaderText = "", Text = "Test", UseColumnDefaultCellStyle = true, Width = 50 };
            testCol.FlatStyle = FlatStyle.Flat;
            _alarmGrid.Columns.Add(testCol);

            var editCol = new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "Edit", UseColumnDefaultCellStyle = true, Width = 50 };
            editCol.FlatStyle = FlatStyle.Flat;
            _alarmGrid.Columns.Add(editCol);

            var deleteCol = new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Del", UseColumnDefaultCellStyle = true, Width = 50 };
            deleteCol.FlatStyle = FlatStyle.Flat;
            _alarmGrid.Columns.Add(deleteCol);

            _alarmGrid.Columns["Time"].FillWeight = 15;
            _alarmGrid.Columns["Label"].FillWeight = 30;
            _alarmGrid.Columns["AudioKey"].FillWeight = 20;

            // === Bottom bar ===
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(8, 8, 8, 8) };

            var volLabel = new Label { Text = "Volume:", AutoSize = true, Location = new Point(10, 17) };
            _volumeSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10,
                Value = 90,
                Location = new Point(70, 8),
                Size = new Size(200, 35)
            };
            _volumeLabel = new Label { Text = "90%", AutoSize = true, Location = new Point(275, 17) };
            _muteCheckbox = new CheckBox { Text = "Mute Today", AutoSize = true, Location = new Point(340, 15) };
            _addAlarmBtn = new Button { Text = "+ Add Bell", Location = new Point(560, 10), Size = new Size(100, 30) };

            bottomPanel.Controls.AddRange(new Control[] { volLabel, _volumeSlider, _volumeLabel, _muteCheckbox, _addAlarmBtn });

            // === Layout order matters ===
            Controls.Add(_alarmGrid);
            Controls.Add(statusPanel);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);

            // === UI Timer for status updates ===
            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            // Keyboard shortcuts
            KeyPreview = true;
        }

        private void WireEvents()
        {
            _profileCombo.SelectedIndexChanged += OnProfileChanged;
            _newProfileBtn.Click += OnNewProfile;
            _renameProfileBtn.Click += OnRenameProfile;
            _deleteProfileBtn.Click += OnDeleteProfile;
            _volumeSlider.ValueChanged += OnVolumeChanged;
            _muteCheckbox.CheckedChanged += OnMuteChanged;
            _addAlarmBtn.Click += OnAddAlarm;
            _alarmGrid.CellContentClick += OnGridCellClick;

            _alarmEngine.AlarmFired += OnAlarmFired;
            _alarmEngine.MidnightReset += OnMidnightReset;

            _audioPlayer.PlaybackError += OnPlaybackError;

            _trayManager.OpenWindowRequested += (s, e) => ShowFromTray();
            _trayManager.ExitRequested += (s, e) => RealExit();
            _trayManager.MuteToggled += (s, muted) =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => { _muteCheckbox.Checked = muted; }));
                else
                    _muteCheckbox.Checked = muted;
            };

            _sleepWakeHandler.SystemResumed += (s, e) =>
            {
                Logger.Info("System resumed — UI notified");
                _wakeScheduler.Recalculate();
            };

            KeyDown += OnKeyDown;
            FormClosing += OnFormClosing;
        }

        private void LoadData()
        {
            _settings = _profileService.LoadSettings();
            _profiles = _profileService.LoadAllProfiles();

            // Auto-reset mute if date changed
            if (_settings.MutedToday && _settings.MutedDate != DateTime.Today.ToString("yyyy-MM-dd"))
            {
                _settings.MutedToday = false;
                _settings.MutedDate = null;
                _profileService.SaveSettings(_settings);
            }

            RefreshProfileCombo();
            _volumeSlider.Value = Math.Max(0, Math.Min(100, _settings.Volume));
            _muteCheckbox.Checked = _settings.MutedToday;
            _audioPlayer.SetVolume(_settings.Volume);
            _alarmEngine.IsMuted = _settings.MutedToday;
        }

        private void RefreshProfileCombo()
        {
            _profileCombo.SelectedIndexChanged -= OnProfileChanged;
            _profileCombo.Items.Clear();

            foreach (var p in _profiles)
            {
                _profileCombo.Items.Add(new ProfileItem(p));
            }

            // Select active profile
            int idx = -1;
            for (int i = 0; i < _profileCombo.Items.Count; i++)
            {
                if (((ProfileItem)_profileCombo.Items[i]).Profile.Id == _settings.ActiveProfileId)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
                _profileCombo.SelectedIndex = idx;
            else if (_profileCombo.Items.Count > 0)
                _profileCombo.SelectedIndex = 0;

            _profileCombo.SelectedIndexChanged += OnProfileChanged;
            OnProfileChanged(null, EventArgs.Empty);
        }

        private void RefreshAlarmGrid()
        {
            _alarmGrid.Rows.Clear();
            if (_activeProfile == null) return;

            foreach (var alarm in _activeProfile.Alarms)
            {
                string friendlyAudio = AudioKeys.FriendlyNames.ContainsKey(alarm.AudioKey)
                    ? AudioKeys.FriendlyNames[alarm.AudioKey]
                    : alarm.AudioKey;

                string time12 = FormatTime12(alarm.Time);
                int rowIdx = _alarmGrid.Rows.Add(time12, alarm.Label, friendlyAudio, "Test", "Edit", "Del");
                _alarmGrid.Rows[rowIdx].Tag = alarm;
            }
        }

        // === Event Handlers ===

        private void OnProfileChanged(object sender, EventArgs e)
        {
            var item = _profileCombo.SelectedItem as ProfileItem;
            _activeProfile = item?.Profile;

            if (_activeProfile != null)
            {
                _settings.ActiveProfileId = _activeProfile.Id;
                _profileService.SaveSettings(_settings);
                _alarmEngine.UpdateAlarms(_activeProfile.Alarms);
                _wakeScheduler.Recalculate();
                _trayManager.UpdateScheduleName(_activeProfile.Name);
            }

            RefreshAlarmGrid();
            UpdateStatus();
        }

        private void OnNewProfile(object sender, EventArgs e)
        {
            string name = PromptInput("New Schedule", "Enter schedule name:");
            if (string.IsNullOrWhiteSpace(name)) return;

            // Limit length and strip control characters
            name = name.Trim();
            if (name.Length > 50) name = name.Substring(0, 50);

            string id = new string(name.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            if (_profiles.Any(p => p.Id == id))
            {
                MessageBox.Show(this, "A schedule with that name already exists.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = new Profile { Id = id, Name = name };
            _profileService.SaveProfile(profile);
            _profiles = _profileService.LoadAllProfiles();
            _settings.ActiveProfileId = id;
            RefreshProfileCombo();
        }

        private void OnRenameProfile(object sender, EventArgs e)
        {
            if (_activeProfile == null) return;

            string name = PromptInput("Rename Schedule", "Enter new name:", _activeProfile.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            _activeProfile.Name = name;
            _profileService.SaveProfile(_activeProfile);
            _profiles = _profileService.LoadAllProfiles();
            RefreshProfileCombo();
        }

        private void OnDeleteProfile(object sender, EventArgs e)
        {
            if (_activeProfile == null) return;

            if (_profiles.Count <= 1)
            {
                MessageBox.Show(this, "Cannot delete the last schedule.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(this,
                $"Delete schedule \"{_activeProfile.Name}\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            _profileService.DeleteProfile(_activeProfile.Id);
            _profiles = _profileService.LoadAllProfiles();
            _settings.ActiveProfileId = _profiles.FirstOrDefault()?.Id;
            RefreshProfileCombo();
        }

        private void OnVolumeChanged(object sender, EventArgs e)
        {
            int vol = _volumeSlider.Value;
            _volumeLabel.Text = $"{vol}%";
            _audioPlayer.SetVolume(vol);
            _settings.Volume = vol;

            // Debounce: save settings 500ms after last change instead of on every tick
            if (_volumeSaveTimer == null)
            {
                _volumeSaveTimer = new System.Windows.Forms.Timer { Interval = 500 };
                _volumeSaveTimer.Tick += (s, ev) =>
                {
                    _volumeSaveTimer.Stop();
                    _profileService.SaveSettings(_settings);
                };
            }
            _volumeSaveTimer.Stop();
            _volumeSaveTimer.Start();
        }

        private void OnMuteChanged(object sender, EventArgs e)
        {
            bool muted = _muteCheckbox.Checked;
            _settings.MutedToday = muted;
            _settings.MutedDate = muted ? DateTime.Today.ToString("yyyy-MM-dd") : null;
            _alarmEngine.IsMuted = muted;
            _profileService.SaveSettings(_settings);
            _trayManager.UpdateMuteState(muted);
            UpdateStatus();
        }

        private void OnAddAlarm(object sender, EventArgs e)
        {
            if (_activeProfile == null)
            {
                MessageBox.Show(this, "Please create or select a schedule first.", "No Schedule",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ShowAlarmDialog(null);
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var alarm = _alarmGrid.Rows[e.RowIndex].Tag as AlarmEntry;
            if (alarm == null) return;

            string colName = _alarmGrid.Columns[e.ColumnIndex].Name;

            switch (colName)
            {
                case "Test":
                    _audioPlayer.CancelCurrent();
                    _audioPlayer.Play(alarm.AudioKey);
                    break;
                case "Edit":
                    ShowAlarmDialog(alarm);
                    break;
                case "Delete":
                    var result = MessageBox.Show(this,
                        $"Delete alarm at {alarm.Time} ({alarm.Label})?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        _activeProfile.Alarms.RemoveAll(a => a.Id == alarm.Id);
                        SaveAndRefresh();
                    }
                    break;
            }
        }

        private void ShowAlarmDialog(AlarmEntry existing)
        {
            using (var dlg = new AddEditAlarmDialog(existing, _activeProfile.Alarms))
            {
                dlg.TestAudioRequested += (s, audioKey) =>
                {
                    _audioPlayer.CancelCurrent();
                    _audioPlayer.Play(audioKey);
                };

                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
                {
                    // Remove old alarm with same ID or same time (if replacing)
                    _activeProfile.Alarms.RemoveAll(a => a.Id == dlg.Result.Id);
                    _activeProfile.Alarms.RemoveAll(a => a.Time == dlg.Result.Time && a.Id != dlg.Result.Id);
                    _activeProfile.Alarms.Add(dlg.Result);
                    SaveAndRefresh();
                }
            }
        }

        private void SaveAndRefresh()
        {
            _profileService.SaveProfile(_activeProfile);
            _alarmEngine.UpdateAlarms(_activeProfile.Alarms);
            _wakeScheduler.Recalculate();
            RefreshAlarmGrid();
            UpdateStatus();
        }

        private void OnAlarmFired(object sender, AlarmFiredEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnAlarmFired(sender, e)));
                return;
            }

            // Ensure system volume is at the desired level before playing
            SystemVolumeManager.EnsureVolume(_settings.Volume);

            _audioPlayer.CancelCurrent();
            _audioPlayer.Play(e.Alarm.AudioKey);
            _trayManager.ShowBalloon("Bell", $"{e.Alarm.Label} — {FormatTime12(e.Alarm.Time)}");
            UpdateStatus();

            // Recalculate wake timer for the next alarm
            _wakeScheduler.Recalculate();
        }

        private void OnMidnightReset(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnMidnightReset(sender, e)));
                return;
            }

            // Auto-reset mute
            if (_settings.MutedToday)
            {
                _muteCheckbox.Checked = false;
            }
            UpdateStatus();
        }

        private void OnPlaybackError(object sender, string errorMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPlaybackError(sender, errorMsg)));
                return;
            }

            _trayManager.ShowBalloon("Audio Error", errorMsg, ToolTipIcon.Warning);
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            bool muted = _alarmEngine.IsMuted;
            _statusIndicator.ForeColor = muted ? Color.Red : Color.Green;
            _statusText.Text = muted ? "PAUSED" : "ACTIVE";

            var next = _alarmEngine.GetNextAlarm();
            if (next != null)
            {
                _nextBellLabel.Text = $"Next bell: {FormatTime12(next.Time)} — {next.Label}";
            }
            else
            {
                _nextBellLabel.Text = _activeProfile?.Alarms.Count == 0
                    ? "No alarms configured"
                    : "No more bells today";
            }

            _trayManager.UpdateNextBell(next);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N)
            {
                OnAddAlarm(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && _alarmGrid.SelectedRows.Count > 0)
            {
                var alarm = _alarmGrid.SelectedRows[0].Tag as AlarmEntry;
                if (alarm != null)
                {
                    OnGridCellClick(sender, new DataGridViewCellEventArgs(
                        _alarmGrid.Columns["Delete"].Index, _alarmGrid.SelectedRows[0].Index));
                }
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.M)
            {
                _muteCheckbox.Checked = !_muteCheckbox.Checked;
                e.Handled = true;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_realExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _trayManager.ShowBalloon("ZyntaSchoolBell", "Running in the background. Right-click tray icon to exit.");
            }
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void RealExit()
        {
            _realExit = true;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe events
                _alarmEngine.AlarmFired -= OnAlarmFired;
                _alarmEngine.MidnightReset -= OnMidnightReset;
                _audioPlayer.PlaybackError -= OnPlaybackError;

                _uiTimer?.Stop();
                _uiTimer?.Dispose();
                _volumeSaveTimer?.Stop();
                _volumeSaveTimer?.Dispose();
                _alarmEngine?.Dispose();
                _audioPlayer?.Dispose();
                _wakeScheduler?.Dispose();
                _sleepWakeHandler?.Dispose();
                _trayManager?.Dispose();
            }
            base.Dispose(disposing);
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

        private string PromptInput(string title, string prompt, string defaultValue = "")
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(350, 160);

                var lbl = new Label { Text = prompt, Location = new Point(15, 15), AutoSize = true };
                var txt = new TextBox { Text = defaultValue, Location = new Point(15, 40), Size = new Size(300, 25) };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(150, 80), Size = new Size(75, 30) };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(235, 80), Size = new Size(75, 30) };

                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });

                return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }

        private class ProfileItem
        {
            public Profile Profile { get; }
            public ProfileItem(Profile p) { Profile = p; }
            public override string ToString() => Profile.Name;
        }
    }
}
