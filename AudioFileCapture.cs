using OpenTok;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;
using System.Xml.Linq;
using System.IO;

namespace AudioCaptureFromFile
{
    internal class AudioFileCapture : IAudioDevice
    {
        bool renderInitialized = false;
        bool renderStarted = false;
        bool captureInitialized = false;
        bool captureStarted = false;        
        int numberOfChannels = 1;
        int sampleRate = 48000;
        private AudioDevice.AudioBus audioBus;
        Queue audioDataQueue = new Queue();
        

        public void LoadFileToBeSent(String audiofile)
        {
            byte[] data;
            Debug.WriteLine("start reading audio romfile");
            try
            {
                using (var reader = new AudioFileReader(audiofile))
                {
                    var outFormat = new WaveFormat(sampleRate, numberOfChannels);
                    using (var conversionStream = new MediaFoundationResampler(reader, outFormat))
                    {
                        //Start reading PCM data
                        using (MemoryStream wavData = new MemoryStream())
                        {
                            var ByteCount = 0;
                            var readBuffer = new byte[1024];
                            while ((ByteCount = conversionStream.Read(readBuffer, 0, readBuffer.Length)) != 0)
                            {
                                wavData.Write(readBuffer, 0, ByteCount);
                            }
                            data = wavData.ToArray();
                        }
                    }
                }
                Debug.WriteLine($"Enqueuing data for OpenTok - Started, Data size:{data.Length}");
                int bufferSize = sampleRate * numberOfChannels * 2 / 100; // Number of bytes in a 10ms batch
                int index = 0;
                while (index < data.Length)
                {
                    int remainingData = data.Length - index;
                    int chunkSize = Math.Min(bufferSize, remainingData);
                    byte[] chunk = new byte[chunkSize];
                    
                    Array.Copy(data, index, chunk, 0, chunkSize);
                    audioDataQueue.Enqueue(chunk);
                    
                    index += chunkSize;
                }

                Debug.WriteLine("Enqueuing data for OpenTok - Finished");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex} An error occurred while enqueuing data for OpenTok: {ex.Message}");
            }
        }



        private void SendToAudioBus(byte[] buffer)
        {
            if (audioBus == null)
            {
                Debug.WriteLine("AUDIO BUS NULL");
                return;
            }
                
            
            int bufferSize = buffer.Length;
            int count = (buffer.Length / 2) / numberOfChannels;

            IntPtr pointer = Marshal.AllocHGlobal(bufferSize);

            Marshal.Copy(buffer, 0, pointer, bufferSize);
            audioBus.WriteCaptureData(pointer, count);
            Marshal.FreeHGlobal(pointer);

            Debug.WriteLine($"Data written to audio bus: {bufferSize}");
        }

        public AudioFileCapture()
        {

        }

        public void DestroyAudio()
        {
            Console.WriteLine("Destroying Audio");
            DestroyAudioCapturer();
            DestroyAudioRenderer();
            Console.WriteLine("Audio Destroyed");
        }

        public void DestroyAudioCapturer()
        {
            StopAudioCapturer();
        }

        public void DestroyAudioRenderer()
        {
            StopAudioRenderer();
        }

        public AudioDeviceSettings GetAudioCapturerSettings()
        {
            Console.WriteLine("Creating new audio capturer settings");
            AudioDeviceSettings capturerSettings = new AudioDeviceSettings();
            capturerSettings.NumChannels = numberOfChannels;
            capturerSettings.SamplingRate = sampleRate;

            Console.WriteLine($"Capturer Sampling Rate: {capturerSettings.SamplingRate} - {capturerSettings.NumChannels} ch");
            return capturerSettings;
        }

        public AudioDeviceSettings GetAudioRendererSettings()
        {

            Console.WriteLine("Creating new audio renderer settings");
            AudioDeviceSettings rendererSettings = new AudioDeviceSettings();
            rendererSettings.NumChannels = numberOfChannels;
            rendererSettings.SamplingRate = sampleRate;

            Console.WriteLine($"Renderer Sampling Rate: {rendererSettings.SamplingRate} - {rendererSettings.NumChannels} ch");
            return rendererSettings;
        }

        public int GetEstimatedAudioCaptureDelay()
        {
            return 0;
        }

        public int GetEstimatedAudioRenderDelay()
        {
            return 0;
        }

        public void InitAudio(AudioDevice.AudioBus audioBus)
        {
            Debug.WriteLine("Audio Initiated");
            this.audioBus = audioBus;
        }

        public void InitAudioCapturer()
        {
            if (captureInitialized)
                return;
            captureInitialized = true;
        }

        public void InitAudioRenderer()
        {
            renderInitialized = true;
        }

        public bool IsAudioCapturerInitialized()
        {
            Console.WriteLine($"Checking Audio Capturer Initialized [{captureInitialized}]");
            return captureInitialized;
        }

        public bool IsAudioCapturerStarted()
        {
            Console.WriteLine($"Checking Audio Capturer Started [{captureStarted}]");
            return captureStarted;
        }

        public bool IsAudioRendererInitialized()
        {
            return renderInitialized;
        }

        public bool IsAudioRendererStarted()
        {
            return renderStarted;
        }

        public void StartAudioCapturer()
        {
            if (captureStarted)
                return;
            captureStarted = true;
            Task.Run(() =>
            {
                DateTime nextBatchTime = DateTime.Now;
                while (captureStarted)
                {
                    try
                    {
                        if (DateTime.Now >= nextBatchTime)
                        {
                            nextBatchTime += new TimeSpan(0, 0, 0, 0, 10);

                            

                            if (audioDataQueue.Count > 0)
                            {
                                byte[] b = (byte[])audioDataQueue.Dequeue();
                                SendToAudioBus(b);
                            }
                               
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }

        public void StartAudioRenderer()
        {
            renderStarted = true;
        }

        public void StopAudioCapturer()
        {
            captureStarted = false;
        }

        public void StopAudioRenderer()
        {
            renderStarted = false;
        }
    }
}

