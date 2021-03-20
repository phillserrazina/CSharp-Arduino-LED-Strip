using System;
using System.Linq;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections.Generic;


namespace Bassinator
{
    class AudioManager
    {
        // VARIABLES
        public static MMDevice CurrentDevice { get; private set; }
        public readonly MMDeviceCollection ConnectedDevices;
        private WaveInEvent waveIn;

        private Int16[] dataPCM;
        private double[] dataFFT;

        private int pitchSmoothnessRate, volumeSmoothnessRate;
        private readonly List<Queue<double>> pitchSmoothnessQueues = new List<Queue<double>>();
        private readonly Queue<double> volumeSmoothnessQueue = new Queue<double>();

        private int bassRange, midRange, highRange;

        public double PitchAverage(int index) => GetSmoothness(pitchSmoothnessRate, pitchSmoothnessQueues[index]);
        public void SetPitchSmoothness(int newValue) => pitchSmoothnessRate = newValue;

        public double VolumeAverage { get { return GetSmoothness(volumeSmoothnessRate, volumeSmoothnessQueue); } }
        public void SetVolumeSmoothness(int newValue) => volumeSmoothnessRate = newValue;

        public int PredominantFFTIndex
        {
            get
            {
                double ans = 0;
                int ansIndex = 0;

                for (int i = 0; i < dataFFT.Length; i++)
                {
                    if (dataFFT[i] > ans)
                    {
                        ans = dataFFT[i];
                        ansIndex = i;
                    }
                }

                return ansIndex;
            }
        }

        // METHODS
        public AudioManager(int defaultDeviceIndex)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            ConnectedDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            for (int i = 0; i < 3; i++) pitchSmoothnessQueues.Add(new Queue<double>());

            CurrentDevice = ConnectedDevices[defaultDeviceIndex];
            AudioMonitorInitialize(defaultDeviceIndex);
        }

        public MMDevice[] GetAllDevices()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            return devices.ToArray();
        }

        public static void SetDevice(MMDevice dev) => CurrentDevice = dev;

        private void AudioMonitorInitialize(
                int DeviceIndex, int sampleRate = 32_000,
                int bitRate = 16, int channels = 1,
                int bufferMilliseconds = 50, bool start = true
            )
        {
            if (waveIn == null)
            {
                waveIn = new WaveInEvent
                {
                    DeviceNumber = DeviceIndex,
                    WaveFormat = new WaveFormat(sampleRate, bitRate, channels)
                };

                waveIn.DataAvailable += OnDataAvailable;
                waveIn.BufferMilliseconds = bufferMilliseconds;
                if (start)
                    waveIn.StartRecording();
            }
        }

        public void AudioMonitorReset(int deviceIndex)
        {
            waveIn.StopRecording();
            AudioMonitorInitialize(deviceIndex);
        }
        
        private void OnDataAvailable(object sender, WaveInEventArgs args)
        {
            int bytesPerSample = waveIn.WaveFormat.BitsPerSample / 8;
            int samplesRecorded = args.BytesRecorded / bytesPerSample;
            if (dataPCM == null)
                dataPCM = new Int16[samplesRecorded];
            for (int i = 0; i < samplesRecorded; i++)
                dataPCM[i] = BitConverter.ToInt16(args.Buffer, i * bytesPerSample);
        }

        public void UpdateFFT()
        {
            if (dataPCM == null)
                return;

            // the PCM size to be analyzed with FFT must be a power of 2
            int fftPoints = 2;
            while (fftPoints * 2 <= dataPCM.Length)
                fftPoints *= 2;

            // apply a Hamming window function as we load the FFT array then calculate the FFT
            NAudio.Dsp.Complex[] fftFull = new NAudio.Dsp.Complex[fftPoints];
            for (int i = 0; i < fftPoints; i++)
                fftFull[i].X = (float)(dataPCM[i] * NAudio.Dsp.FastFourierTransform.HammingWindow(i, fftPoints));
            NAudio.Dsp.FastFourierTransform.FFT(true, (int)Math.Log(fftPoints, 2.0), fftFull);

            // copy the complex values into the double array that will be plotted
            if (dataFFT == null)
                dataFFT = new double[fftPoints / 2];
            for (int i = 0; i < fftPoints / 2; i++)
            {
                double fftLeft = Math.Abs(fftFull[i].X + fftFull[i].Y);
                double fftRight = Math.Abs(fftFull[fftPoints - i - 1].X + fftFull[fftPoints - i - 1].Y);
                dataFFT[i] = fftLeft + fftRight;
            }
        }

        public double GetFFT(int index) => dataFFT[index];

        public double GetFrequencyAverage(int beginning, int end)
        {
            double ans = 0;
            end += beginning;

            for (int i = beginning; i < end; i++)
                ans += dataFFT[i];

            return ans / (end - beginning);
        }

        public double GetSmoothness(int smoothnessRate, Queue<double> queue)
        {
            if (smoothnessRate == 0) return queue.Last();
            if (queue.Count < smoothnessRate) return 0;

            double ans = 0;

            for (int i = 0; i < smoothnessRate; i++)
            {
                ans += queue.ElementAt(i);
            }

            return ans / smoothnessRate;
        }

        public void SetPitchRanges(int bassRange, int midRange, int highRange)
        {
            this.bassRange = bassRange;
            this.midRange = midRange;
            this.highRange = highRange;
        }

        public void UpdatePitchSmoothnessQueue(double bassIntensity, double midIntensity, double highIntensity, double masterIntensity)
        {
            foreach (var q in pitchSmoothnessQueues)
                while (q.Count > pitchSmoothnessRate) q.Dequeue();

            pitchSmoothnessQueues[0].Enqueue(GetFrequencyAverage(0, bassRange) * masterIntensity * bassIntensity);
            pitchSmoothnessQueues[1].Enqueue(GetFrequencyAverage(30, midRange) * masterIntensity * midIntensity);
            pitchSmoothnessQueues[2].Enqueue(GetFrequencyAverage(60, highRange) * masterIntensity * highIntensity);
        }

        public void UpdateVolumeSmoothnessQueue(double intensity)
        {
            while (volumeSmoothnessQueue.Count > volumeSmoothnessRate) volumeSmoothnessQueue.Dequeue();
            volumeSmoothnessQueue.Enqueue(CurrentDevice.AudioMeterInformation.MasterPeakValue * intensity);
        }
    }
}
