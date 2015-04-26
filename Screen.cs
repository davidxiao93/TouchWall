using Microsoft.Kinect;

namespace TouchWall
{


    public class Screen
    {
        /// <summary>
        /// Distance (metres) from screen before moving the mouse
        /// </summary>
        public static float DetectThreshold { get; set; }

        /// <summary>
        /// Distance (metres) from screen before moving the mouse
        /// </summary>
        public static float MouseMoveThreshold { get; set; }

        /// <summary>
        /// Distance (metres) from screen before registering a left click down
        /// </summary>
        public static float MouseDownThreshold { get; set; }

        /// <summary>
        /// Distance (metres) from screen before registering a left click up
        /// </summary>
        public static float MouseUpThreshold { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and left edge of screen
        /// </summary>
        public static float LeftEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and right edge of screen
        /// </summary>
        public static float RightEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and top edge of screen
        /// </summary>
        public static float TopEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and bottom edge of screen
        /// </summary>
        public static float BottomEdge { get; set; }

        

        /// <summary>
        /// Storage of previous calibration values
        /// </summary>
        private static CameraSpacePoint[] _calibratePoints;

        /// <summary>
        /// For storing current position of user's hand
        /// </summary>
        private static CameraSpacePoint _pointFound;


        private ScreenMemento _screenMemento;

        /// <summary>
        /// Constructor
        /// </summary>
        public Screen()
        {
            // Default values
            MouseMoveThreshold = 0.10f;
            MouseDownThreshold = 0.001f;
            MouseUpThreshold = 0.03f;
            DetectThreshold = 0.15f;
            LeftEdge = 0.5f;
            RightEdge = 1.0f;
            TopEdge = 0.15f;
            BottomEdge = -0.14f;
            _screenMemento = new ScreenMemento(TopEdge, LeftEdge, RightEdge, BottomEdge);
            _calibratePoints = new CameraSpacePoint[TouchWallApp.KinectWidth * TouchWallApp.KinectHeight];
        }

        /// <summary>
        /// Sets up the calibration process by saving the current values of the screen in case the user decides to cancel later
        /// </summary>
        public void BeginCalibration()
        {
            TouchWallApp.CurrentGestureType = 1;
            TouchWallApp.CursorStatus = 0;
            _screenMemento = new ScreenMemento(TopEdge, LeftEdge, RightEdge, BottomEdge);
            if (TouchWallApp.CalibrateStatus == 0)
            {
                TouchWallApp.CalibrateStatus = 1;
            }
        }

        /// <summary>
        /// Restores the values of the screen to before calibration started
        /// </summary>
        public void CancelCalibration()
        {
            BottomEdge = _screenMemento.BottomEdge;
            RightEdge = _screenMemento.RightEdge;
            LeftEdge = _screenMemento.LeftEdge;
            TopEdge = _screenMemento.TopEdge;
            TouchWallApp.CurrentGestureType = 1;
            TouchWallApp.CalibrateStatus = 0;
        }

        /// <summary>
        /// Creates a frame of reference to compare to when calibrating
        /// </summary>
        /// <param name="spacePoints">3D model of the world</param>
        /// <param name="depthFrameDataSize">size of frame</param>
        internal static void CreateReferenceFrame(CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                _calibratePoints[i].X = spacePoints[i].X;
                _calibratePoints[i].Y = spacePoints[i].Y;
                _calibratePoints[i].Z = spacePoints[i].Z;
            }
            TouchWallApp.CalibrateStatus = 2;
        }

        internal static void LookForPoints(CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            _pointFound.Y = MouseMoveThreshold;

            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                if (TouchWallApp.CalibrateStatus == 2 || TouchWallApp.CalibrateStatus == 3) // Left and right calibration
                {
                    if (0 < spacePoints[i].Y && spacePoints[i].Y < _pointFound.Y && spacePoints[i].X > -0.1f && spacePoints[i].X < 0.1f 
                        && spacePoints[i].Z > 0.5f && spacePoints[i].Z < 8.0f && NewPointFound(spacePoints, i))
                    {
                        _pointFound.X = spacePoints[i].X;
                        _pointFound.Y = spacePoints[i].Y;
                        _pointFound.Z = spacePoints[i].Z;
                    }
                }
                else // Top and bottom calibration
                {
                    if (0 < spacePoints[i].Y && spacePoints[i].Y < _pointFound.Y && spacePoints[i].Z > LeftEdge && spacePoints[i].Z < RightEdge && NewPointFound(spacePoints, i))
                    {
                        _pointFound.X = spacePoints[i].X;
                        _pointFound.Y = spacePoints[i].Y;
                        _pointFound.Z = spacePoints[i].Z;
                    }
                }
            }
        }
        
        private static bool NewPointFound(CameraSpacePoint[] spacePoints, int i)
        {
            const float tolerance = 0.1f;
            if (spacePoints[i].X.Equals(float.NegativeInfinity) || spacePoints[i].X.Equals(float.PositiveInfinity)
                            || spacePoints[i].Y.Equals(float.NegativeInfinity) || spacePoints[i].Y.Equals(float.PositiveInfinity)
                            || spacePoints[i].Z.Equals(float.NegativeInfinity) || spacePoints[i].Z.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].X.Equals(float.NegativeInfinity) || _calibratePoints[i].X.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].Y.Equals(float.NegativeInfinity) || _calibratePoints[i].Y.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].Z.Equals(float.NegativeInfinity) || _calibratePoints[i].Z.Equals(float.PositiveInfinity))
            {
                // For this , the new pixel is unknown. As the point we are looking for isnt unknown, we don't want to use them
            }
            else if ((_calibratePoints[i].X - spacePoints[i].X) < tolerance && (_calibratePoints[i].X - spacePoints[i].X) > -tolerance
                && (_calibratePoints[i].Y - spacePoints[i].Y) < tolerance && (_calibratePoints[i].Y - spacePoints[i].Y) > -tolerance
                && (_calibratePoints[i].Z - spacePoints[i].Z) < tolerance && (_calibratePoints[i].Z - spacePoints[i].Z) > -tolerance)
            {
                // This point is the same as in the reference. We can ignore this piece of data
            }
            else
            {
                return true; // If a point has been found
            }
            return false;
        }

        public static void CalibrateEdge()
        {
            if (_pointFound.X.Equals(0) && _pointFound.Y.Equals(MouseMoveThreshold) && _pointFound.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                switch (TouchWallApp.CalibrateStatus)
                {
                    case 2:
                        RightEdge = _pointFound.Z;
                        break;
                    case 3:
                        LeftEdge = _pointFound.Z;
                        break;
                    case 4:
                        TopEdge = _pointFound.X;
                        break;
                    case 5:
                        BottomEdge = _pointFound.X;
                        break;
                }
                WaitForUserClick(_pointFound);
            }
        }

        public static void WaitForUserClick(CameraSpacePoint pointFound)
        {
            if (pointFound.Y < MouseDownThreshold && TouchWallApp.CurrentGestureType == 1)
            {
                TouchWallApp.CurrentGestureType = 2;
            }
            
            if (pointFound.Y > MouseUpThreshold && TouchWallApp.CurrentGestureType == 2)
            {
                TouchWallApp.CurrentGestureType = 1;
                if (TouchWallApp.CalibrateStatus < 5)
                {
                    TouchWallApp.CalibrateStatus++;
                }
                else
                {
                    TouchWallApp.CalibrateStatus = 0;
                    TouchWallApp.CursorStatus = 1;
                }
            }
        }
    }
}