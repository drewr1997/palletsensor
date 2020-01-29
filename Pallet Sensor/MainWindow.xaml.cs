using Microsoft.Kinect;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System;

// Author: Andrew Ross
// Student ID: 16511676

namespace Pallet_Sensor
{
    public partial class MainWindow : Window
    {
        // Declaring Variables
        private KinectSensor ksensor;                   //Declares kinect
        private WriteableBitmap colorBmap;              //Declares place to store color bitmap
        private DepthImagePixel[] depthPixels;          //Declares place to store depth data
        private byte[] colorPixels;                     //Declares place to store color data

        public MainWindow()
        {
            InitializeComponent();
        }

        //Stream Click Event Handler
        private void Stream_Click(object sender, RoutedEventArgs e)
        {
            //Handling starting of service
            if (this.Stream.Content.ToString() == "Start")
            {
                foreach (var potentialSensor in KinectSensor.KinectSensors)
                {
                    //Checks for a connected Kinect
                    if (potentialSensor.Status == KinectStatus.Connected)
                    {
                        this.ksensor = potentialSensor;
                        break;
                    }
                }

                if (null != this.ksensor)
                {
                    // Turn on the depth stream
                    this.ksensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                    // Allocate space to put the depth pixels
                    this.depthPixels = new DepthImagePixel[this.ksensor.DepthStream.FramePixelDataLength];

                    // This is the bitmap that will display
                    this.colorBmap = new WriteableBitmap(this.ksensor.DepthStream.FrameWidth, this.ksensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                    // Set the image we display to point to the bitmap where we'll put the image data
                    this.Depthstream.Source = this.colorBmap;

                    // Event handler to be called whenever there is new depth frame data
                    this.ksensor.DepthFrameReady += this.Ksensor_DepthFrameReady;

                    // Turn on the color stream
                    this.ksensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                    // Allocate space to put the color pixels
                    this.colorPixels = new byte[this.ksensor.DepthStream.FramePixelDataLength * sizeof(int)];

                    // This is the bitmap that will display
                    this.colorBmap = new WriteableBitmap(this.ksensor.DepthStream.FrameWidth, this.ksensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                    // Set the image we display to point to the bitmap where we'll put the image data
                    this.Colorstream.Source = this.colorBmap;

                    // Event handler to be called whenever there is new depth frame data
                    this.ksensor.ColorFrameReady += this.Ksensor_ColorFrameReady;

                    // Start the sensor
                    try
                    {
                        this.ksensor.Start();
                        this.Stream.Content = ("Stop");
                    }
                    catch (IOException)
                    {
                        this.ksensor = null;
                    }
                }

                if (null == this.ksensor){}
            }

            //Handling stopping of service
            else if (this.Stream.Content.ToString() == "Stop")
            {
                if (this.ksensor != null)
                {
                    this.ksensor.Stop();
                    this.Stream.Content = ("Start");
                }
                else{}
            }
        }

        //Gets color frame from kinect
        private void Ksensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            ColorImageFrame colorFrame = e.OpenColorImageFrame();
            BitmapSource bmap = ImageToBitmap(colorFrame);
            Colorstream.Source = bmap;
            Processedstream.Source = Imageprocessing(bmap);
        }

        //Image Processing
        BitmapSource Imageprocessing(BitmapSource Image)
        {
            //Checks to see if there is an image
            if (Image != null)
            {
                //Converts to image<>
                MemoryStream Stream = new MemoryStream();
                BitmapEncoder encoded = new BmpBitmapEncoder();
                encoded.Frames.Add(BitmapFrame.Create(Image));
                encoded.Save(Stream);
                System.Drawing.Bitmap myBmp = new System.Drawing.Bitmap(Stream);            //Casts image to bitmap
                Image<Hsv, Byte> processed = new Image<Hsv, Byte>(myBmp);                   //Casts bitmap to image<Hsv, byte>

                //Main processing
                CvInvoke.Flip(processed, processed, Emgu.CV.CvEnum.FlipType.Horizontal);    //Flips the image in the horizontal
                Image<Gray,Byte> Thr1, Thr2;                                                //Creates two Grayscale images that will be used when segmenting
                Thr1 = processed.InRange(new Hsv(0, 120, 70), new Hsv(10, 255, 255));       //Handles first range for RED
                Thr2 = processed.InRange(new Hsv(170, 120, 70), new Hsv(180, 255, 255));    //Handles second range for RED
                Thr1 = Thr1 + Thr2;

                //Handles noise and cleans image
                Mat kernel = Mat.Ones(3, 3, Emgu.CV.CvEnum.DepthType.Cv32F, 1);             //Creates 3x3 kernel for use as kernel
                CvInvoke.MorphologyEx(Thr1, Thr1, Emgu.CV.CvEnum.MorphOp.Open, kernel, new System.Drawing.Point(0,0),1,Emgu.CV.CvEnum.BorderType.Default,new MCvScalar(1));
                CvInvoke.MorphologyEx(Thr1, Thr1, Emgu.CV.CvEnum.MorphOp.Dilate, kernel, new System.Drawing.Point(0, 0), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(1));

                //Extracts only RED parts from orignal image
                Mat Mask;                                                                  //Creates Mat for converting mask to Mat
                Mask = Thr1.Mat;                                                           //Casts mask to Mat
                Image<Hsv, byte> Final = new Image<Hsv, byte>(processed.Width,processed.Height);    //Creates Image<Hsv,byte> for final processed image
                CvInvoke.BitwiseAnd(processed, processed, Final,Mask);                     //ANDS mask with orignal image to retain only portions that are RED

                //Cleanup
                Mask.Dispose();
                Thr1.Dispose();
                Thr2.Dispose();

                return BitmapSourceConvert.ToBitmapSource(Final);                          //Returns processed image
            }
            else {return null;}
        }

        //Converts image to bitmap
        BitmapSource ImageToBitmap(ColorImageFrame Image)
        {
            if (Image != null)
            {
                Image.CopyPixelDataTo(this.colorPixels);
                BitmapSource bmap = BitmapSource.Create(Image.Width, Image.Height, 10, 10, PixelFormats.Bgr32, null, this.colorPixels, Image.Width * Image.BytesPerPixel);
                Image.Dispose();
                return bmap;
            }
            else {
                return null;
            }
        }

        //Gets depth info from kinect and casts to a bitmap
        private void Ksensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            DepthImageFrame depthFrame = e.OpenDepthImageFrame();   //Puts Depthframe into Depthframe

            //Checks if there is a depthFrame
            if (depthFrame != null)
            {
                // Copy the pixel data from the image to a temporary array
                depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                // Get the min and max reliable depth for the current frame
                int minDepth = depthFrame.MinDepth;
                int maxDepth = depthFrame.MaxDepth;

                //Convert depth data to bitmapsource
                short[] pixelData = new short[depthFrame.PixelDataLength];
                depthFrame.CopyPixelDataTo(pixelData);
                BitmapSource bmap = BitmapSource.Create(
                    depthFrame.Width,
                    depthFrame.Height,
                    2, 2,
                    PixelFormats.Gray16, null,
                    pixelData,
                    depthFrame.Width * depthFrame.BytesPerPixel);

                //Set stream to image
                Depthstream.Source = bmap;

                //Cleanup
                depthFrame.Dispose();
            }
        }

        //Handles closing of the window
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (ksensor != null)                    //Checks if there is a kinect connected
            {
                this.ksensor.Stop();                //Stops the kinect sensor
            }
            else{}
        }
    }
}
