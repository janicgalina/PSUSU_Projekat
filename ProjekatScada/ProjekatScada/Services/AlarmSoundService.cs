using System;
using System.Windows.Media;
using System.Windows.Threading;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Services
{
    public class AlarmSoundService : IAlarmSoundService
    {
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly DispatcherTimer _previewTimer;
        private AlarmSoundProfile _selectedProfile = AlarmSoundProfile.ClassicBeep;
        private string _currentFilePath;
        private bool _isPlaying;
        private bool _isPreviewing;
        private double _volume = 0.7;

        public AlarmSoundService()
        {
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _previewTimer.Tick += PreviewTimer_Tick;
            LoadProfileFile(_selectedProfile);
        }

        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = Math.Max(0, Math.Min(1, value));
                _mediaPlayer.Volume = _volume;
            }
        }

        public AlarmSoundProfile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (_selectedProfile == value)
                {
                    return;
                }

                _selectedProfile = value;
                LoadProfileFile(value);

                if (_isPlaying)
                {
                    RestartPlayback();
                }
            }
        }

        public void Start()
        {
            _isPreviewing = false;
            _previewTimer.Stop();

            if (_isPlaying)
            {
                return;
            }

            RestartPlayback();
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPreviewing = false;
            _previewTimer.Stop();
            _mediaPlayer.Stop();
            _isPlaying = false;
        }

        public void Preview()
        {
            Stop();
            _isPreviewing = true;
            RestartPlayback();
            _previewTimer.Start();
        }

        private void RestartPlayback()
        {
            _mediaPlayer.Open(new Uri(_currentFilePath, UriKind.Absolute));
            _mediaPlayer.Volume = _volume;
            _mediaPlayer.Play();
        }

        private void LoadProfileFile(AlarmSoundProfile profile)
        {
            _currentFilePath = AlarmToneGenerator.WriteTempWav(profile);
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (!_isPlaying && !_isPreviewing)
            {
                return;
            }

            _mediaPlayer.Position = TimeSpan.Zero;
            _mediaPlayer.Play();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            _previewTimer.Stop();
            _isPreviewing = false;
            _mediaPlayer.Stop();
        }
    }
}
