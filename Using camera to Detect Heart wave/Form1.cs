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
using System.Windows.Forms.DataVisualization.Charting;
using Accord.Statistics.Analysis;


namespace Using_camera_to_Detect_Heart_wave
{
    public partial class Form1 : Form
    {
        public IndependentComponentAlgorithm Algorithm { get; set; }
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

        private Queue<double> posSignalQueue = new Queue<double>();
        private int signalWindowSize = 100;
        private Chart signalChart;

        private List<double> R_list = new List<double>();
        private List<double> G_list = new List<double>();
        private List<double> B_list = new List<double>();

        private Queue<double> rQueue = new Queue<double>();
        private Queue<double> gQueue = new Queue<double>();
        private Queue<double> bQueue = new Queue<double>();

        CancellationTokenSource cts = new CancellationTokenSource();
        private void InitChart()
        {
            signalChart = new Chart();
            signalChart.Dock = DockStyle.Fill;

            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Title = "Frame";

            // Primary Y axis for RGB
            chartArea.AxisY.Title = "RGB Intensity";
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = 255;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;

            // Secondary Y axis for POS
            chartArea.AxisY2.Title = "POS Signal";
            chartArea.AxisY2.Enabled = AxisEnabled.True;
            chartArea.AxisY2.MajorGrid.Enabled = false;
            chartArea.AxisY2.Minimum = -5.0;
            chartArea.AxisY2.Maximum = 5.0;

            signalChart.ChartAreas.Add(chartArea);

            // POS Signal using secondary Y axis
            var posSeries = new Series("POS Signal")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 2,
                ChartArea = "MainArea",
                YAxisType = AxisType.Secondary
            };

            // RGB Series using primary Y axis
            var rSeries = new Series("R")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 1
            };
            var gSeries = new Series("G")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Green,
                BorderWidth = 1
            };
            var bSeries = new Series("B")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Blue,
                BorderWidth = 1
            };

            signalChart.Series.Add(posSeries);
            signalChart.Series.Add(rSeries);
            signalChart.Series.Add(gSeries);
            signalChart.Series.Add(bSeries);

            pictureBox2.Controls.Clear();
            pictureBox2.Controls.Add(signalChart);
            signalChart.BringToFront();
        }

        private void UpdateChart(double posValue, int r, int g, int b)
        {
            if (signalChart == null || signalChart.Series.Count < 4)
                return;

            if (posSignalQueue.Count >= signalWindowSize)
            {
                posSignalQueue.Dequeue();
                rQueue.Dequeue();
                gQueue.Dequeue();
                bQueue.Dequeue();
            }

            posSignalQueue.Enqueue(posValue);
            rQueue.Enqueue(r);
            gQueue.Enqueue(g);
            bQueue.Enqueue(b);

            signalChart.Series["POS Signal"].Points.Clear();
            signalChart.Series["R"].Points.Clear();
            signalChart.Series["G"].Points.Clear();
            signalChart.Series["B"].Points.Clear();

            int i = 0;
            foreach (var val in posSignalQueue)
            {
                signalChart.Series["POS Signal"].Points.AddXY(i++, val);
            }

            i = 0;
            foreach (var val in rQueue) signalChart.Series["R"].Points.AddXY(i++, val);
            i = 0;
            foreach (var val in gQueue) signalChart.Series["G"].Points.AddXY(i++, val);
            i = 0;
            foreach (var val in bQueue) signalChart.Series["B"].Points.AddXY(i++, val);

            var area = signalChart.ChartAreas[0];
            area.AxisX.Minimum = Math.Max(0, i - signalWindowSize);
            area.AxisX.Maximum = i;
        }

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

                   

                    R_list.Add(avgR);
                    G_list.Add(avgG);
                    B_list.Add(avgB);

                    if (R_list.Count > 1)
                    {
                        double[] S = GetPOS(R_list, G_list, B_list);
                        if (S.Length > 0)
                        {
                            double last = S[S.Length - 1];
                            this.Invoke(new Action(() =>
                            {
                                UpdateChart(last, avgR, avgG, avgB);
                            }));
                        }
                    }


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
        

        private double[] GetICA(List<double> R, List<double> G, List<double> B)
        {
            int len = Math.Min(R.Count, Math.Min(G.Count, B.Count));
            if (len < 60) return new double[0];

            int windowSize = 60;
            if (len > windowSize)
            {
                R = R.Skip(len - windowSize).ToList();
                G = G.Skip(len - windowSize).ToList();
                B = B.Skip(len - windowSize).ToList();
                len = windowSize;
            }

            // Construct the RGB matrix: [samples][channels]
            double[][] rgbMatrix = new double[len][];
            for (int i = 0; i < len; i++)
            {
                rgbMatrix[i] = new double[3] { R[i], G[i], B[i] };
            }

            // Run ICA (no Method or Algorithm property in version 3.8.0)
            var ica = new IndependentComponentAnalysis(); // 3 outputs

            ica.Learn(rgbMatrix); // Learn ICA components
            double[][] sources = ica.Transform(rgbMatrix); // Apply transformation

            // Select the first component as BVP signal (you can try others too)
            double[] bvp = sources.Select(s => s[0]).ToArray();

            return bvp;
        }


        private double[] GetPOS(List<double> R, List<double> G, List<double> B)
        {
            int len = Math.Min(R.Count, Math.Min(G.Count, B.Count));
            if (len < 60) return new double[0]; // need enough frames

            int windowSize = 60;
            if (len > windowSize)
            {
                R = R.Skip(len - windowSize).ToList();
                G = G.Skip(len - windowSize).ToList();
                B = B.Skip(len - windowSize).ToList();
                len = windowSize;
            }

            // Convert to double arrays
            double[] r = R.ToArray();
            double[] g = G.ToArray();
            double[] b = B.ToArray();

            // Normalize each color channel by its mean (temporal normalization)
            double meanR = r.Average();
            double meanG = g.Average();
            double meanB = b.Average();

            for (int i = 0; i < len; i++)
            {
                r[i] = r[i] / (meanR + 1e-8) - 1.0;
                g[i] = g[i] / (meanG + 1e-8) - 1.0;
                b[i] = b[i] / (meanB + 1e-8) - 1.0;
            }

            // POS projection
            double[] x1 = new double[len];
            double[] x2 = new double[len];
            for (int i = 0; i < len; i++)
            {
                x1[i] = 2.0 * r[i] - g[i];
                x2[i] = 1.5 * r[i] + g[i] - 1.5 * b[i];
            }

            // Normalize x1 and x2
            double meanX1 = x1.Average();
            double meanX2 = x2.Average();
            double stdX2 = Math.Sqrt(x2.Select(v => (v - meanX2) * (v - meanX2)).Average());
            stdX2 = Math.Max(stdX2, 1e-6); // avoid division explosion

            // Combine signal
            double[] h = new double[len];
            for (int i = 0; i < len; i++)
            {
                h[i] = (x1[i] - meanX1) + (x2[i] - meanX2) / stdX2;
            }

            return h;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            InitChart();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }
}

