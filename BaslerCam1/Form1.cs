using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Basler.Pylon;
using System.Diagnostics;
using System.IO;

namespace BaslerCam1
{
    public partial class MainForm : Form
    {
        public int CameraNumber = CameraFinder.Enumerate().Count();
        public delegate void CameraImage(Bitmap bmp);
        public event CameraImage CameraImageEvent;
        Camera camera;
        bool GrabOver = false;
        PixelDataConverter pxConvert = new PixelDataConverter();
        
        public MainForm()
        {
            InitializeComponent();
            CameraImageEvent += MainForm_CameraImageEvent;

        }

        private void MainForm_CameraImageEvent(Bitmap bmp)
        {
            pictureBox1.Invoke(new MethodInvoker(delegate
            {
                Bitmap old = pictureBox1.Image as Bitmap;
                pictureBox1.Image = bmp;
                if (old != null)
                {
                    old.Dispose();
                }
            }));
        }
        void Unanble()
        {
            btnStop.Enabled = false;
            btnOneShot.Enabled = false;
            btnContinuousShot.Enabled = false;

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (CameraNumber > 0)
            {
                CameraInit();               
                
            }
            else
            {
                MessageBox.Show("Not connect to camera");
                Unanble();
            }
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label2.Text = trackBar1.Value.ToString();
            camera.Parameters[PLCamera.ExposureTime].SetValue(trackBar1.Value);
        }
        private void btnOneShot_Click(object sender, EventArgs e)
        {
            OneShot();
        }

        private void btnContinuousShot_Click(object sender, EventArgs e)
        {
            KeepShot();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        public void CameraInit()
        {
            camera = new Camera();
            camera.CameraOpened += Configuration.AcquireContinuous;
            camera.ConnectionLost += Camera_ConnectionLost;
            camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;

            camera.Open();
        }
        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            GrabOver = false;
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;
            if (grabResult.IsValid)
            {
                if (GrabOver)
                    CameraImageEvent(GrabResult2Bmp(grabResult));
            }
        }

        private void Camera_ConnectionLost(object sender, EventArgs e)
        {
            camera.StreamGrabber.Stop();
            DestroyCamera();
        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            GrabOver = true;
        }

        public void OneShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void KeepShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void Stop()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Stop();
            }
        }

        Bitmap GrabResult2Bmp(IGrabResult grabResult)
        {
            Bitmap b = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);
            pxConvert.OutputPixelFormat = PixelType.BGRA8packed;
            IntPtr bmpIntpr = bmpData.Scan0;
            pxConvert.Convert(bmpIntpr, bmpData.Stride * b.Height, grabResult);
            b.UnlockBits(bmpData);
            return b;
        }

        public void DestroyCamera()
        {
            if (camera != null)
            {
                camera.Close();
                camera.Dispose();
                camera = null;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DestroyCamera();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(@"C:\Users\DEMO1\source\repos\BaslerCam1\Capture"))
                {
                    Directory.CreateDirectory(@"C:\Users\DEMO1\source\repos\USBCamApp1\Capture");
                    MessageBox.Show("NO FLODER FOUND!!");
                }
                else
                {
                    string path = @"C:\Users\DEMO1\source\repos\BaslerCam1\Capture";
                    pictureBox1.Image.Save(path + @"\" + textBox1.Text + ".jpg", ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
