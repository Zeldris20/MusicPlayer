
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using TagLib;
using System.Timers;
using System.Windows.Threading;
using NAudio.Wave;
using System.Windows.Controls.Primitives;
using System.Diagnostics.Eventing.Reader;
using Microsoft.VisualBasic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Data.Common;

namespace Music_Player
{

    public partial class MainWindow : Window

    {
        private IWavePlayer? wavePlayer;
        private AudioFileReader? audioFileReader;
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private bool isSongEndedPromptShown = false; // Declare the variable here
        private string? selectedFilePath;
        private List<string> playlist = new List<string>();
        private int currentSongIndex = -1; // Index of the currently playing song 
        public MainWindow()
        {
            InitializeComponent();



            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            TimerSlider.PreviewMouseLeftButtonUp += TimerSlider_PreviewMouseLeftButtonUp;
            InitializeAudioComponents();

            // Attach the event handler for PreviewKeyDown
            PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.Space)
                {
                    TogglePlayback();
                }
            };

            PreviewKeyDown += MainWindow_PreviewKeyDown;


           

        }

        private void TimerSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;

            if (wavePlayer != null)
            {
                wavePlayer.Play();
            }
        }


        private void TimerSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;

            if (wavePlayer != null)
            {
                wavePlayer.Play();
            }
        }
        private void UpdateSongMetaDataUI(string filePath)
        {
            var tagLibFile = TagLib.File.Create(filePath);
            string songName = tagLibFile.Tag.Title;
            string artistName = tagLibFile.Tag.FirstPerformer;
            TimeSpan duration = tagLibFile.Properties.Duration;

            // Update UI elements with metadata
            lblSongname.Text = songName;
            lblArtistname.Text = artistName;

            // Format and display song duration 
            string formattedDuration = $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";

            lblMusiclength.Text = formattedDuration;
        }
        private void PlayNextSong()
        {
            isSongEndedPromptShown = false;
            currentSongIndex = (currentSongIndex + 1) % playlist.Count;
                PlaySong(currentSongIndex);
            
            if (wavePlayer != null && audioFileReader != null)
            {
                wavePlayer.Pause();
            }

        }
       
            private void TogglePlayback()
        {
            if (wavePlayer != null && audioFileReader != null)
            {
                if (!isPlaying)
                {
                    wavePlayer.Play();
                    timer.Start();
                    isPlaying = true;
                }
                else
                {
                    wavePlayer.Pause();
                    timer.Stop();
                    isPlaying = false;
                }
            }
        }
        private void UpdatePlaylistUI()
        {
            lstPlaylist.ItemsSource = playlist.Select(filePath => System.IO.Path.GetFileNameWithoutExtension(filePath));
        }
        private void AddToPlaylist(string filepath)
        {
            playlist.Add(filepath);
            UpdatePlaylistUI();

            if (currentSongIndex == -1)
            {
                currentSongIndex = 0;
                PlaySong(currentSongIndex);
            }
        }




        private AudioFileReader? GetAudioFileReader()
        {
            return audioFileReader;
        }

        private void InitializeAudioComponents()
        {

            CleanupAudio();

            wavePlayer = new WaveOut();
            if (!string.IsNullOrEmpty(selectedFilePath)) // select the path of the mp3
            {
                audioFileReader = new AudioFileReader(selectedFilePath);
                wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                wavePlayer.Init(audioFileReader);
            }

        }



        private void CleanupAudio()

        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
                wavePlayer.Dispose();
                wavePlayer = null;

            }

            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                audioFileReader = null;


            }
        }


        private void WavePlayer_PlaybackStopped(object sender, EventArgs e)
        {
            CleanupAudio();
        }


        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemMinus)
            {
                AdjustVolume(-0.05);
            }
            else if (e.Key == Key.OemPlus)
            {
                AdjustVolume(0.05);
            }

        }

        private void AdjustVolume(double change)
        {
            if (wavePlayer != null && audioFileReader != null)
            {
                float newVolume = audioFileReader.Volume + (float)change;

                // Ensure the volume is within the valid range of 0 to 1 
                newVolume = Math.Max(0, Math.Min(1, newVolume));

                // Set the new volume for both wavePlayer and audioFileReader
                wavePlayer.Volume = newVolume;
                audioFileReader.Volume = newVolume;
            }
        }
        private void btnFile_Click(object sender, RoutedEventArgs e)

        {

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*";
            openFileDialog.Multiselect = true; // allows you to select multiple files 



            if (openFileDialog.ShowDialog() == true)
            {


                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (!playlist.Contains(filePath))
                    {
                        AddToPlaylist(filePath);
                    }

                }
                
            }
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (isPlaying && audioFileReader != null && sender != null)
            {
                // Get current playback position and song duration from audio file reader
                TimeSpan currentPosition = audioFileReader.CurrentTime;
                TimeSpan songDuration = audioFileReader.TotalTime;

                // Update the TimerSlider position
                TimerSlider.Value = (currentPosition.TotalSeconds / songDuration.TotalSeconds) * TimerSlider.Maximum;

                lblCurrenttime.Text = $"{(int)currentPosition.TotalMinutes:D2}:{currentPosition.Seconds:D2}";
                // Check if the song has finished playing
                if (currentPosition >= songDuration)
                {
                    timer.Stop();
                    isPlaying = false;
                }
            }
        }

        private void Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }


        private void btnPlay_Click(object sender, RoutedEventArgs e)

        {



            if (playlist.Count == 0)
            {
                MessageBox.Show("Please select a song from the playlist");
                return;
            }

            if (currentSongIndex < 0 || currentSongIndex >= playlist.Count)
            {
                currentSongIndex = 0;
                PlaySong(currentSongIndex);
            }




            else
            {
                TogglePlayback();

            }

            if (wavePlayer != null && audioFileReader != null)
            {
                if (!isPlaying)
                {
                    wavePlayer.Pause();
                    timer.Stop();

                }
                else if (audioFileReader.CurrentTime == audioFileReader.TotalTime)
                {


                    audioFileReader.CurrentTime = TimeSpan.Zero;

                }



                isPlaying = !isPlaying;

                UpdateSongMetaDataUI(playlist[currentSongIndex]);
            }
            else
            {
                MessageBox.Show("Please select an MP3");
            }

        }



        private TimeSpan GetAudioPlaybackPosition()
        {
            if (wavePlayer != null && audioFileReader != null)
            {
                return audioFileReader.CurrentTime;
            }
            return TimeSpan.Zero;
        }
        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            if (currentSongIndex >= 0 && currentSongIndex < playlist.Count)
            {
                PlaySong(currentSongIndex);
            }
            currentSongIndex = (currentSongIndex + 1) % playlist.Count;
            PlaySong(currentSongIndex);

            if (wavePlayer != null && audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.Zero;
                wavePlayer.Play();
                isPlaying = true;
                timer.Start();
            }


        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            PlayNextSong();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private bool isDragging = false;
        private void TimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFileReader != null)
            {
                double position = (TimerSlider.Value / TimerSlider.Maximum) * audioFileReader.TotalTime.TotalSeconds;

                audioFileReader.CurrentTime = TimeSpan.FromSeconds(position);

                lblCurrenttime.Text = $"{(int)audioFileReader.CurrentTime.TotalMinutes:D2}:{audioFileReader.CurrentTime.Seconds:D2}";

                if (isDragging && wavePlayer != null && !wavePlayer.PlaybackState.Equals(PlaybackState.Playing))
                {
                    wavePlayer.Play();
                }

            }
        }
        private bool isSongEndedPromptPending = false;

        private void WavePlayer_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (!isDragging && currentSongIndex >= 0 && currentSongIndex < playlist.Count)
            {

                if (!isSongEndedPromptPending)
                {
                    isSongEndedPromptPending = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBoxResult result = MessageBox.Show("The song ended. Do you want to select another song?", "Song Finished", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            PlayNextSong();
                        }
                        isSongEndedPromptPending = false;
                    }));


                    {
                        PlayNextSong();
                    }
                    isSongEndedPromptShown = true;

                   
                    }
                    else
                    {
                        PlayNextSong();
                    }

                }

            }
          
        private void PlaySong(int songIndex)
        {
            if (songIndex >= 0 && songIndex < playlist.Count)
            {
                if (wavePlayer != null)
                {
                    if (audioFileReader != null)
                    {
                        audioFileReader.Dispose();
                        audioFileReader = null;
                    }

                    audioFileReader = new AudioFileReader(playlist[songIndex]);
                    wavePlayer.Init(audioFileReader);
                    wavePlayer.Play();

                    isPlaying = true;
                    UpdatePlaylistUI();
                    UpdateSongMetaDataUI(playlist[songIndex]);

                    timer.Start();

                }
            }
         }
       


        private void lstPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        
        }
    }


        
        


         
           