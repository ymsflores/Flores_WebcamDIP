using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics.Tracing;
using System.Drawing.Imaging;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
      
        int filter = 1; // 1 = copy, 2 = greyscale, 3 = invert, 4 = histogram, 5 = sepia, 6 = subtract

        string path;
        Bitmap loaded, loadedBG, loadedBGtemp, processed, colorGreen, bg1, bg2;
        VideoCaptureDevice videoSource;
        public Form1()
        {
            InitializeComponent();
            this.Text = "Webcam Video Processor";
            MessageBox.Show("Welcome! Please turn on your camera to access filters.");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Stop and free the webcam object if application is closing
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource = null;
            }
        }

        void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
            //you'll probably get access violation exceptions
            loaded = ResizeImage((Bitmap)eventArgs.Frame.Clone(), new Size(pictureBox1.Width, pictureBox1.Height));
            processed = ResizeImage((Bitmap)eventArgs.Frame.Clone(), new Size(pictureBox1.Width, pictureBox1.Height));
            pictureBox1.BackgroundImage = loaded;

            if (filter != 6) {
                pictureBox3.BackgroundImage = null;
            }

            switch (filter)
            {
                case 1:
                    pictureBox2.BackgroundImage = processed;
                    break;
                case 2:
                    pictureBox2.BackgroundImage = GreyscaleFilter(processed);
                    break;
                case 3:
                    pictureBox2.BackgroundImage = InvertFilter(processed);
                    break;
                case 4:
                    pictureBox2.BackgroundImage = HistogramFilter(processed);
                    break;
                case 5:
                    pictureBox2.BackgroundImage = SepiaFilter(processed);
                    break;
                case 6:
                    pictureBox3.BackgroundImage = SubtractFilter(processed);
                    break;
                default:
                    pictureBox2.BackgroundImage = null;
                    break;
            }
            
        }

        public static Bitmap GreyscaleFilter(Bitmap b)
        {
            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            System.IntPtr Scan0 = bmData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - b.Width * 3;

                byte red, green, blue;

                for (int y = 0; y < b.Height; ++y)
                {
                    for (int x = 0; x < b.Width; ++x)
                    {
                        blue = p[0];
                        green = p[1];
                        red = p[2];

                        p[0] = p[1] = p[2] = (byte)(.299 * red + .587 * green + .114 * blue);

                        p += 3;  ///very good....
					}
                    p += nOffset;
                }
            }

            b.UnlockBits(bmData);

            return b;
        }

        public static Bitmap InvertFilter(Bitmap b)
        {
            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            System.IntPtr Scan0 = bmData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - b.Width * 3;
                int nWidth = b.Width * 3;

                for (int y = 0; y < b.Height; ++y)
                {
                    for (int x = 0; x < nWidth; ++x)
                    {
                        p[0] = (byte)(255 - p[0]);
                        ++p;
                    }
                    p += nOffset;
                }
            }

            b.UnlockBits(bmData);

            return b;
        }

        public Bitmap HistogramFilter(Bitmap b)
        {
            Color pixel;
            int intensity;
            int[] histogram = new int[256];
            processed = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    pixel = b.GetPixel(x, y);
                    intensity = (int)(pixel.R + pixel.G + pixel.B) / 3;

                    histogram[intensity]++;
                }
            }

            using (Graphics g = Graphics.FromImage(processed))
            {
                int maxValue = 0;
                foreach (int value in histogram)
                {
                    if (value > maxValue)
                        maxValue = value;
                }

                for (int i = 0; i < 256; i++)
                {
                    int barHeight = (int)((double)histogram[i] / maxValue * processed.Height);
                    g.DrawLine(Pens.Black, i, processed.Height, i, processed.Height - barHeight);
                }
            }

            return processed;
        }

        public static Bitmap SepiaFilter(Bitmap b)
        {
            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            System.IntPtr Scan0 = bmData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - b.Width * 3;

                byte red, green, blue;

                for (int y = 0; y < b.Height; ++y)
                {
                    for (int x = 0; x < b.Width; ++x)
                    {
                        blue = p[0];
                        green = p[1];
                        red = p[2];

                        byte tr = (byte)(0.393 * red + 0.769 * green + 0.189 * blue);
                        byte tg = (byte)(0.349 * red + 0.686 * green + 0.168 * blue);
                        byte tb = (byte)(0.272 * red + 0.534 * green + 0.131 * blue);

                        if (tr > 255) {
                            red = 255;
                        }
                        else {
                            red = tr;
                        }

                        if (tg > 255) {
                            green = 255;
                        }
                        else {
                            green = tg;
                        }

                        if (tb > 255) {
                            blue = 255;
                        }
                        else {
                            blue = tb;
                        }

                        p[0] = blue;
                        p[1] = green;
                        p[2] = red;
                        p += 3;  ///very good....
					}
                    p += nOffset;
                }
            }

            b.UnlockBits(bmData);

            return b;
        }

        public Bitmap SubtractFilter(Bitmap b)
        {
            Color mygreen = Color.FromArgb(0, 0, 255);
            int greygreen = (mygreen.R + mygreen.G + mygreen.B) / 3;
            int threshold = 5;
            processed = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            for (int x = 0; x < b.Width; x++) {
                for (int y = 0; y < b.Height; y++)
                {
                    Color pixel = b.GetPixel(x, y);
                    Color backpixel = loadedBG.GetPixel(x, y);
                    int grey = (pixel.R + pixel.G + pixel.B) / 3;
                    int subtractvalue = Math.Abs(grey - greygreen);

                    if (subtractvalue > threshold)
                        processed.SetPixel(x, y, backpixel);
                    else
                        processed.SetPixel(x, y, pixel);
                }
            }

            return processed;

        }
        public static Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            try
            {
                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage((System.Drawing.Image)b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch
            {
                Console.WriteLine("Bitmap could not be resized");
                return imgToResize;
            }
        }

        private void copyToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            btnBg.Enabled = false;
            filter = 1;
        }

        private void grayscaleToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            filter = 2;
        }


        private void invertToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            filter = 3;
        }

        private void histogramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filter = 4;
        }

        private void sepiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filter = 5;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (loaded == null)
            {
                MessageBox.Show("No image has been loaded.");
            }
            else
            {
                SaveFileDialog save = new SaveFileDialog();
                save.ShowDialog();
                path = save.FileName;
                if (path == "") {
                    MessageBox.Show("Please write a filename");
                } else {
                    if (filter == 6)
                        pictureBox3.BackgroundImage.Save(path + ".jpg");
                    else
                        pictureBox2.BackgroundImage.Save(path + ".jpg");
                }
            }
        }

        private void btnBg_Click(object sender, EventArgs e)
        {
            filter = 0;
            OpenFileDialog open = new OpenFileDialog();
            open.ShowDialog();
            path = open.FileName;
            if (path == "") {
                MessageBox.Show("No image selected. Please retry.");
            } else {
                try {
                    using (var bitmap = new Bitmap(path)) {
                        loadedBGtemp = new Bitmap(path);
                        loadedBG = ResizeImage(loadedBGtemp, new Size(pictureBox2.Width, pictureBox2.Height));
                        filter = 6;
                        bg1 = (Bitmap)loadedBG.Clone();
                        pictureBox2.BackgroundImage = bg1;
                        btnBg.Enabled = false;
                    }
                }
                catch {
                    MessageBox.Show("Wrong filetype uploaded. Please select a valid image file.");
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            foreach (ToolStripButton item in toolStrip1.Items)
            {
                item.Enabled = true;

            }

            btnLoad.Enabled = false;
            //List all available video sources. (That can be webcams as well as tv cards, etc)
            FilterInfoCollection videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //Check if atleast one video source is available
            if (videosources != null)
            {
                //For example use first video device. You may check if this is your webcam.
                videoSource = new VideoCaptureDevice(videosources[0].MonikerString);

                try
                {
                    //Check if the video device provides a list of supported resolutions
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        string highestSolution = "0;0";
                        //Search for the highest resolution
                        for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
                        {
                            if (videoSource.VideoCapabilities[i].FrameSize.Width > Convert.ToInt32(highestSolution.Split(';')[0]))
                                highestSolution = videoSource.VideoCapabilities[i].FrameSize.Width.ToString() + ";" + i.ToString();
                        }
                        //Set the highest resolution as active
                        videoSource.VideoResolution = videoSource.VideoCapabilities[Convert.ToInt32(highestSolution.Split(';')[1])];
                    }
                }
                catch { }

                //Create NewFrame event handler
                //(This one triggers every time a new frame/image is captured
                videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);

                //Start recording
                videoSource.Start();
            }
        }

        private void subtractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnBg.Enabled = true;
            filter = 0;
            MessageBox.Show("You have selected SUBTRACT. Please load an image with the 'Load Background' button in order to proceed. ");
        }

    }
}
