using System;
using System.ComponentModel;
// Master Version 3
// Master Version2

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Variables
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for depth frames
        /// </summary>
        private DepthFrameReader _depthFrameReader;

        /// <summary>
        /// Description of the data contained in the depth frame
        /// </summary>
        private readonly FrameDescription _depthFrameDescription;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private readonly WriteableBitmap _depthBitmap;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private readonly byte[] _depthPixels;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string _statusText;

        /// <summary>
        /// Constant defining the flag for left click down
        /// </summary>
        private const int MouseeventfLeftdown = 0x0002;

        /// <summary>
        /// Constant defining the flag for left click up
        /// </summary>
        private const int MouseeventfLeftup = 0x0004;

        /// <summary>
        /// Constant defining the flag for mouse movement
        /// </summary>
        private const int MouseeventfMove = 0x0001;

        /// <summary>
        /// Constant defining the flag for making mouse_event move in an absolute manner
        /// </summary>
        private const int MouseeventfAbsolute = 0x8000;

        /// <summary>
        /// Width resolution of the Kinect Sensor
        /// </summary>
        private const int KinectWidth = 512;

        /// <summary>
        /// Height resolution of the Kinect Sensor
        /// </summary>
        private const int KinectHeight = 424;

        /// <summary>
        /// Distance the right edge of the screen is from the sensor in meters
        /// </summary>
        private float _rightOfScreen = 0.7f;

        /// <summary>
        /// Distance the left edge of the screen is from the sensor in meters
        /// </summary>
        private float _leftOfScreen = 0.6f;

        /// <summary>
        /// Distance the top edge of the screen is from the sensor in meters
        /// </summary>
        private float _topOfScreen = 0.19f;

        /// <summary>
        /// Distance the bottom edge of the screen is from the sensor in meters
        /// </summary>
        private float _bottomOfScreen = -0.11f;

        /// <summary>
        /// Previous distance the right edge of the screen is from the sensor in meters, used for calibration
        /// </summary>
        private float _oldRightOfScreen = 0.7f;

        /// <summary>
        /// Previous distance the left edge of the screen is from the sensor in meters, used for calibration
        /// </summary>
        private float _oldLeftOfScreen = 0.6f;

        /// <summary>
        /// Previous distance the top edge of the screen is from the sensor in meters, used for calibration
        /// </summary>
        private float _oldTopOfScreen = 0.19f;

        /// <summary>
        /// Previous distance the bottom edge of the screen is from the sensor in meters, used for calibration
        /// </summary>
        private float _oldBottomOfScreen = -0.11f;

        /// <summary>
        /// Distance from screen before moving the mouse in meters
        /// </summary>
        private float _mouseMoveThreshold = 0.12f;

        /// <summary>
        /// Distance from screen before registering a left click down in meters
        /// </summary>
        private float _mouseDownThreshold = 0.003f;

        /// <summary>
        /// Distance from screen before registering a left click up in meters
        /// </summary>
        private float _mouseUpThreshold = 0.006f;

        /// <summary>
        /// Determines if the mouse can be moved. 0 = no movement, 1 = movemement, 2 = movement and clicking
        /// </summary>
        private int _mouseAllowed = 1;

        /// <summary>
        /// Determines if the program is in which calibration mode
        /// </summary>
        private int _calibrateStatus = 0;

        /// <summary>
        /// Storage of previous calibration values
        /// </summary>
        CameraSpacePoint[] _calibratePoints = new CameraSpacePoint[KinectWidth * KinectHeight];
        
        /// <summary>
        /// Storage for moving average of X values
        /// </summary>
        private int[] _prevMouseX = { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};

        /// <summary>
        /// Storage for moving average of Y values
        /// </summary>
        private int[] _prevMouseY = { 0,0,0,0,0,0,0,0,0 };

        /// <summary>
        /// Variable describing status of mouse. 0 = no finger, 1 = move only, 2 = click only, 3 = click and drag
        /// </summary>
        private int _mouseStatus = 0;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double confidenceThreshold = 0.3;

            if (e.Result.Confidence >= confidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                     
                    case "CALIBRATE":
                        _oldBottomOfScreen = _bottomOfScreen;
                        _oldLeftOfScreen = _leftOfScreen;
                        _oldRightOfScreen = _rightOfScreen;
                        _oldTopOfScreen = _topOfScreen;
                        InfoLabel2.Content = "Calibrating...";
                        if (_calibrateStatus == 0)
                        {
                            _calibrateStatus = 1;
                        }
                        _mouseStatus = 1;
                        _mouseAllowed = 0;
                        break;
                    case "CANCELCALIBRATE":
                        _bottomOfScreen = _oldBottomOfScreen;
                        _rightOfScreen = _oldRightOfScreen;
                        _leftOfScreen = _oldLeftOfScreen;
                        _topOfScreen = _oldTopOfScreen;
                        _calibrateStatus = 0;
                        _mouseStatus = 1;
                        InfoLabel2.Content = "";
                        CalibrateButton.Content = "Calibration Canceled.";
                        _mouseAllowed = 1;
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // initialize the components (controls) of the window
            InitializeComponent();

            // get the kinectSensor object
            _kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            _depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            _depthFrameReader.FrameArrived += Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            _depthFrameDescription = _kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            _depthPixels = new byte[_depthFrameDescription.Width * _depthFrameDescription.Height];

            // create the bitmap to display
            _depthBitmap = new WriteableBitmap(_depthFrameDescription.Width, _depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // set IsAvailableChanged event notifier
            _kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // open the sensor
            _kinectSensor.Open();

            // grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = _kinectSensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // create the convert stream
            convertStream = new KinectAudioStream(audioStream);

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                speechEngine = new SpeechRecognitionEngine(ri.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("Kinect Calibrate", "CALIBRATE"));
                directions.Add(new SemanticResultValue("Kinect Cancel", "CANCELCALIBRATE"));

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);
                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);
                
                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // let the convertStream know speech is going active
                convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                //speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                speechEngine.SetInputToAudioStream(convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }

            // set the status text
            StatusText = _kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.NoSensorStatusText;

            CalibrateButton.Content = "Click Me For Easy Calibrate";
            
            InfoLabel.Content = "Hello World";
            // use the window object as the view model in this simple example
            DataContext = this;

            for (int i = 0; i < _prevMouseX.Length/sizeof (int); i++)
            {
                _prevMouseX[i] = 0;
            }
        }

        //Cursor Control events
        //[DllImport("user32")]
        //public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return _depthBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return _statusText;
            }

            set
            {
                if (_statusText != value)
                {
                    _statusText = value;

                    // notify any bound elements that the text has changed
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (null != convertStream)
            {
                convertStream.SpeechActive = false;
            }

            if (null != speechEngine)
            {
                speechEngine.SpeechRecognized -= SpeechRecognized;
                speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                speechEngine.RecognizeAsyncStop();
            }

            if (_depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                _depthFrameReader.Dispose();
                _depthFrameReader = null;
            }

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((_depthFrameDescription.Width * _depthFrameDescription.Height) == (depthBuffer.Size / _depthFrameDescription.BytesPerPixel)) &&
                            (_depthFrameDescription.Width == _depthBitmap.PixelWidth) && (_depthFrameDescription.Height == _depthBitmap.PixelHeight))
                        {
                            ConvertProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
        }

        /// <summary>
        /// This function takes in the depth frame, converts it to camera space, and looks for the user finger.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the raw depth buffer</param>
        /// <param name="depthFrameDataSize">How large the depth fram is. should be KinectHeight*KinectWidth</param>
        private unsafe void ConvertProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize)
        {
            ushort* frameData = (ushort*)depthFrameData;

            WallTopLabel.Content = "Top Wall M: " + _topOfScreen;
            WallLeftLabel.Content = "Left Wall M: " + _leftOfScreen;
            WallRightLabel.Content = "Right Wall M: " + _rightOfScreen;
            WallBottomLabel.Content = "Bottom Wall M: " + _bottomOfScreen;

            #region SetupCanvasFrame
            
            for (int y = 0; y < KinectHeight; y++)
            {
                if (y == KinectHeight/2)
                {
                    for (int x = 0; x < KinectWidth; x++)
                    {
                        _depthPixels[y * KinectWidth + x] = (255);
                    }
                }
                else
                {
                    for (int x = 0; x < KinectWidth; x++)
                    {
                        ushort depth = frameData[y * KinectWidth + x];
                        _depthPixels[y * KinectWidth + x] = (byte)(depth >= 500 && depth <= 5000 ? (depth *256 / 5000) : 0);
                    }
                }
            }

            #endregion

            CoordinateMapper m = _kinectSensor.CoordinateMapper;
            ushort[] frameUshorts = new ushort[depthFrameDataSize/sizeof(ushort)];
            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                frameUshorts[i] = frameData[i];
            }
            CameraSpacePoint[] spacePoints = new CameraSpacePoint[depthFrameDataSize / sizeof(ushort)]; // X,Y,Z in terms of the camera, not the user
            m.MapDepthFrameToCameraSpace(frameUshorts, spacePoints);

            // now spacePoints contains a 3d virtualisation of where everything is.

            CameraSpacePoint userPoint = new CameraSpacePoint();
            userPoint.X = 0.0f;
            userPoint.Y = 0.0f;
            userPoint.Z = 0.0f;

            if (_calibrateStatus == 0)
            {
                // we aren't in calibration mode now.

                userPoint.Y = _mouseMoveThreshold;
                for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
                {
                    if (0 < spacePoints[i].Y && spacePoints[i].Y < userPoint.Y
                        && _bottomOfScreen < spacePoints[i].X && spacePoints[i].X < _topOfScreen
                        && _leftOfScreen < spacePoints[i].Z && spacePoints[i].Z < _rightOfScreen)
                    {
                        userPoint.X = spacePoints[i].X;
                        userPoint.Y = spacePoints[i].Y;
                        userPoint.Z = spacePoints[i].Z;
                    }
                }

                // found our point now?
                if (userPoint.X.Equals(0) && userPoint.Y.Equals(_mouseMoveThreshold) && userPoint.Z.Equals(0))
                {
                    // no point found
                    _mouseStatus = 0;
                }
                else
                {
                    if (_mouseStatus == 0)
                    {
                        _mouseStatus = 1;
                    }
                    
                    ProcessGesture(userPoint.X, userPoint.Y, userPoint.Z);
                }

                InfoLabel.Content = "X: " + userPoint.Z + "\nY: " + userPoint.X + "\nZ: " + userPoint.Y;
            }
            else if (_calibrateStatus == 1)
            {
                _mouseAllowed = 0;
                _mouseStatus = 1;
                // creating reference frame
                for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
                {
                    _calibratePoints[i].X = spacePoints[i].X;
                    _calibratePoints[i].Y = spacePoints[i].Y;
                    _calibratePoints[i].Z = spacePoints[i].Z;
                }
                CalibrateButton.Content = "Touch right edge of screen";
                _calibrateStatus = 3;
            }
            else if (_calibrateStatus == 3 || _calibrateStatus == 4)
            {
                // getting right coordinates
                userPoint.Y = _mouseMoveThreshold;
                userPoint.Z = 4.0f;
                _mouseAllowed = 0;
                float tolerance = 0.1f;
                for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
                {
                    if (0 < spacePoints[i].Y && spacePoints[i].Y < userPoint.Y && spacePoints[i].X > -0.1f && spacePoints[i].X < 0.1f && spacePoints[i].Z > 0.5f && spacePoints[i].Z < 8.0f)
                    {
                        if(spacePoints[i].X.Equals(float.NegativeInfinity) || spacePoints[i].X.Equals(float.PositiveInfinity)
                                || spacePoints[i].Y.Equals(float.NegativeInfinity) || spacePoints[i].Y.Equals(float.PositiveInfinity)
                                || spacePoints[i].Z.Equals(float.NegativeInfinity) || spacePoints[i].Z.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].X.Equals(float.NegativeInfinity) || _calibratePoints[i].X.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].Y.Equals(float.NegativeInfinity) || _calibratePoints[i].Y.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].Z.Equals(float.NegativeInfinity) || _calibratePoints[i].Z.Equals(float.PositiveInfinity))
                        {
                            // for this point, the new pixel is unknown. As the point we are looking for isnt unknown, we don't want to use them
                        }
                        else if ((_calibratePoints[i].X - spacePoints[i].X) < tolerance && (_calibratePoints[i].X - spacePoints[i].X) > -1 * tolerance
                            && (_calibratePoints[i].Y - spacePoints[i].Y) < tolerance && (_calibratePoints[i].Y - spacePoints[i].Y) > -1 * tolerance
                            && (_calibratePoints[i].Z - spacePoints[i].Z) < tolerance && (_calibratePoints[i].Z - spacePoints[i].Z) > -1 * tolerance)
                        {
                            // this point is the same as in the reference. We can ignore this piece of data
                        }
                        else
                        {
                            userPoint.X = spacePoints[i].X;
                            userPoint.Y = spacePoints[i].Y;
                            userPoint.Z = spacePoints[i].Z;
                        }
                    }
                }

                if (userPoint.X.Equals(0) && userPoint.Y.Equals(_mouseMoveThreshold) && userPoint.Z.Equals(0))
                {
                    // no point has been found
                }
                else
                {
                    InfoLabel.Content = "X: " + userPoint.Z + "\nY: " + userPoint.X + "\nZ: " + userPoint.Y;
                    if (_calibrateStatus == 3)
                    {
                        // right side
                        _rightOfScreen = userPoint.Z;

                        if (userPoint.Y < _mouseDownThreshold && _mouseStatus == 1)
                        {
                            _mouseStatus = 2;

                        }
                        if (userPoint.Y > _mouseUpThreshold && _mouseStatus == 2)
                        {
                            _mouseStatus = 1;
                            CalibrateButton.Content = "Touch left edge of screen";
                            
                            _calibrateStatus = 4;
                        }
                    }
                    else
                    {
                        _leftOfScreen = userPoint.Z;
                        if (userPoint.Y < _mouseDownThreshold && _mouseStatus == 1)
                        {
                            _mouseStatus = 2;

                        }
                        if (userPoint.Y > _mouseUpThreshold && _mouseStatus == 2)
                        {
                            _mouseStatus = 1;
                            CalibrateButton.Content = "Touch top edge of screen";
                            _calibrateStatus = 5;
                        }
                    }
                    
                }
            }
            else if (_calibrateStatus == 5 || _calibrateStatus == 6)
            {
                userPoint.Y = _mouseMoveThreshold;
                _mouseAllowed = 0;
                float tolerance = 0.1f;
                for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
                {
                    if (0 < spacePoints[i].Y && spacePoints[i].Y < userPoint.Y && spacePoints[i].Z > _leftOfScreen && spacePoints[i].Z < _rightOfScreen)
                    {
                        if (spacePoints[i].X.Equals(float.NegativeInfinity) || spacePoints[i].X.Equals(float.PositiveInfinity)
                                || spacePoints[i].Y.Equals(float.NegativeInfinity) || spacePoints[i].Y.Equals(float.PositiveInfinity)
                                || spacePoints[i].Z.Equals(float.NegativeInfinity) || spacePoints[i].Z.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].X.Equals(float.NegativeInfinity) || _calibratePoints[i].X.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].Y.Equals(float.NegativeInfinity) || _calibratePoints[i].Y.Equals(float.PositiveInfinity)
                                || _calibratePoints[i].Z.Equals(float.NegativeInfinity) || _calibratePoints[i].Z.Equals(float.PositiveInfinity))
                        {
                            // for this point, the new pixel is unknown. As the point we are looking for isnt unknown, we don't want to use them
                        }
                        else if ((_calibratePoints[i].X - spacePoints[i].X) < tolerance && (_calibratePoints[i].X - spacePoints[i].X) > -1 * tolerance
                            && (_calibratePoints[i].Y - spacePoints[i].Y) < tolerance && (_calibratePoints[i].Y - spacePoints[i].Y) > -1 * tolerance
                            && (_calibratePoints[i].Z - spacePoints[i].Z) < tolerance && (_calibratePoints[i].Z - spacePoints[i].Z) > -1 * tolerance)
                        {
                            // this point is the same as in the reference. We can ignore this piece of data
                        }
                        else
                        {
                            userPoint.X = spacePoints[i].X;
                            userPoint.Y = spacePoints[i].Y;
                            userPoint.Z = spacePoints[i].Z;
                        }
                    }
                }

                if (userPoint.X.Equals(0) && userPoint.Y.Equals(_mouseMoveThreshold) && userPoint.Z.Equals(0))
                {
                    // no point has been found
                }
                else
                {
                    InfoLabel.Content = "X: " + userPoint.Z + "\nY: " + userPoint.X + "\nZ: " + userPoint.Y;
                    if (_calibrateStatus == 5)
                    {
                        _topOfScreen = userPoint.X;
                        if (userPoint.Y < _mouseDownThreshold && _mouseStatus == 1)
                       {
                            _mouseStatus = 2;

                        }
                        if (userPoint.Y > _mouseUpThreshold && _mouseStatus == 2)
                        {
                            _mouseStatus = 1;
                            CalibrateButton.Content = "Touch bottom edge of screen";
                            _calibrateStatus = 6;
                        }
                    }
                    else
                    {
                        _bottomOfScreen = userPoint.X;

                        if (userPoint.Y < _mouseDownThreshold && _mouseStatus == 1)
                        {
                            _mouseStatus = 2;

                        }
                        if (userPoint.Y > _mouseUpThreshold && _mouseStatus == 2)
                        {
                            _mouseStatus = 1;
                            CalibrateButton.Content = "Finished. Click here to recalibrate";
                            InfoLabel2.Content = "";
                            _mouseAllowed = 1;
                            _calibrateStatus = 0;
                        }
                    }

                }
            }

        }


        /// <summary>
        /// This method takes in the coordinates of the detected finger in CAMERA space, and converts it into mouse coordinates, as well as deciding if a mouse click/grag should occur
        /// </summary>
        /// <param name="spaceX">X coordinate in CAMERA space (Y in userspace)</param>
        /// <param name="spaceY">Y coordinate in CAMERA space (Z in userspace)</param>
        /// <param name="spaceZ">Z coordinate in CAMERA space (X in userspace)</param>
        private void ProcessGesture(float spaceX, float spaceY, float spaceZ)
        {
            float width = _rightOfScreen - _leftOfScreen;
            float height = _topOfScreen - _bottomOfScreen;

            int myX = (int)(Convert.ToDouble((spaceZ - _leftOfScreen) * 65535) / width);
            int myY = (int)(Convert.ToDouble((_topOfScreen - spaceX) * 65535) / height);




            if (_mouseStatus == 1)
            {
                // cursor move mode only
                
                int oldValueX = 0;
                for (int i = 0; i < _prevMouseX.Length / sizeof(int); i++)
                {
                    oldValueX += _prevMouseX[i];
                }

                int oldValueY = 0;
                for(int i = 0; i < _prevMouseY.Length/sizeof(int);i++)
                {
                    oldValueY += _prevMouseY[i];
                }


                if (spaceY < _mouseDownThreshold && _mouseAllowed == 2)
                {
                    // left mouse button has gone down
                    mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftdown,
                        ((oldValueX + myX)/(_prevMouseX.Length/sizeof (int) + 1)),
                        ((oldValueY + myY)/(_prevMouseY.Length/sizeof (int) + 1)), 0, 0);
                    _mouseStatus = 2;
                    
                }
                else
                {
                    if (_mouseAllowed != 0)
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);
                        
                    }
                    
                    for (int i = _prevMouseX.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseX[i] = _prevMouseX[i - 1];
                    }
                    for (int i = _prevMouseY.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseY[i] = _prevMouseY[i - 1];
                    }
                    _prevMouseX[0] = myX;
                    _prevMouseY[0] = myY;
                }
            }
            else if (_mouseStatus == 2)
            {
                // user has just pressed down
                // dont move cursor until it has moved a certain distance away
                
                double tempDistance = Math.Sqrt((myX - _prevMouseX[0]) * (myX - _prevMouseX[0]) + (myY - _prevMouseY[0]) * (myY - _prevMouseY[0]));

                InfoLabel.Content = tempDistance;
                
                if (tempDistance > 3000)
                {
                    // if distance moved has moved beyond a certain threshold, then the user has intended to click and drag
                    _mouseStatus = 3;
                }
            }
            else if (_mouseStatus == 3)
            {
                // user has pressed down and dragged at the same time
                int oldValueX = 0;
                for (int i = 0; i < _prevMouseX.Length / sizeof(int); i++)
                {
                    oldValueX += _prevMouseX[i];
                }

                int oldValueY = 0;
                for (int i = 0; i < _prevMouseY.Length / sizeof(int); i++)
                {
                    oldValueY += _prevMouseY[i];
                }

                if (spaceY > _mouseUpThreshold)
                {

                    mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftup,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);

                    
                    
                    _mouseStatus = 1;
                }
                else
                {
                    mouse_event(MouseeventfAbsolute | MouseeventfMove,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);

                    for (int i = _prevMouseX.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseX[i] = _prevMouseX[i - 1];
                    }
                    for (int i = _prevMouseY.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseY[i] = _prevMouseY[i - 1];
                    }

                    _prevMouseX[0] = myX;
                    _prevMouseY[0] = myY;
                }
            }

        }



        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            _depthBitmap.WritePixels(
                new Int32Rect(0, 0, _depthBitmap.PixelWidth, _depthBitmap.PixelHeight),
                _depthPixels,
                _depthBitmap.PixelWidth,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            StatusText = _kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }


        #region ButtonActions

        /// <summary>
        /// Handles the event where the user clicks on WallTopUp
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallTop_ClickUp(object sender, RoutedEventArgs e)
        {
            _topOfScreen += 0.01f;
            WallTopLabel.Content = "Top Wall M: " + _topOfScreen;

        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopDown
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallTop_ClickDown(object sender, RoutedEventArgs e)
        {
            _topOfScreen -= 0.01f;
            WallTopLabel.Content = "Top Wall M: " + _topOfScreen;
            
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftLeft
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallLeft_ClickLeft(object sender, RoutedEventArgs e)
        {
            _leftOfScreen -= 0.01f;
            WallLeftLabel.Content = "Left Wall M: " + _leftOfScreen;

        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftRight
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallLeft_ClickRight(object sender, RoutedEventArgs e)
        {
            _leftOfScreen += 0.01f;
            WallLeftLabel.Content = "Left Wall M: " + _leftOfScreen;
            
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightLeft
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallRight_ClickLeft(object sender, RoutedEventArgs e)
        {
            _rightOfScreen -= 0.01f;
            WallRightLabel.Content = "Right Wall M: " + _rightOfScreen;
            
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightRight
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallRight_ClickRight(object sender, RoutedEventArgs e)
        {
            _rightOfScreen += 0.01f;
            WallRightLabel.Content = "Right Wall M: " + _rightOfScreen;
            
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomUp
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallBottom_ClickUp(object sender, RoutedEventArgs e)
        {
            _bottomOfScreen += 0.01f;
            WallBottomLabel.Content = "Bottom Wall M: " + _bottomOfScreen;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomDown
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WallBottom_ClickDown(object sender, RoutedEventArgs e)
        {
            _bottomOfScreen -= 0.01f;
            WallBottomLabel.Content = "Bottom Wall M: " + _bottomOfScreen;
        }

        #endregion


    }
}
