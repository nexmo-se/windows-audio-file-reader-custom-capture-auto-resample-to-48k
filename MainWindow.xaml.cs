using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.SqlServer.Server;
using NAudio.Wave;
using System.Windows.Markup;
using OpenTok;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.ComponentModel;

namespace AudioCaptureFromFile
{
    public partial class MainWindow : Window
    {
        private const string API_KEY = "47464991";
        private const string SESSION_ID = "2_MX40NzQ2NDk5MX5-MTY4NjgzMzk5ODMxOH5LUlc3c1h6VkNXemN6Z2RaeVNGUkZNMmV-fn4";
        private const string TOKEN = "T1==cGFydG5lcl9pZD00NzQ2NDk5MSZzaWc9MTRmZDcwOGVhNzJhODE1ODhhMmQ1NTQwMTQwMjJlN2I3NTNkMzUyODpzZXNzaW9uX2lkPTJfTVg0ME56UTJORGs1TVg1LU1UWTROamd6TXprNU9ETXhPSDVMVWxjM2MxaDZWa05YZW1ONloyUmFlVk5HVWtaTk1tVi1mbjQmY3JlYXRlX3RpbWU9MTY4NjgzNDAyMyZub25jZT0wLjE2NzMwOTQ3MjUxNTc5OTUzJnJvbGU9cHVibGlzaGVyJmV4cGlyZV90aW1lPTE2ODY5MjA0MjMmaW5pdGlhbF9sYXlvdXRfY2xhc3NfbGlzdD0=";
        Context context = new Context(new WPFDispatcher());
        
        private Session Session;
        private Publisher Publisher;
        private AudioFileCapture AudioFileRd ;

        StringBuilder sb = new StringBuilder();
        private String logfile = "../../log.txt";

        //the audio file you want to load
        //private const string audiofile = "../../sample-48khz-96kbit.mp3";
        //private const string audiofile = "../../sample-48khz-96kbit.wav";
        //private const string audiofile = "../../sample-24khz-48kbit.mp3";
        private const string audiofile = "../../sample-24khz-48kbit.wav";

        public object Data { get; private set; }

        public MainWindow()
        {
            Debug.WriteLine("Starting");

            void OTlogger(string message)
            {
                sb.AppendLine(message);
            }
            OpenTok.Logger.LogCallback OTLog = OTlogger;

            OpenTok.Logger.Enable(Logger.Level.Debug, true, OTLog);
            InitializeComponent();
       
            
            AudioFileRd = new AudioFileCapture();

            AudioFileRd.LoadFileToBeSent(audiofile);
            AudioDevice.SetCustomAudioDevice(context, AudioFileRd);

            IList<VideoCapturer.VideoDevice> capturerDevices = VideoCapturer.EnumerateDevices();
            if (capturerDevices == null || capturerDevices.Count == 0)
                throw new Exception("No video capture devices detected");
            
            Publisher = new Publisher.Builder(context)
            {
                Capturer = capturerDevices[0].CreateVideoCapturer(VideoCapturer.Resolution.High, VideoCapturer.FrameRate.Fps30),
                Renderer = PublisherVideo,
                HasAudioTrack = true

            }.Build();
            
            Session = new Session.Builder(context, API_KEY, SESSION_ID).Build();
            Session.Connected += Session_Connected;
            Session.Disconnected += Session_Disconnected;
            Session.Error += Session_Error;
            Session.StreamReceived += Session_StreamReceived;
            
            Session.Connect(TOKEN);           
        }

        public byte[] AudioReader(string filename, int samplerate, int bits, int channel)
        {
            Data = null;
            Debug.WriteLine("Reading Data");
            WaveFormat Format = new WaveFormat(samplerate, bits, channel);
            using (WaveFileReader reader = new WaveFileReader(filename))
            {
                byte[] src = new byte[reader.Length];
                reader.Read(src, 0, src.Length);
                return src;
            }            
        }

        private  void Session_Connected(object sender, System.EventArgs e)
        {
            Session.Publish(Publisher);
            Debug.WriteLine("Publishing");
        }
 
        private void Session_Disconnected(object sender, System.EventArgs e)
        {
            Trace.WriteLine("Session disconnected.");
        }

        private void Session_Error(object sender, Session.ErrorEventArgs e)
        {
            Trace.WriteLine("Session error:" + e.ErrorCode);
        }

        private void Session_StreamReceived(object sender, Session.StreamEventArgs e)
        {
            Subscriber subscriber = new Subscriber.Builder(context, e.Stream)
            {
                Renderer = SubscriberVideo
            }.Build();
            Session.Subscribe(subscriber);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            MessageBox.Show("Closing called");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logfile))
            {
                file.WriteLine(sb.ToString()); // "sb" is the StringBuilder

            }
           
        }

    }
}
