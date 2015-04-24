using System.Windows.Media;
using Microsoft.Kinect;

namespace TouchWall
{
    public class MultiTouchWindow : CanvasWindow
    {
        /// <summary>
        /// TouchWallApp object, to provide coordinates for points
        /// </summary>
        private readonly TouchWallApp _touchWall;

        /// <summary>
        /// Array of colours to be used for the colour picking
        /// </summary>
        private readonly Color[] _idColor = { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow};

        /// <summary>
        /// Constructor, uses the constructor of CanvasWindow as well
        /// </summary>
        /// <param name="touchWall"></param>
        private MultiTouchWindow(TouchWallApp touchWall) : base (touchWall, "Touch Wall - Multi Touch Mode")
        {
            InitializeComponent();
            _touchWall = touchWall;
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += MapPoints;
        }

        /// <summary>
        /// Returns the MultiTouchWindow instance. We only want one of these
        /// </summary>
        /// <param name="touchWall"></param>
        /// <returns>Returns the MultiTouchWindow instance</returns>
        public static CanvasWindow GetMultiTouchWindowInstance(TouchWallApp touchWall)
        {
            if (Instance == null)
            {
                Instance = new MultiTouchWindow(touchWall);
            }
            return Instance;
        }

        /// <summary>
        /// Does the calculations before calling MapPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void MapPoints(object sender, DepthFrameArrivedEventArgs e)
        {
            Map.Children.Clear();
            for (int i = 0; i < 4; i++)
            {
                if (_touchWall.FrameDataManager.Frame.Gestures[i] != null)
                {
                    MapPoint(_idColor[i], 16,i);
                }
            }
        }
    }
}