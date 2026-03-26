using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZyntaSchoolBell.Models;

namespace ZyntaSchoolBell.UI
{
    public class AddEditAlarmDialog : Form
    {
        private DateTimePicker _timePicker;
        private TextBox _labelText;
        private ComboBox _audioKeyCombo;
        private Button _testButton;
        private Button _saveButton;
        private Button _cancelButton;

        private readonly List<AlarmEntry> _existingAlarms;
        private readonly string _editingId;

        public AlarmEntry Result { get; private set; }

        public event EventHandler<string> TestAudioRequested;

        public AddEditAlarmDialog(AlarmEntry existing, List<AlarmEntry> existingAlarms)
        {
            _existingAlarms = existingAlarms ?? new List<AlarmEntry>();
            _editingId = existing?.Id;

            Text = existing == null ? "Add Bell" : "Edit Bell";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 280);
            AcceptButton = null;
            CancelButton = null;

            var timeLabel = new Label { Text = "Time:", Location = new Point(20, 22), AutoSize = true };
            _timePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Location = new Point(120, 20),
                Size = new Size(240, 25)
            };

            var nameLabel = new Label { Text = "Bell Name:", Location = new Point(20, 57), AutoSize = true };
            _labelText = new TextBox
            {
                Location = new Point(120, 55),
                Size = new Size(240, 25)
            };

            var audioLabel = new Label { Text = "Audio Type:", Location = new Point(20, 92), AutoSize = true };
            _audioKeyCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 90),
                Size = new Size(240, 25)
            };

            foreach (var kvp in AudioKeys.FriendlyNames)
            {
                _audioKeyCombo.Items.Add(new AudioKeyItem(kvp.Key, kvp.Value));
            }
            _audioKeyCombo.SelectedIndex = 0;

            _testButton = new Button
            {
                Text = "Test Audio",
                Location = new Point(120, 130),
                Size = new Size(100, 30)
            };
            _testButton.Click += (s, e) =>
            {
                var selected = _audioKeyCombo.SelectedItem as AudioKeyItem;
                if (selected != null)
                    TestAudioRequested?.Invoke(this, selected.Key);
            };

            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(175, 190),
                Size = new Size(85, 35),
                DialogResult = DialogResult.None
            };
            _saveButton.Click += OnSaveClick;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(275, 190),
                Size = new Size(85, 35),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                timeLabel, _timePicker,
                nameLabel, _labelText,
                audioLabel, _audioKeyCombo,
                _testButton,
                _saveButton, _cancelButton
            });

            if (existing != null)
            {
                if (TimeSpan.TryParse(existing.Time, out var ts))
                    _timePicker.Value = DateTime.Today.Add(ts);
                _labelText.Text = existing.Label;

                for (int i = 0; i < _audioKeyCombo.Items.Count; i++)
                {
                    if (((AudioKeyItem)_audioKeyCombo.Items[i]).Key == existing.AudioKey)
                    {
                        _audioKeyCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_labelText.Text))
            {
                MessageBox.Show(this, "Please enter a bell name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string time = _timePicker.Value.ToString("HH:mm");
            var selectedAudio = (AudioKeyItem)_audioKeyCombo.SelectedItem;

            // Check for duplicate time
            var duplicate = _existingAlarms.FirstOrDefault(a =>
                a.Time == time && a.Id != _editingId);

            if (duplicate != null)
            {
                var result = MessageBox.Show(this,
                    $"An alarm already exists at {time} (\"{duplicate.Label}\").\nReplace it?",
                    "Duplicate Time",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            Result = new AlarmEntry
            {
                Id = _editingId ?? Guid.NewGuid().ToString(),
                Time = time,
                Label = _labelText.Text.Trim(),
                AudioKey = selectedAudio.Key,
                Enabled = true
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private class AudioKeyItem
        {
            public string Key { get; }
            public string DisplayName { get; }

            public AudioKeyItem(string key, string displayName)
            {
                Key = key;
                DisplayName = displayName;
            }

            public override string ToString() => DisplayName;
        }
    }
}
