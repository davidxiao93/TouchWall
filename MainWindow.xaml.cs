using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace TouchWall
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Variables

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _kinectSensor;

        /// <summary>
        /// Calibrator, in charge of knowing where edges of screen are
        /// </summary>
        private readonly Screen _screen;

        /// <summary>
        /// Collects and processes frame data from sensor
        /// </summary>
        private readonly FrameDataManager _frameDataManager;

        /// <summary>
        /// Turns gestures into mouse movements
        /// </summary>
        private readonly TouchGestureHandler _gestures;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private readonly KinectAudioStream _convertStream;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private readonly SpeechRecognitionEngine _speechEngine;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string _statusText;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
        
        public MainWindow()
        {
            // Initialize the components (controls) of the window
            InitializeComponent();

            // Get the _kinectSensor object
            _kinectSensor = KinectSensor.GetDefault();

            // Create the screen for calibration
            _screen = new Screen();
            
            // Create a gesture handler to handle touch gestures on the screen
            _gestures = new TouchGestureHandler();

            // Create a manager for processing sensor data from the kinect
            _frameDataManager = new FrameDataManager(_kinectSensor, _screen, _gestures);

            // Open the sensor
            _kinectSensor.Open();

            // Wire handler for frame arrival
            _frameDataManager.DepthFrameReader.FrameArrived += _frameDataManager.Reader_FrameArrived;

            // Update the UI on each frame
            _frameDataManager.DepthFrameReader.FrameArrived += UpdateUI;

            // Set IsAvailableChanged event notifier
            _kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            DataContext = this;

            #region Speech

            // Grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = _kinectSensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // Create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            RecognizerInfo recognizerInfo = TryGetKinectRecognizer();

            if (recognizerInfo != null)
            {
                _speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("Kinect Calibrate", "CALIBRATE"));
                directions.Add(new SemanticResultValue("Kinect Cancel", "CANCELCALIBRATE"));

                var gb = new GrammarBuilder { Culture = recognizerInfo.Culture };
                gb.Append(directions);
                var g = new Grammar(gb);
                _speechEngine.LoadGrammar(g);
                
                _speechEngine.SpeechRecognized += SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // Let the convertStream know speech is going active
                _convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                //speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                _speechEngine.SetInputToAudioStream(_convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }

            #endregion
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_convertStream != null)
            {
                _convertStream.SpeechActive = false;
            }

            if (_speechEngine != null)
            {
                _speechEngine.SpeechRecognized -= SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                _speechEngine.RecognizeAsyncStop();
            }

            _frameDataManager.DisposeDepthFrameReader();

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }
        }

        /// <summary>
        /// Changes the status text when the sensor is available/unavailable (e.g. paused, closed, unplugged)
        /// </summary>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            StatusText = _kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get { return FrameDataManager.DepthBitmap; }
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
                    if (PropertyChanged != null) // Notify any bound elements that the text has changed
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Update the UI on each frame
        /// </summary>
        private void UpdateUI(object sender, DepthFrameArrivedEventArgs e)
        {
            WallTopLabel.Content = "Top Wall M: " + _screen.TopEdge;
            WallLeftLabel.Content = "Left Wall M: " + _screen.LeftEdge;
            WallRightLabel.Content = "Right Wall M: " + _screen.RightEdge;
            WallBottomLabel.Content = "Bottom Wall M: " + _screen.BottomEdge;

            InfoLabel.Content = "X: " + _frameDataManager.Frame.UserPoint.Z + "\nY: " + _frameDataManager.Frame.UserPoint.X + "\nZ: " + _frameDataManager.Frame.UserPoint.Y;

            switch (_screen.CalibrateStatus)
            {
                case 1:
                    InfoLabel2.Content = "Calibrating...";
                    CalibrateButton.Content = "Calibrating...";
                    break;
                case 3:
                    CalibrateButton.Content = "Touch right edge of screen";
                    break;
                case 4:
                    CalibrateButton.Content = "Touch Left edge of screen";
                    break;
                case 5:
                    CalibrateButton.Content = "Touch top edge of screen";
                    break;
                case 6:
                    CalibrateButton.Content = "Touch bottom edge of screen";
                    break;
                default:
                    CalibrateButton.Content = "Click Me For Easy Calibrate";
                    InfoLabel2.Content = "";
                    break;
            }
        }

        #region ButtonActions

        /// <summary>
        /// Handles the event where the user clicks on WallTopUp
        /// </summary>
        private void WallTop_ClickUp(object sender, RoutedEventArgs e)
        {
            _screen.TopEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopDown
        /// </summary>
        private void WallTop_ClickDown(object sender, RoutedEventArgs e)
        {
            _screen.TopEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftLeft
        /// </summary>
        private void WallLeft_ClickLeft(object sender, RoutedEventArgs e)
        {
            _screen.LeftEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftRight
        /// </summary>
        private void WallLeft_ClickRight(object sender, RoutedEventArgs e)
        {
            _screen.LeftEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightLeft
        /// </summary>
        private void WallRight_ClickLeft(object sender, RoutedEventArgs e)
        {
            _screen.RightEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightRight
        /// </summary>
        private void WallRight_ClickRight(object sender, RoutedEventArgs e)
        {
            _screen.RightEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomUp
        /// </summary>
        private void WallBottom_ClickUp(object sender, RoutedEventArgs e)
        {
            _screen.BottomEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomDown
        /// </summary>
        private void WallBottom_ClickDown(object sender, RoutedEventArgs e)
        {
            _screen.BottomEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on CalibrateButton
        /// </summary>
        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            _screen.BeginCalibration();
            _gestures.MouseStatus = 1;
            _gestures.MouseAllowed = 0;
        }

        #endregion

        #region Speech2

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to process audio from Kinect
        /// </summary>
        /// <returns>
        /// <code>RecognizerInfo</code> if found, <code>null</code> otherwise.
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
                        _screen.BeginCalibration();
                        _gestures.MouseStatus = 1;
                        _gestures.MouseAllowed = 0;
                        break;
                    case "CANCELCALIBRATE":
                        _screen.CancelCalibration();
                        _gestures.MouseStatus = 1;
                        _gestures.MouseAllowed = 1;
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
    }

        #endregion
}
