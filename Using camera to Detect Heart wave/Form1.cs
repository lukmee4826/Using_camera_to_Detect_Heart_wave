using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            
            CascadeClassifier faceCascade = new CascadeClassifier("C:\\code\\Iwate Internship\\Using camera to Detect Heart wave\\Using camera to Detect Heart wave\\Feature\\nose.xml");

            while (true)
            {
                capture.Read(image);
                if (image.Empty()) break;

                // Convert to grayscale for detection
                Mat grayImage = new Mat();
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGRA2GRAY);

                // Detect faces/noses
                Rect[] faces = faceCascade.DetectMultiScale(grayImage, 1.1, 4, HaarDetectionTypes.ScaleImage);

                if (faces.Length > 0)
                {
                    Rect face = faces[0];
                    Cv2.Rectangle(image, face, Scalar.Green, 2);

                    int centerX = face.X + face.Width / 2;
                    int centerY = face.Y + face.Height / 2;

                    Vec3b pixelColor = image.At<Vec3b>(centerY, centerX);
                    byte blue = pixelColor.Item0;
                    byte green = pixelColor.Item1;
                    byte red = pixelColor.Item2;

                    // Save data to list if saving is active
                    if (isSave)
                    {
                        string dataLine = $"{DateTime.Now:HH:mm:ss.fff},{red},{green},{blue}";
                        waveDataLog.Add(dataLine);
                    }

                    this.Invoke(new Action(() =>
                    {
                        this.Text = $"Nose RGB: R={red} G={green} B={blue}";
                    }));

                    Cv2.Circle(image, new OpenCvSharp.Point(centerX, centerY), 2, Scalar.Red, -1);
                }

                pictureBox1.Image?.Dispose();
                pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);

                Cv2.WaitKey(1);
            }
        }



        bool isSave = false;
        private void save_data_Click(object sender, EventArgs e)
        {
            if (isSave)
            {
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
                                writer.WriteLine("Time,R,G,B");
                                foreach (var line in waveDataLog)
                                {
                                    writer.WriteLine(line);
                                }
                            }

                            MessageBox.Show("Wave data saved successfully!");
                        }
                    }

                    waveDataLog.Clear(); // clear after save
                }

                
                save_data.Text = "Save";
                save_data.BackColor = Color.White;
                save_data.ForeColor = Color.Black;
                isSave = false;
            }
            else
            {
                
                save_data.Text = "Saving..";
                save_data.BackColor = Color.Green;
                save_data.ForeColor = Color.White;
                isSave = true;
            }
        }


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
