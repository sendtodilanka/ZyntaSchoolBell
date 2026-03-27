using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using ZyntaSchoolBell.Models;

namespace ZyntaSchoolBell.Services
{
    public interface IAudioPlayer : IDisposable
    {
        void Play(string audioKey);
        void CancelCurrent();
        void SetVolume(int percent);
        bool IsPlaying { get; }

        event EventHandler PlaybackFinished;
        event EventHandler<string> PlaybackError;
    }

    public class AudioPlayer : IAudioPlayer
    {
        private readonly string _audioBasePath;
        private readonly object _lock = new object();
        private static readonly string[] Languages = { "si", "en", "ta" };

        private WaveOutEvent _waveOut;
        private AudioFileReader _reader;
        private Queue<string> _playQueue;
        private float _volume = 0.9f;
        private bool _disposed;

        public bool IsPlaying { get; private set; }

        public event EventHandler PlaybackFinished;
        public event EventHandler<string> PlaybackError;

        public AudioPlayer(string audioBasePath)
        {
            _audioBasePath = audioBasePath;
        }

        public void Play(string audioKey)
        {
            // Validate audioKey against allowlist to prevent path traversal
            if (!AudioKeys.All.Contains(audioKey))
            {
                Logger.Warn($"Rejected invalid audio key: {audioKey}");
                return;
            }

            lock (_lock)
            {
                CancelCurrentInternal();

                _playQueue = new Queue<string>();
                foreach (string lang in Languages)
                {
                    string filePath = Path.Combine(_audioBasePath, audioKey, lang + ".mp3");
                    _playQueue.Enqueue(filePath);
                }

                IsPlaying = true;
                PlayNext();
            }
        }

        public void CancelCurrent()
        {
            lock (_lock)
            {
                CancelCurrentInternal();
            }
        }

        public void SetVolume(int percent)
        {
            _volume = Math.Max(0f, Math.Min(1f, percent / 100f));
            lock (_lock)
            {
                if (_reader != null)
                {
                    try { _reader.Volume = _volume; }
                    catch { /* ignore if disposed */ }
                }
            }
        }

        private void PlayNext()
        {
            while (_playQueue != null && _playQueue.Count > 0)
            {
                string filePath = _playQueue.Dequeue();

                if (!File.Exists(filePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    string msg = $"Audio file not found: {filePath}";
                    Logger.Warn(msg);
                    PlaybackError?.Invoke(this, $"Missing audio: {fileName}");
                    continue;
                }

                try
                {
                    _reader = new AudioFileReader(filePath);
                    _reader.Volume = _volume;

                    _waveOut = new WaveOutEvent();
                    _waveOut.PlaybackStopped += OnPlaybackStopped;
                    _waveOut.Init(_reader);
                    _waveOut.Play();
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to play {filePath}", ex);
                    PlaybackError?.Invoke(this, $"Playback error: {Path.GetFileName(filePath)}");
                    DisposeCurrentPlayback();
                }
            }

            IsPlaying = false;
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            bool shouldContinue;
            lock (_lock)
            {
                if (_disposed) return;

                DisposeCurrentPlayback();
                shouldContinue = true;

                if (e.Exception != null)
                {
                    Logger.Error("Playback stopped with error", e.Exception);
                }
            }

            // Raise error event outside lock
            if (e.Exception != null)
            {
                PlaybackError?.Invoke(this, $"Audio device error: {e.Exception.Message}");
            }

            // Continue chain outside lock to prevent deadlock if subscribers call Play/Cancel
            if (shouldContinue)
            {
                PlayNext();
            }
        }

        private void CancelCurrentInternal()
        {
            _playQueue = null;
            IsPlaying = false;

            DisposeCurrentPlayback();
        }

        private void DisposeCurrentPlayback()
        {
            try
            {
                if (_waveOut != null)
                {
                    _waveOut.PlaybackStopped -= OnPlaybackStopped;
                    _waveOut.Stop();
                    _waveOut.Dispose();
                    _waveOut = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error disposing WaveOutEvent", ex);
            }

            try
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error disposing AudioFileReader", ex);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                CancelCurrentInternal();
            }
        }
    }
}
