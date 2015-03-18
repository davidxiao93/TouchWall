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
        
        public MultiTouchWindow(TouchWallApp touchWall)
        {
            InitializeComponent();
            _touchWall = touchWall;
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += MapPoints;
        }

        /// <summary>
        /// Disable MultiTouch mode and close window
        /// </summary>
        private void MultiTouchWindow_Closing(object sender, RoutedEventArgs e)
        {
            TouchWallApp.MultiTouchMode = 0;
            TouchWallApp.CursorStatus = 1;
            Close();
        }

        private void MapPoints(object sender, DepthFrameArrivedEventArgs e)
        {
            Map.Children.Clear();
            Map.Children.Add(CloseButton);
            Ellipse[] cursors = new Ellipse[4];

            for (int i = 0; i < 4; i++)
            {
                if (_touchWall.FrameDataManager.Frame.Gestures[i] != null)
                {
                    cursors[i] = new Ellipse { Fill = new SolidColorBrush(Colors.Red), Width = 15, Height = 15 };
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
