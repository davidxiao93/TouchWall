using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace TouchWall
{

    

    /// <summary>
    /// Interaction logic for CalibrateWindow.xaml
    /// </summary>
    public partial class CalibrateWindow : Window
    {
        private static MainWindow _parentMainWindow;
        private CalibrateWindow(MainWindow mainWindow)
        {
            _parentMainWindow = mainWindow;
            InitializeComponent();
            UpdateLabels();
        }

        private static CalibrateWindow _instance;

        public static CalibrateWindow OpenCalibrateWindow(MainWindow mainWindow)
        {
            if (_instance == null)
            {
                _instance = new CalibrateWindow(mainWindow);
            }
            return _instance;
        }

        /// <summary>
        /// Called when the Close button in the top right is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WindowClosing(object sender, CancelEventArgs e)
        {
            _instance = null;
        }

        private void UpdateLabels()
        {
            WallTopLabel.Content = Screen.TopEdge.ToString("0.00") + "m";
            WallLeftLabel.Content = Screen.LeftEdge.ToString("0.00") + "m";
            WallRightLabel.Content = Screen.RightEdge.ToString("0.00") + "m";
            WallBottomLabel.Content = Screen.BottomEdge.ToString("0.00") + "m";
        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopUp
        /// </summary>
        private void WallTop_ClickUp(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.TopEdge += 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallTopDown
        /// </summary>
        private void WallTop_ClickDown(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.TopEdge -= 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftLeft
        /// </summary>
        private void WallLeft_ClickLeft(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.LeftEdge -= 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallLeftRight
        /// </summary>
        private void WallLeft_ClickRight(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.LeftEdge += 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightLeft
        /// </summary>
        private void WallRight_ClickLeft(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.RightEdge -= 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallRightRight
        /// </summary>
        private void WallRight_ClickRight(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.RightEdge += 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomUp
        /// </summary>
        private void WallBottom_ClickUp(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.BottomEdge += 0.01f;
            }
            UpdateLabels();
        }

        /// <summary>
        /// Handles the event where the user clicks on WallBottomDown
        /// </summary>
        private void WallBottom_ClickDown(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                Screen.BottomEdge -= 0.01f;
            }
            UpdateLabels();
        }

        private void FullCalibration(object sender, RoutedEventArgs e)
        {
            if (TouchWallApp.KinectSensor.IsAvailable)
            {
                _parentMainWindow.CalibrateClick();
            }
        }
    }
}
