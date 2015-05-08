using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace TouchWall
{
    public class FrameDataManager
    {
        /// <summary>
        /// Reader for depth frames
        /// </summary>
        private DepthFrameReader _depthFrameReader;
        
        /// <summary>
        /// Description of the data contained in depth frame
        /// </summary>
        private readonly FrameDescription _depthFrameDescription;
        
        /// <summary>
        /// Bitmap to display
        /// </summary>
        private static WriteableBitmap _depthBitmap;

        /// <summary>
        /// Frame object to hold values for current frame
        /// </summary>
        private Frame _frame;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public FrameDataManager()
        {
            // Open the reader for the depth frames
            _depthFrameReader = TouchWallApp.KinectSensor.DepthFrameSource.OpenReader();

            // Get FrameDescription from DepthFrameSource
            _depthFrameDescription = TouchWallApp.KinectSensor.DepthFrameSource.FrameDescription;

            // Create the bitmap to display
            _depthBitmap = new WriteableBitmap(_depthFrameDescription.Width, _depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
        }

        /// <summary>
        /// Getter for the DepthFrameReader Object
        /// </summary>
        public DepthFrameReader DepthFrameReader
        {
            get { return _depthFrameReader; }
        }

        /// <summary>
        /// Getter for the DepthBitmap Object
        /// </summary>
        public static WriteableBitmap DepthBitmap
        {
            get { return _depthBitmap; }
        }

        /// <summary>
        /// Getter for the Frame Object
        /// </summary>
        public Frame Frame
        {
            get { return _frame; }
        }

        /// <summary>
        /// Kills the depthframereader.
        /// </summary>
        public void DisposeDepthFrameReader()
        {
            if (_depthFrameReader != null) // DepthFrameReader is IDisposable
            {
                _depthFrameReader.Dispose();
                _depthFrameReader = null;
            }
        }

        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        public void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // The fastest way to process the body index data is to directly access the underlying buffer
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // Verify data and write the colour data to the display bitmap
                        if (((_depthFrameDescription.Width * _depthFrameDescription.Height) == (depthBuffer.Size /_depthFrameDescription.BytesPerPixel))
                            && (_depthFrameDescription.Width == _depthBitmap.PixelWidth) && (_depthFrameDescription.Height == _depthBitmap.PixelHeight))
                        {
                            _frame = new Frame(depthBuffer.UnderlyingBuffer, depthBuffer.Size, _depthFrameDescription);
                            RenderDepthPixels();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Renders colour pixels into the WriteableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            _depthBitmap.WritePixels(new Int32Rect(0, 0, _depthBitmap.PixelWidth, _depthBitmap.PixelHeight),
                _frame.DepthPixels, _depthBitmap.PixelWidth, 0);
        }
    }
}