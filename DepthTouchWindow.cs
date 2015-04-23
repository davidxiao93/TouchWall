using System.Windows.Media;
using Microsoft.Kinect;

namespace TouchWall
{
    public class DepthTouchWindow : CanvasWindow
    {
        /// <summary>
        /// TouchWallApp object, to provide coordinates for points
        /// </summary>
        private readonly TouchWallApp _touchWall;

        /// <summary>
        /// Array of colours to be used for the colour picking
        /// </summary>
        private readonly Color[] _idColor = { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow };

        /// <summary>
        /// Constructor, uses the constructor of CanvasWindow as well
        /// </summary>
        /// <param name="touchWall"></param>
        private DepthTouchWindow(TouchWallApp touchWall) : base(touchWall)
        {
            InitializeComponent();
            _touchWall = touchWall;
            _touchWall.FrameDataManager.DepthFrameReader.FrameArrived += MapPoints;
        }

        /// <summary>
        /// Returns the DepthTouchWindow instance. We only want one of these
        /// </summary>
        /// <param name="touchWall"></param>
        /// <returns>Returns the DepthTouchWindow instance</returns>
        public static CanvasWindow GetDepthTouchWindowInstance(TouchWallApp touchWall)
        {
            if (instance == null)
            {
                instance = new DepthTouchWindow(touchWall);
            }
            return instance;
        }

        /// <summary>
        /// Does the calculations before calling MapPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void MapPoints(object sender, DepthFrameArrivedEventArgs e)
        {
            Map.Children.Clear();
            if (_touchWall.FrameDataManager.Frame.Gestures[0] != null)
            {
                double size = 2 / (_touchWall.FrameDataManager.Frame.Gestures[0].Z + 0.02);
                int colorPicker = 0;
                if (_touchWall.FrameDataManager.Frame.Gestures[0].Z < 0.01f)
                {
                    colorPicker = 1;
                }

                MapPoint(_idColor[colorPicker], size, 0);
            }
        }
    }
}

