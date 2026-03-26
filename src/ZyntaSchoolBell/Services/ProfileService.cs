using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ZyntaSchoolBell.Models;

namespace ZyntaSchoolBell.Services
{
    public interface IProfileService
    {
        List<Profile> LoadAllProfiles();
        Profile LoadProfile(string id);
        void SaveProfile(Profile profile);
        void DeleteProfile(string id);
        AppSettings LoadSettings();
        void SaveSettings(AppSettings settings);
    }

    public class ProfileService : IProfileService
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private readonly string _profilesDir;
        private readonly string _settingsFile;

        public ProfileService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string baseDir = Path.Combine(appData, "ZyntaSchoolBell");
            _profilesDir = Path.Combine(baseDir, "profiles");
            _settingsFile = Path.Combine(baseDir, "settings.json");

            Directory.CreateDirectory(_profilesDir);
        }

        public List<Profile> LoadAllProfiles()
        {
            var profiles = new List<Profile>();
            if (!Directory.Exists(_profilesDir)) return profiles;

            foreach (string file in Directory.GetFiles(_profilesDir, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file, Utf8NoBom);
                    var profile = JsonConvert.DeserializeObject<Profile>(json);
                    if (profile != null)
                    {
                        profile.Alarms = profile.Alarms
                            .OrderBy(a => a.Time)
                            .ToList();
                        profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load profile from {file}", ex);
                }
            }

            return profiles.OrderBy(p => p.Name).ToList();
        }

        public Profile LoadProfile(string id)
        {
            string file = Path.Combine(_profilesDir, id + ".json");
            if (!File.Exists(file)) return null;

            try
            {
                string json = File.ReadAllText(file, Utf8NoBom);
                var profile = JsonConvert.DeserializeObject<Profile>(json);
                if (profile != null)
                {
                    profile.Alarms = profile.Alarms
                        .OrderBy(a => a.Time)
                        .ToList();
                }
                return profile;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load profile {id}", ex);
                return null;
            }
        }

        public void SaveProfile(Profile profile)
        {
            try
            {
                profile.Alarms = profile.Alarms
                    .OrderBy(a => a.Time)
                    .ToList();

                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                string file = Path.Combine(_profilesDir, profile.Id + ".json");
                AtomicWrite(file, json);
                Logger.Info($"Saved profile: {profile.Id} ({profile.Name})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save profile {profile.Id}", ex);
                throw;
            }
        }

        public void DeleteProfile(string id)
        {
            try
            {
                string file = Path.Combine(_profilesDir, id + ".json");
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Logger.Info($"Deleted profile: {id}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete profile {id}", ex);
                throw;
            }
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFile))
                    return new AppSettings();

                string json = File.ReadAllText(_settingsFile, Utf8NoBom);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings", ex);
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                AtomicWrite(_settingsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
                throw;
            }
        }

        private void AtomicWrite(string filePath, string content)
        {
            string dir = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(dir);

            string tmpFile = filePath + ".tmp";
            File.WriteAllText(tmpFile, content, Utf8NoBom);

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tmpFile, filePath);
        }
    }
}
