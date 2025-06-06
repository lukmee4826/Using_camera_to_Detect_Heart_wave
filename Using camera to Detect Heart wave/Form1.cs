using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using System.Threading;



namespace Using_camera_to_Detect_Heart_wave
{
    public partial class Form1 : Form
    {
        VideoCapture capture = new VideoCapture(0); // 0 for the default camera
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
                thread.Join(); // Ensure the thread is properly terminated
                pictureBox1.Image = null; // Clear the picture box
                isActive = false;
            }
        }

        public void ShowCamera()
        {
            while (true)
            {
                capture.Read(image);
                if (image.Empty())
                {
                    break;
                }
                pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
            }
        }


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
