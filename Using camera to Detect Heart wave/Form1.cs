using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;



namespace Using_camera_to_Detect_Heart_wave
{
    public partial class Form1 : Form
    {
        private Stopwatch stopwatch = new Stopwatch();

        private List<string> waveDataLog = new List<string>();
        VideoCapture capture = new VideoCapture(0);
        Mat image = new Mat();
        public Form1()
        {
            InitializeComponent();
        }
        Thread thread;
        private int regionSize = 20;
        bool isActive = false;
        private void ActiveButton_Click(object sender, EventArgs e)
        {
            if (!isActive)
            {
                
                ActiveButton.Text = "Stop";
                ActiveButton.BackColor = Color.Red;
                ActiveButton.ForeColor = Color.White;
                thread = new Thread(new ThreadStart(ShowCamera));
                thread.Start();
                isActive = true;
            }
            else
            {
                ActiveButton.Text = "Start";
                ActiveButton.BackColor = Color.Green;
                ActiveButton.ForeColor = Color.White;
                thread.Abort();
                thread.Join(); 
                pictureBox1.Image = null; 
                isActive = false;
            }
        }

        public void ShowCamera()
        {
            while (true)
            {
                capture.Read(image);
                if (image.Empty()) break;

                // Center point
                
                int halfRegion = regionSize / 2;
                int centerX = image.Width / 2;
                int centerY = image.Height / 2;

                if (image.Channels() == 3 &&
                centerX >= halfRegion && centerX < image.Width - halfRegion &&
                centerY >= halfRegion && centerY < image.Height - halfRegion)
                {
                    long sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    for (int y = centerY - halfRegion; y < centerY + halfRegion; y++)
                    {
                        for (int x = centerX - halfRegion; x < centerX + halfRegion; x++)
                        {
                            var pixel = image.At<Vec3b>(y, x);
                            sumB += pixel.Item0;
                            sumG += pixel.Item1;
                            sumR += pixel.Item2;
                            count++;
                        }
                    }

                    int avgR = (int)(sumR / count);
                    int avgG = (int)(sumG / count);
                    int avgB = (int)(sumB / count);

                    if (!isNoseDetectionEnabled)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.Text = $"Center Area Avg RGB: R={avgR} G={avgG} B={avgB}";
                                Cv2.Rectangle(image,
                                    new OpenCvSharp.Rect(centerX - halfRegion, centerY - halfRegion, regionSize, regionSize),
                                    Scalar.Red, 1);
                            }));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Display error: {ex.Message}");
                        }
                    }

                    if (isSave)
                    {
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        string timeStamp = elapsedSeconds.ToString("F4", CultureInfo.InvariantCulture);
                        string dataLine = $"{timeStamp},{avgR},{avgG},{avgB}";
                        waveDataLog.Add(dataLine);
                    }

                   
                }

                if (isNoseDetectionEnabled)
                {
                    if (DetectNose(image, out Rect noseRect, out OpenCvSharp.Point noseCenter))
                    {
                        int sumR = 0, sumG = 0, sumB = 0;
                        int count = 0;

                        for (int y = noseRect.Top; y < noseRect.Bottom && y < image.Height; y++)
                        {
                            for (int x = noseRect.Left; x < noseRect.Right && x < image.Width; x++)
                            {
                                Vec3b pixel = image.At<Vec3b>(y, x);
                                sumB += pixel.Item0;
                                sumG += pixel.Item1;
                                sumR += pixel.Item2;
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            int avgR = sumR / count;
                            int avgG = sumG / count;
                            int avgB = sumB / count;

                            if (isSave)
                            {
                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                string timeStamp = elapsedSeconds.ToString("F4", CultureInfo.InvariantCulture);
                                string dataLine = $"{timeStamp},{avgR},{avgG},{avgB}";
                                waveDataLog.Add(dataLine);
                            }

                            this.Invoke(new Action(() =>
                            {
                                this.Text = $"Nose RGB (avg): R={avgR} G={avgG} B={avgB}";
                            }));
                        }
                    }
                }
                pictureBox1.Image?.Dispose();
                pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
            }
        }



        bool isSave = false;
        

        private void save_data_Click(object sender, EventArgs e)
        {
            if (isSave)
            {
                // Stop the stopwatch first
                

                // Save to CSV
                if (waveDataLog.Count > 0)
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "CSV files (*.csv)|*.csv";
                        saveFileDialog.Title = "Save RGB Data";
                        saveFileDialog.FileName = "wave_data.csv";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                            {
                                writer.WriteLine("Time(s),R,G,B");
                                foreach (var line in waveDataLog)
                                {
                                    writer.WriteLine(line);
                                }
                            }

                            MessageBox.Show("Wave data saved successfully!");
                        }
                    }
                    stopwatch.Stop();
                    waveDataLog.Clear(); // Clear after saving
                }

                // Reset button UI
                save_data.Text = "Save";
                save_data.BackColor = Color.White;
                save_data.ForeColor = Color.Black;
                isSave = false;
            }
            else
            {
                // Start the stopwatch
                stopwatch.Restart();
                waveDataLog.Clear(); // Optional: clear old data when starting new session

                // Update button UI
                save_data.Text = "Saving..";
                save_data.BackColor = Color.Green;
                save_data.ForeColor = Color.White;
                isSave = true;
            }
        }

        private void detect_nose_Click(object sender, EventArgs e)
        {
            if (capture.IsOpened())
            {
                // Start the face detection thread
                if (!isActive)
                {
                    ActiveButton.PerformClick(); // Start the camera if not already active
                }
            }
            else
            {
                MessageBox.Show("Camera is not available.");
            }
        }   

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isActive)
            {
                thread.Abort();
                thread.Join();
            }
            capture.Release();
            pictureBox1.Image?.Dispose();
        }



        private bool isNoseDetectionEnabled = false;

        private bool DetectNose(Mat image, out Rect noseRect, out OpenCvSharp.Point center)
        {
            noseRect = new Rect();
            center = new OpenCvSharp.Point();

            try
            {
                var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);

                var noseCascade = new CascadeClassifier("C:\\code\\Iwate Internship\\Using camera to Detect Heart wave\\Using camera to Detect Heart wave\\Feature\\nose.xml");
                Rect[] noses = noseCascade.DetectMultiScale(gray, 1.1, 4, HaarDetectionTypes.ScaleImage);

                if (noses.Length > 0)
                {
                    noseRect = noses[0];
                    center = new OpenCvSharp.Point(noseRect.X + noseRect.Width / 2, noseRect.Y + noseRect.Height / 2);

                    Cv2.Rectangle(image, noseRect, Scalar.Red, 1);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Detection error: {ex.Message}");
            }

            return false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            isNoseDetectionEnabled = !isNoseDetectionEnabled;

            button1.Text = isNoseDetectionEnabled ? "Nose Detection: ON" : "Nose Detection: OFF";
            button1.BackColor = isNoseDetectionEnabled ? Color.Orange : Color.Gray;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            regionSize = trackBar1.Value;

            this.Text = $"Region Size: {regionSize} x {regionSize}";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

    }
}

