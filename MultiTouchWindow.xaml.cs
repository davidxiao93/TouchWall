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

            if (_touchWall.FrameDataManager.Frame.Gestures[0] != null)
            {
                cursors[0] = new Ellipse { Fill = new SolidColorBrush(Colors.Red), Width = 12, Height = 12 };
                Canvas.SetLeft(cursors[0], Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[0].X - Screen.LeftEdge) /
                                          (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursors[0], Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[0].Y - Screen.BottomEdge) /
                                          (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursors[0]);
            }

            if (_touchWall.FrameDataManager.Frame.Gestures[1] != null)
            {
                cursors[1] = new Ellipse { Fill = new SolidColorBrush(Colors.Green), Width = 12, Height = 12 };
                Canvas.SetLeft(cursors[1], Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[1].X - Screen.LeftEdge) /
                                          (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursors[1], Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[1].Y - Screen.BottomEdge) /
                                            (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursors[1]);
            }

            if (_touchWall.FrameDataManager.Frame.Gestures[2] != null)
            {
                cursors[2] = new Ellipse { Fill = new SolidColorBrush(Colors.Blue), Width = 12, Height = 12 };
                Canvas.SetLeft(cursors[2], Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[2].X - Screen.LeftEdge) /
                                          (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursors[2], Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[2].Y - Screen.BottomEdge) /
                                            (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursors[2]);
            }

            if (_touchWall.FrameDataManager.Frame.Gestures[3] != null)
            {
                cursors[3] = new Ellipse { Fill = new SolidColorBrush(Colors.Yellow), Width = 12, Height = 12 };
                Canvas.SetLeft(cursors[3], Map.ActualWidth * ((_touchWall.FrameDataManager.Frame.Gestures[3].X - Screen.LeftEdge) /
                                          (Screen.RightEdge - Screen.LeftEdge)));
                Canvas.SetBottom(cursors[3], Map.ActualHeight * ((_touchWall.FrameDataManager.Frame.Gestures[3].Y - Screen.BottomEdge) /
                                            (Screen.TopEdge - Screen.BottomEdge)));
                Map.Children.Add(cursors[3]);
            }
        }
    }
}
