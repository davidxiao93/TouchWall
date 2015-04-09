using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace TouchWall
{
    public partial class MultiTouchWindow
    {
        /// <summary>
        /// TouchWallApp object, to provide coordinates for points
        /// </summary>
        private readonly TouchWallApp _touchWall;

        private readonly Color[] idColor = {Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow};

        public MultiTouchWindow(TouchWallApp touchWall)
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
            Ellipse[] cursors = new Ellipse[4];

            for (int i = 0; i < 4; i++)
            {
                if (_touchWall.FrameDataManager.Frame.Gestures[i] != null)
                {
                    double size = 1.5/(_touchWall.FrameDataManager.Frame.Gestures[i].Z + 0.05);
                    cursors[i] = new Ellipse { Fill = new SolidColorBrush(idColor[i]), Width = size, Height = size };
                    Canvas.SetLeft(cursors[i], Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[i].X - Screen.LeftEdge) /
                                              (Screen.RightEdge - Screen.LeftEdge)));
                    Canvas.SetBottom(cursors[i], Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[i].Y - Screen.BottomEdge) /
                                              (Screen.TopEdge - Screen.BottomEdge)));
                    Map.Children.Add(cursors[i]);
                }
            }
        }
    }
}
