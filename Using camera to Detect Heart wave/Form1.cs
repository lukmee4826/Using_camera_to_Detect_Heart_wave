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
                int centerX = image.Width / 2;
                int centerY = image.Height / 2;
                
                if (image.Channels() == 3 &&
                    centerX >= 0 && centerX < image.Width &&
                    centerY >= 0 && centerY < image.Height)
                {
                    var pixel = image.At<Vec3b>(centerY, centerX); // (row, col) = (Y, X)

                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.Text = $"Center Point RGB: R={pixel.Item2} G={pixel.Item1} B={pixel.Item0}";
                        }));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Detection error: {ex.Message}");
                    }

                    Cv2.Circle(image, new OpenCvSharp.Point(centerX, centerY), 2, Scalar.Red, -1);

                    if (isSave)
                    {
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        string timeStamp = elapsedSeconds.ToString("F4", CultureInfo.InvariantCulture);
                        string dataLine = $"{timeStamp},{pixel.Item2},{pixel.Item1},{pixel.Item0}";
                        waveDataLog.Add(dataLine);
                    }
                }

                
                if (isNoseDetectionEnabled)
                {
                    if (DetectNose(image, out Vec3b noseRGB, out OpenCvSharp.Point noseCenter))
                    {
                        if (isSave)
                        {
                            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                            string timeStamp = elapsedSeconds.ToString("F4", CultureInfo.InvariantCulture);
                            string dataLine = $"{timeStamp},{noseRGB.Item2},{noseRGB.Item1},{noseRGB.Item0}";
                            waveDataLog.Add(dataLine);
                        }

                        this.Invoke(new Action(() =>
                        {
                            this.Text = $"Nose RGB: R={noseRGB.Item2} G={noseRGB.Item1} B={noseRGB.Item0}";
                        }));
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

        
        private void pictureBox1_Click(object sender, EventArgs e)
        {
           
        }



        private bool isNoseDetectionEnabled = false;

        private bool DetectNose(Mat image, out Vec3b rgb, out OpenCvSharp.Point center)
        {
            rgb = new Vec3b();
            center = new OpenCvSharp.Point();

            try
            {
                var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);
                Mat resized = new Mat();
                Cv2.Resize(gray, resized, new OpenCvSharp.Size(gray.Width / 3, gray.Height / 3));

                var noseCascade = new CascadeClassifier("C:\\code\\Iwate Internship\\Using camera to Detect Heart wave\\Using camera to Detect Heart wave\\Feature\\nose.xml");
                Rect[] noses = noseCascade.DetectMultiScale(gray, 1.1, 4, HaarDetectionTypes.ScaleImage);

                if (noses.Length > 0)
                {
                    var nose = noses[0];
                    center = new OpenCvSharp.Point(nose.X + nose.Width / 2, nose.Y + nose.Height / 2);
                    rgb = image.At<Vec3b>(center.Y, center.X);

                   
                    Cv2.Rectangle(image, nose, Scalar.Green, 2);
                    Cv2.Circle(image, center, 2, Scalar.Red, -1);

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
    }
}
