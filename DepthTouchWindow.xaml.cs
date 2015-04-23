using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace TouchWall
{
    public partial class DepthTouchWindow
    {
        /// <summary>
        /// TouchWallApp object, to provide coordinates for points
        /// </summary>
        private readonly TouchWallApp _touchWall;

        private readonly Color[] _idColor = {Colors.Red, Colors.Green};

        public DepthTouchWindow(TouchWallApp touchWall)
        {
            InitializeComponent();
            _touchWall = touchWall;
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += MapPoints;
        }

        private void MultiTouchWindow_Closing(object sender, CancelEventArgs e)
        {
            TouchWallApp.MultiTouchMode = 0;
            TouchWallApp.CursorStatus = 1;
            //Close();
        }

        private void MapPoints(object sender, DepthFrameArrivedEventArgs e)
        {
            Map.Children.Clear();
            if (_touchWall.FrameDataManager.Frame.Gestures[0] != null)
            {
                double size = 2/(_touchWall.FrameDataManager.Frame.Gestures[0].Z + 0.02);
                int colorPicker = 0;
                if (_touchWall.FrameDataManager.Frame.Gestures[0].Z < 0.01f)
                {
                    colorPicker = 1;
                }
                Ellipse cursor = new Ellipse { Fill = new SolidColorBrush(_idColor[colorPicker]), Width = size, Height = size };
                Canvas.SetLeft(cursor, -0.5 * size + Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[0].X - Screen.LeftEdge) /
                                            (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursor, -0.5 * size + Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[0].Y - Screen.BottomEdge) /
                                            (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursor);
            }
        }
    }
}
