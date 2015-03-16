using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace TouchWall
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        /// <summary>
        /// TouchWallApp object, responsible for communicating with kinect and cursor
        /// </summary>
        private readonly TouchWallApp _touchWall;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string _statusText;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            // Initialize the components (controls) of the window
            InitializeComponent();

            _touchWall = new TouchWallApp();

            // Wire handler for frame arrival
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += _touchWall.FrameDataManager.Reader_FrameArrived;

            // Update the UI on each frame
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += UpdateLabels;

            // Set IsAvailableChanged event notifier
            TouchWallApp.KinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            DataContext = this;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _touchWall.DisableSpeech();
            _touchWall.FrameDataManager.DisposeDepthFrameReader();
            _touchWall.DisableKinect();
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get { return FrameDataManager.DepthBitmap; }
        }

        #region TextUpdates

        /// <summary>
        /// Changes the status text when the sensor is available/unavailable (e.g. paused, closed, unplugged)
        /// </summary>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            StatusText = TouchWallApp.KinectSensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get { return _statusText; }
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
        private void UpdateLabels(object sender, DepthFrameArrivedEventArgs e)
        {
            UpdateDimensionLabels();
            UpdateCoordaintesLabel();
            UpdateCursorStatusLabel();
            UpdateCalibrationLabels();
        }

        private void UpdateDimensionLabels()
        {
            WallTopLabel.Content = "Top Wall M: " + Screen.TopEdge;
            WallLeftLabel.Content = "Left Wall M: " + Screen.LeftEdge;
            WallRightLabel.Content = "Right Wall M: " + Screen.RightEdge;
            WallBottomLabel.Content = "Bottom Wall M: " + Screen.BottomEdge;
        }

        private void UpdateCoordaintesLabel()
        {
            if (TouchWallApp.MultiTouchMode == 1)
            {
                CoordinatesLabel.Content = "MultiTouch Mode Enabled";
            } 
            else if (_touchWall.FrameDataManager.Frame.Gestures[0] == null)
            {
                CoordinatesLabel.Content = "No points found";
            }
            else
            {
                CoordinatesLabel.Content = "X: " + _touchWall.FrameDataManager.Frame.Gestures[0].X
                    + "\nY: " + _touchWall.FrameDataManager.Frame.Gestures[0].Y
                    + "\nZ: " + _touchWall.FrameDataManager.Frame.Gestures[0].Z;
            }
        }

        private void UpdateCursorStatusLabel()
        {
            switch (TouchWallApp.CursorStatus)
            {
                case 1:
                    ToggleCursorButton.Content = "Curser Enabled Without Click - Click to toggle";
                    break;
                case 2:
                    ToggleCursorButton.Content = "Curser Enabled With Click - Click to toggle";
                    break;
                default:
                    ToggleCursorButton.Content = "Curser Disabled - Click to toggle";
                    break;
            }
        }

        private void UpdateCalibrationLabels()
        {
            switch (TouchWallApp.CalibrateStatus)
            {
                case 1:
                    CalibrateStatusLabel.Content = "Calibrating...";
                    CalibrateButton.Content = "Calibrating...";
                    break;
                case 2:
                    CalibrateButton.Content = "Touch right edge of screen";
                    break;
                case 3:
                    CalibrateButton.Content = "Touch Left edge of screen";
                    break;
                case 4:
                    CalibrateButton.Content = "Touch top edge of screen";
                    break;
                case 5:
                    CalibrateButton.Content = "Touch bottom edge of screen";
                    break;
                default:
                    CalibrateButton.Content = "Click Me For Easy Calibrate";
                    CalibrateStatusLabel.Content = "";
                    break;
            }
        }

        #endregion

        #region ButtonActions

        /// <summary>
        /// Handles the event where the user clicks on WallTopUp
        /// </summary>
        private void WallTop_ClickUp(object sender, RoutedEventArgs e)
        {
            Screen.TopEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopDown
        /// </summary>
        private void WallTop_ClickDown(object sender, RoutedEventArgs e)
        {
            Screen.TopEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftLeft
        /// </summary>
        private void WallLeft_ClickLeft(object sender, RoutedEventArgs e)
        {
            Screen.LeftEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftRight
        /// </summary>
        private void WallLeft_ClickRight(object sender, RoutedEventArgs e)
        {
            Screen.LeftEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightLeft
        /// </summary>
        private void WallRight_ClickLeft(object sender, RoutedEventArgs e)
        {
            Screen.RightEdge -= 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightRight
        /// </summary>
        private void WallRight_ClickRight(object sender, RoutedEventArgs e)
        {
            Screen.RightEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomUp
        /// </summary>
        private void WallBottom_ClickUp(object sender, RoutedEventArgs e)
        {
            Screen.BottomEdge += 0.01f;
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomDown
        /// </summary>
        private void WallBottom_ClickDown(object sender, RoutedEventArgs e)
        {
            Screen.BottomEdge -= 0.01f;
        }

        /// <summary>
        /// Enables or disables clicking with the cursor
        /// </summary>
        private void Toggle_MultiTouch(object sender, RoutedEventArgs e)
        {
            MultiTouchWindow multiTouchWindow = new MultiTouchWindow(_touchWall);
            multiTouchWindow.Show();
            TouchWallApp.CursorStatus = 0;
            TouchWallApp.MultiTouchMode = 1;
        }

        /// <summary>
        /// Handles the event where the user clicks on CalibrateButton
        /// </summary>
        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            _touchWall.BeginCalibration();
        }

        /// <summary>
        /// Enables or disables clicking with the cursor
        /// </summary>
        private void Toggle_Cursor(object sender, RoutedEventArgs e)
        {
            _touchWall.ToggleCursor();
        }

        #endregion
    }
}
