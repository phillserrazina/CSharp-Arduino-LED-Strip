using System;
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
using System.IO.Ports;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.Windows.Threading;

namespace Bassinator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // VARIABLES
        private readonly SerialPort serialPort = new SerialPort("COM3", 9600);
        private readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private readonly AudioManager audioManager;

        Color hFreqColor = Color.FromArgb(255, 255, 0,   0);
        Color mFreqColor = Color.FromArgb(255, 0,   255, 0);
        Color lFreqColor = Color.FromArgb(255, 0,   0,   255);

        List<Color> highFreqColorList = new List<Color>();
        List<Color> lowFreqColorList = new List<Color>();
        int gradientSize = 255;

        int currentMode = 0;

        // CONSTRUCTOR
        public MainWindow()
        {
            InitializeComponent();

            audioManager = new AudioManager(0);

            SetupDeviceButtons();
            SetupGradients();
            StartUpdateTick();

            App.OnExitEvent += (() =>
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                }
            });
        }

        // EXECUTION FUNCTIONS
        private void Timer_Tick(object sender, EventArgs e)
        {
            
            UpdateAudioManagerValues();
            
            // Initialize volume and pitch values
            int volume = (int)audioManager.VolumeAverage;
            volume = NonLinearVolume(volume);
            int[] pitches = new int[3];

            if (volume < VolumeThresholdSlider.Value) volume = 0;

            //int freq = (255 / 70) * audioManager.PredominantFFTIndex;

            // Get the current pitches
            for (int i = 0; i < 3; i++) pitches[i] = (int)audioManager.PitchAverage(i);

            // Pitch prioritization
            int higherIndex = GetHigher(pitches);
            pitches[higherIndex] *= 2;

            // Pitch white noise prevention if music is not playing
            //if (volume <= 1) freq = 0;
            if (volume <= 1)
                for (int i = 0; i < pitches.Length; i++) pitches[i] = 0;

            // Clamp all values
            for (int i = 0; i < 3; i++) pitches[i] = Math.Clamp(pitches[i], 0, 254);
            //freq = Math.Clamp(freq, 0, 254);
            volume = Math.Clamp(volume, 0, 254);

            //for (int i = 0; i < 3; i++) if (pitches[i] < 50) pitches[i] = 0;

            //Color chosenColor = highFreqColorList[freq];
            UpdateLabels(volume, pitches);
            
            // Send information to the port
            if (!serialPort.IsOpen) serialPort.Open();

            //serialPort.Write("r" + chosenColor.R.ToString() + "g" + chosenColor.G.ToString() + "b" + chosenColor.B.ToString() + "a" + vol.ToString());
            serialPort.Write("m" + currentMode.ToString() + "r" + pitches[0].ToString() + "g" + pitches[1].ToString() + "b" + pitches[2].ToString() + "a" + volume.ToString());
        
        }

        // METHODS
        private void SetupDeviceButtons()
        {
            for (int i = 0; i < audioManager.ConnectedDevices.Count; i++)
            {
                var newButton = new SelectDeviceButton
                {
                    Content = audioManager.ConnectedDevices[i].FriendlyName,
                    Height = 30,
                    Margin = new Thickness(0, 1, 0, 3),
                    Device = audioManager.ConnectedDevices[i],
                    DeviceID = i
                };

                newButton.Click += new RoutedEventHandler((object sender, RoutedEventArgs eventArgs) => { audioManager.AudioMonitorReset(newButton.DeviceID); });
                DevicePanel.Children.Add(newButton);
            }
        }

        private void SetupGradients()
        {
            highFreqColorList = new List<Color>(ColorHelper.GetGradient(new Color[2] { mFreqColor, hFreqColor }, gradientSize));
            lowFreqColorList = new List<Color>(ColorHelper.GetGradient(new Color[2] { lFreqColor, mFreqColor }, gradientSize));
        }
        
        private void StartUpdateTick()
        {
            dispatcherTimer.Tick += Timer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }

        private void UpdateAudioManagerValues()
        {
            // Update FFT (Pitches) values
            audioManager.UpdateFFT();

            // Update Slider Values
            audioManager.SetPitchSmoothness((int)PitchSmoothnessSlider.Value);
            audioManager.SetVolumeSmoothness((int)VolumeSmoothnessSlider.Value);
            audioManager.SetPitchRanges((int)BassRangeSlider.Value, (int)MidRangeSlider.Value, (int)HighRangeSlider.Value);

            // Update Smoothnesses
            audioManager.UpdateVolumeSmoothnessQueue(VolumeIntensitySlider.Value * 100);
            audioManager.UpdatePitchSmoothnessQueue(BassIntensitySlider.Value, MidIntensitySlider.Value, HighIntensitySlider.Value, PitchIntensitySlider.Value);
        }

        private void UpdateLabels(int volume, int[] pitches)
        {
            VolumeLabel.Content = $"Volume: { volume }";

            /*
            string[] pitchStrings = new string[pitches.Length];
            for (int i = 0; i < pitchStrings.Length; i++) pitchStrings[i] = pitches[i].ToString();

            for (int i = 0; i < pitchStrings.Length; i++)
            {
                while (pitchStrings[i].Length < 3)
                    pitchStrings[i] = pitchStrings[i].Insert(0, "0");
            }
            */

            PitchLabel.Content = $"Pitch: ({ pitches[0]}, {pitches[1]}, {pitches[2]})";
        }

        int NonLinearVolume(int current)
        {
            if (current > 180)
            {
                current += 60;
            }

            else if (current > 120)
            {
                current += 40;
            }

            else if (current > 60)
            {
                current += 20;
            }

            return current;
        }

        private int GetHigher(int[] arr)
        {
            int ans = 0;
            int ansVal = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > ansVal)
                {
                    ansVal = arr[i];
                    ans = i;
                }
            }

            return ans;
        }

        private void FullColorButton_Click(object sender, RoutedEventArgs e) => currentMode = 0;
        private void WormButton_Click(object sender, RoutedEventArgs e) => currentMode = 1;
        private void WaveformButton_Click(object sender, RoutedEventArgs e) => currentMode = 2;
    }
}
