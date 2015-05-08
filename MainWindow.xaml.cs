using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Kinect;
using System.IO;

namespace TouchWall
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        /// <summary>
        /// TouchWallApp object, responsible for communicating with kinect and cursor
        /// </summary>
        private readonly TouchWallApp _touchWall;

        /// <summary>
        /// MultiTouchWindow object. can contain a reference the singleton
        /// </summary>
        private MultiTouchWindow _multiTouchWindow;

        /// <summary>
        /// DepthTouchWindow object. can contain a reference the singleton
        /// </summary>
        private DepthTouchWindow _depthTouchWindow;

        /// <summary>
        /// HelpWindow object. can contain a reference the singleton
        /// </summary>
        private HelpWindow _helpWindow;

        /// <summary>
        /// HelpWindow object. can contain a reference the singleton
        /// </summary>
        private SplashWindow _splashWindow;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            // Initialize the components (controls) of the window
            InitializeComponent();

            _touchWall = new TouchWallApp(this);

            // Wire handler for frame arrival
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += _touchWall.FrameDataManager.Reader_FrameArrived;

            // Update the UI on each frame
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += UpdateLabels;

            // Set IsAvailableChanged event notifier
            TouchWallApp.KinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            DataContext = this;

            UpdateAllLabels();
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _touchWall.DisableSpeech();
            _touchWall.FrameDataManager.DisposeDepthFrameReader();
            _touchWall.DisableKinect();
            if (_multiTouchWindow != null)
            {
                CloseMultiTouchWindow();
            }
            if (_depthTouchWindow != null)
            {
                CloseDepthTouchWindow();
            }
            if (_helpWindow != null)
            {
                CloseHelpWindow();
            }
            if (_splashWindow != null)
            {
                CloseSplashWindow();
            }

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
            UpdateStatusLabel();
        }

        /// <summary>
        /// Clears all UI labels when Kinect no longer avaliable
        /// </summary>
        private void ClearLabels()
        {
            WallTopLabel.Content = "";
            WallLeftLabel.Content = "";
            WallRightLabel.Content = "";
            WallBottomLabel.Content = "";
            CoordinatesLabel.Content = "";
            ToggleCursorButton.Content = "";
            CalibrateButton.Content = "";
            ToggleDepthTouchButton.Content = "";
            ToggleMultiTouchButton.Content = "";
            LaunchTouchdevelopButton.Content = "";
            LaunchTouchdevelopLocalButton.Content = "";
            LaunchKeyboardButton.Content = "";
            VoiceLabel.Content = "";
            ToggleVoiceButton.Content = "";
            DepthViewer.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Update the UI on each frame
        /// </summary>
        private void UpdateLabels(object sender, DepthFrameArrivedEventArgs e)
        {
            UpdateAllLabels();
        }

        /// <summary>
        /// Update the UI
        /// </summary>
        private void UpdateAllLabels()
        {
            UpdateDimensionLabels();
            UpdateCoordaintesLabel();
            UpdateCursorStatusLabel();
            UpdateCalibrationLabels();
            UpdateModeLabels();
            UpdateStatusLabel2();
            UpdateVoiceLabel();
            DepthViewer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Update the Calibration numbers
        /// </summary>
        private void UpdateDimensionLabels()
        {
            WallTopLabel.Content = Screen.TopEdge.ToString("0.00") + "m";
            WallLeftLabel.Content = Screen.LeftEdge.ToString("0.00") + "m";
            WallRightLabel.Content = Screen.RightEdge.ToString("0.00") + "m";
            WallBottomLabel.Content = Screen.BottomEdge.ToString("0.00") + "m";
        }

        /// <summary>
        /// Update the info labels
        /// </summary>
        private void UpdateCoordaintesLabel()
        {
            if (TouchWallApp.MultiTouchMode == 1)
            {
                CoordinatesLabel.Content = "Multi Mode Enabled";
            } 
            else if (TouchWallApp.MultiTouchMode == 2)
            {
                CoordinatesLabel.Content = "Depth Mode Enabled";
            }
            else
            {
                try
                {
                    if (_touchWall.FrameDataManager.Frame.Gestures[0] == null)
                    {
                        CoordinatesLabel.Content = "No points found";
                    }
                    else
                    {
                        CoordinatesLabel.Content = "X: " + _touchWall.FrameDataManager.Frame.Gestures[0].X
                            + "\nY: " + _touchWall.FrameDataManager.Frame.Gestures[0].Y
                            + "\nZ: " + _touchWall.FrameDataManager.Frame.Gestures[0].Z
                            + "\nGesture Status: " + TouchWallApp.CurrentGestureType;
                    }
                }
                catch (NullReferenceException e)
                {
                    CoordinatesLabel.Content = "";
                }
            }
        }

        /// <summary>
        /// Update the cursor button
        /// </summary>
        private void UpdateCursorStatusLabel()
        {
            if (TouchWallApp.CalibrateStatus != 0)
            {
                ToggleCursorButton.Content = "";
            }
            else
            {
            switch (TouchWallApp.CursorStatus)
            {
                case 1:
                    ToggleCursorButton.Content = "Cursor Enabled only";
                    break;
                case 2:
                    ToggleCursorButton.Content = "Cursor Enabled, Click Enabled";
                    break;
                case 3:
                    ToggleCursorButton.Content = "Cursor Enabled, Scroll Enabled";
                    break;
                default:
                    ToggleCursorButton.Content = "Cursor Disabled";
                    break;
            }
        }
        }

        /// <summary>
        /// Update the calibration Labels
        /// </summary>
        private void UpdateCalibrationLabels()
        {
            switch (TouchWallApp.CalibrateStatus)
            {
                case 1:
                    CalibrateButton.Content = "Calibrating...";
                    break;
                case 2:
                    CalibrateButton.Content = "Touch right edge of screen";
                    CoordinatesLabel.Content = "Touch right edge of screen";
                    break;
                case 3:
                    CalibrateButton.Content = "Touch Left edge of screen";
                    CoordinatesLabel.Content = "Touch Left edge of screen";
                    break;
                case 4:
                    CalibrateButton.Content = "Touch top edge of screen";
                    CoordinatesLabel.Content = "Touch top edge of screen";
                    break;
                case 5:
                    CalibrateButton.Content = "Touch bottom edge of screen";
                    CoordinatesLabel.Content = "Touch bottom edge of screen";
                    break;
                default:
                    CalibrateButton.Content = "Calibrate";
                    break;
            }
        }

        /// <summary>
        /// Update the Other buttons
        /// </summary>
        private void UpdateModeLabels()
        {
            if (TouchWallApp.CalibrateStatus != 0)
            {
                ToggleDepthTouchButton.Content = "";
                ToggleMultiTouchButton.Content = "";
                LaunchTouchdevelopButton.Content = "";
                LaunchKeyboardButton.Content = "";
                LaunchTouchdevelopLocalButton.Content = "";
            }
            else
            {
                LaunchTouchdevelopButton.Content = "TouchDevelop";
                LaunchTouchdevelopLocalButton.Content = "TouchDevelop Local";
                LaunchKeyboardButton.Content = "Keyboard";
                switch (TouchWallApp.MultiTouchMode)
                {
                    case 1:
                        ToggleDepthTouchButton.Content = "Depth Mode";
                        ToggleMultiTouchButton.Content = "Close Multi Mode";
                        break;
                    case 2:
                        ToggleDepthTouchButton.Content = "Close Depth Mode";
                            ToggleMultiTouchButton.Content = "Multi Mode";
                        break;
                    default:
                        ToggleDepthTouchButton.Content = "Depth Mode";
                            ToggleMultiTouchButton.Content = "Multi Mode";
                        break;
                }
            }
        }

        /// <summary>
        /// Status label updater - called when the Sensor connection is changed
        /// </summary>
        private void UpdateStatusLabel()
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                StatusLabel.Content = "Kinect Avaliable";
                
            }
            else
            {
                StatusLabel.Content = "Kinect Not Avaliable!";
                OpenSplashWindow();
                CloseDepthTouchWindow();
                CloseMultiTouchWindow();
                ClearLabels();
            }
        }

        /// <summary>
        /// Status label updater - called when the application has access to the depth data
        /// </summary>
        private void UpdateStatusLabel2()
        {
            if (_splashWindow != null)
            {
                CloseSplashWindow();
            }
            
            StatusLabel.Content = "Kinect In Use";
        }

        private void UpdateVoiceLabel()
        {
            if (_touchWall.VoiceRecoginitionMode == 1)
            {
                ToggleVoiceButton.Content = "Voice Control ON";
                if (_touchWall.CheckVoiceEngine())
                {
                    VoiceLabel.Content = "Press F1 For Voice Commands";
                }
                else
                {
                    VoiceLabel.Content = "Voice Commands Unavailable";
                }
            }
            else
            {
                ToggleVoiceButton.Content = "Voice Control OFF";
                VoiceLabel.Content = "Voice Commands Disabled";
            }

            
        }

        #endregion

        #region ButtonActions

        /// <summary>
        /// Handles the event where the user clicks on WallTopUp
        /// </summary>
        private void WallTop_ClickUp(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.TopEdge += 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopDown
        /// </summary>
        private void WallTop_ClickDown(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.TopEdge -= 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftLeft
        /// </summary>
        private void WallLeft_ClickLeft(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.LeftEdge -= 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftRight
        /// </summary>
        private void WallLeft_ClickRight(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.LeftEdge += 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightLeft
        /// </summary>
        private void WallRight_ClickLeft(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.RightEdge -= 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightRight
        /// </summary>
        private void WallRight_ClickRight(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.RightEdge += 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomUp
        /// </summary>
        private void WallBottom_ClickUp(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.BottomEdge += 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomDown
        /// </summary>
        private void WallBottom_ClickDown(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.BottomEdge -= 0.01f;
                Screen.SaveSettings();
            }
        }

        /// <summary>
        /// Enables or disables multitouch mode with the cursor
        /// </summary>
        private void Toggle_MultiTouch(object sender, RoutedEventArgs e)
        {
            
            if (TouchWallApp.MultiTouchMode != 1)
            {
                OpenMultiTouchWindow();
            } 
            else if (_multiTouchWindow.IsEnabled)
            {
                CloseMultiTouchWindow();
            }
        }

        /// <summary>
        /// Enables or disables multitouch mode with the cursor
        /// </summary>
        private void Toggle_DepthTouch(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.MultiTouchMode != 2)
            {
                OpenDepthTouchWindow();
            }
            else if (_depthTouchWindow.IsEnabled)
            {
                CloseDepthTouchWindow();
            }
            
        }

        /// <summary>
        /// Handles the event where the user clicks on CalibrateButton
        /// </summary>
        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            CalibrateClick();
        }

        /// <summary>
        /// Closes any canvas windows, and begins calibration
        /// </summary>
        public void CalibrateClick()
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                if (TouchWallApp.CalibrateStatus == 0)
                {
                    if (TouchWallApp.MultiTouchMode == 2)
                    {
                        CloseDepthTouchWindow();
                    }
                    else if (TouchWallApp.MultiTouchMode == 1)
                    {
                        CloseMultiTouchWindow();
                    }
                    TouchWallApp.MultiTouchMode = 0;
                    _touchWall.BeginCalibration();
                }
                else
                {
                    _touchWall.CancelCalibration();
                }
            }
        }

        /// <summary>
        /// Enables or disables clicking with the cursor
        /// </summary>
        private void Toggle_Cursor(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                if (TouchWallApp.CalibrateStatus == 0)
                {
                    _touchWall.ToggleCursor();
                }
            }

        }

        /// <summary>
        /// Called when the Touchdevelop button is pressed
        /// </summary>
        private void Launch_Touchdevelop(object sender, RoutedEventArgs e)
        {
            LaunchTouchdevelop();
        }

        /// <summary>
        /// Launches Touchdevelop in the browser
        /// </summary>
        public void LaunchTouchdevelop()
        {
            if (TouchWallApp.CalibrateStatus == 0)
            {
                Process.Start("https://www.touchdevelop.com/app/");
            }
        }

        /// <summary>
        /// Called when the Touchdevelop local button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Launch_TouchdevelopLocal(object sender, RoutedEventArgs e)
        {
            LaunchTouchdevelopLocal();
        }

        /// <summary>
        /// Launches Touchdevelop local in the browser
        /// </summary>
        public void LaunchTouchdevelopLocal()
        {
            if (TouchWallApp.CalibrateStatus == 0)
            {
                Process.Start("cmd", "/C \"C:\\Program Files\\nodejs\\nodevars.bat\" && touchdevelop"); // Touchdevelop local
            }
        }


        private void Launch_Keyboard(object sender, RoutedEventArgs e)
        {
            LaunchKeyboard();
        }

        public void LaunchKeyboard()
        {
            if (TouchWallApp.CalibrateStatus == 0)
            {
                //Process.Start("osk");
                string progFiles = @"C:\Program Files\Common Files\Microsoft Shared\ink";
                string keyboardPath = Path.Combine(progFiles, "TabTip.exe");

                Process.Start(keyboardPath);
            }
        }

        #endregion

        /// <summary>
        /// Closes the DepthModeWindow if it exists, then opens the Multitouchwindow
        /// </summary>
        public void OpenMultiTouchWindow()
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                if (TouchWallApp.MultiTouchMode == 2)
                {
                    CloseDepthTouchWindow();
                }
                if (TouchWallApp.MultiTouchMode != 1 && TouchWallApp.CalibrateStatus == 0)
                {
                    TouchWallApp.CursorStatus = 0;
                    TouchWallApp.MultiTouchMode = 1;
                    _multiTouchWindow = (MultiTouchWindow) MultiTouchWindow.GetMultiTouchWindowInstance(_touchWall);
                    _multiTouchWindow.Show();
                }
            }
        }

        /// <summary>
        /// Closes the MultitouchWindow if it exists
        /// </summary>
        public void CloseMultiTouchWindow()
        {
            try
            {
                if (_multiTouchWindow.IsEnabled)
                {
                    _multiTouchWindow.Close();
                    TouchWallApp.MultiTouchMode = 0;
                }
            }
            catch (NullReferenceException e)
            {
                // _multitouchWindow not initialized -> hasn't been opened yet -> so ignore error
            }
            
        }

        /// <summary>
        /// Closes the MultitouchWindow if it exists, then opens the DepthTouchWindow
        /// </summary>
        public void OpenDepthTouchWindow()
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                if (TouchWallApp.MultiTouchMode == 1)
                {
                    CloseMultiTouchWindow();
                }
                if (TouchWallApp.MultiTouchMode != 2 && TouchWallApp.CalibrateStatus == 0)
                {
                    TouchWallApp.CursorStatus = 0;
                    TouchWallApp.MultiTouchMode = 2;
                    _depthTouchWindow = (DepthTouchWindow) DepthTouchWindow.GetDepthTouchWindowInstance(_touchWall);
                    _depthTouchWindow.Show();
                }
            }
        }

        /// <summary>
        /// Closes the DepthTouchWindow if it exists
        /// </summary>
        public void CloseDepthTouchWindow()
        {
            try
            {
                if (_depthTouchWindow.IsEnabled)
                {
                    _depthTouchWindow.Close();
                    TouchWallApp.MultiTouchMode = 0;
                }
            }
            catch (NullReferenceException e)
            {
                // _depthTouchWindow not initialized -> hasn't been opened yet -> so ignore error
            }
        }

        /// <summary>
        /// Whenever a key press is activated on this window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString().Equals("F1"))
            {
                OpenHelpWindow();
            }
        }

        /// <summary>
        /// Opens new help window and brings it to focus
        /// </summary>
        public void OpenHelpWindow()
        {
            _helpWindow = HelpWindow.OpenHelpWindow();
            _helpWindow.Show();
            _helpWindow.Focus();
        }

        /// <summary>
        /// Closes the HelpWindow if it exists
        /// </summary>
        public void CloseHelpWindow()
        {
            if (_helpWindow.IsEnabled)
            {
                _helpWindow.Close();
            }
        }

        /// <summary>
        /// Opens new help window and brings it to focus
        /// </summary>
        public void OpenSplashWindow()
        {
            _splashWindow = SplashWindow.OpenSplashWindow();
            _splashWindow.Show();
            _splashWindow.Focus();
            _splashWindow.Activate();
            
            _splashWindow.Topmost = true;
        }

        /// <summary>
        /// Closes the SplashWindow if it exists
        /// </summary>
        public void CloseSplashWindow()
        {
            
            if (_splashWindow.IsEnabled)
            {
                _splashWindow.Close();
            }
        }


        /// <summary>
        /// Toggles the Voice Recognition abilities (if desired)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toggle_Voice(object sender, RoutedEventArgs e)
        {
            _touchWall.VoiceRecoginitionMode = (_touchWall.VoiceRecoginitionMode + 1)%2;
            
        }
    }
}
