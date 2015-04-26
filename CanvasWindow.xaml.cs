using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace TouchWall
{
    /// <summary>
    /// Abstract Class that covers both the depth window and multi window
    /// </summary>
    public abstract partial class CanvasWindow
    {
        /// <summary>
        /// CanvasWindow object, used for Singleton tracking
        /// </summary>
        protected static CanvasWindow Instance;

        /// <summary>
        /// TouchWallApp object, to provide coordinates for points
        /// </summary>
        private readonly TouchWallApp _touchWall;

        /// <summary>
        /// Constructor to initialize
        /// </summary>
        /// <param name="touchWall">TouchWallApp Object</param>
        /// <param name="winTitle">Title of the window</param>
        public CanvasWindow(TouchWallApp touchWall, String winTitle)
        {
            InitializeComponent();
            _touchWall = touchWall;
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += MapPoints;
            Title = winTitle;
        }

        /// <summary>
        /// Called when the Close button in the top right is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WindowClosing(object sender, CancelEventArgs e)
        {
            Instance = null;
            TouchWallApp.MultiTouchMode = 0;
            TouchWallApp.CursorStatus = 1;
        }

        /// <summary>
        /// Abstract class. Should do the calculations to find where to use MapPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public abstract void MapPoints(object sender, DepthFrameArrivedEventArgs e);

        /// <summary>
        /// Draws a single circle onto the screen.
        /// </summary>
        /// <param name="colour">What colour the circle is</param>
        /// <param name="size">How large the circle should be. This is number should be the diameter</param>
        /// <param name="id">ID number of the circle</param>
        public void MapPoint(Color colour, double size, int id)
        {
            if (size > 90)
            {
                Rectangle cursor = new Rectangle
                {
                    Fill = new SolidColorBrush(colour),
                    Width = size,
                    Height = size
                };
                Canvas.SetLeft(cursor, -0.5 * size + Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[id].X - Screen.LeftEdge) / (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursor, -0.5 * size + Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[id].Y - Screen.BottomEdge) / (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursor);
            }
            else
            {
                Ellipse cursor = new Ellipse
                {
                    Fill = new SolidColorBrush(colour),
                    Width = size,
                    Height = size
                };
                Canvas.SetLeft(cursor, -0.5 * size + Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[id].X - Screen.LeftEdge) / (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursor, -0.5 * size + Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[id].Y - Screen.BottomEdge) / (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursor);
            }

        }

        private void CanvasWindow_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString().Equals("F1"))
            {
                _touchWall.ParentMainWindow.OpenHelpWindow();
            }
            
        }
    }
}
