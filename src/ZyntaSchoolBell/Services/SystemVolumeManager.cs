using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace ZyntaSchoolBell.Services
{
    /// <summary>
    /// Ensures the Windows system master volume is at the desired level before playing an alarm.
    /// Uses the Windows Core Audio API (Vista+) via NAudio.
    /// </summary>
    public static class SystemVolumeManager
    {
        /// <summary>
        /// Ensures system volume is at least at the desired percentage.
        /// Also unmutes the system if muted. Does not lower volume if it's already higher.
        /// </summary>
        /// <param name="desiredPercent">Desired minimum volume (0-100)</param>
        public static void EnsureVolume(int desiredPercent)
        {
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    MMDevice device;
                    try
                    {
                        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    }
                    catch (COMException)
                    {
                        // No audio device available (headless server, disconnected speakers)
                        Logger.Warn("SystemVolumeManager: no audio output device found");
                        return;
                    }

                    var volume = device.AudioEndpointVolume;

                    // Unmute system if muted
                    if (volume.Mute)
                    {
                        volume.Mute = false;
                        Logger.Info("System audio was muted — unmuted for alarm playback");
                    }

                    // Check and raise volume if below desired level
                    float currentScalar = volume.MasterVolumeLevelScalar; // 0.0 to 1.0
                    int currentPercent = (int)(currentScalar * 100f);
                    float desiredScalar = Math.Max(0f, Math.Min(1f, desiredPercent / 100f));

                    if (currentScalar < desiredScalar)
                    {
                        volume.MasterVolumeLevelScalar = desiredScalar;
                        Logger.Info($"System volume raised from {currentPercent}% to {desiredPercent}% for alarm");
                    }
                }
            }
            catch (Exception ex)
            {
                // Volume check must never prevent the alarm from firing
                Logger.Error("SystemVolumeManager: failed to check/set volume", ex);
            }
        }
    }
}
