using Microsoft.Kinect;

namespace TouchWall
{
    public class Screen
    {
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
        /// Previous distance between sensor and left edge of screen, used for calibration
        /// </summary>
        private float _oldLeftEdge;

        /// <summary>
        /// Previous distance between sensor and right edge of screen, used for calibration
        /// </summary>
        private float _oldRightEdge;

        /// <summary>
        /// Previous distance between sensor and top edge of screen, used for calibration
        /// </summary>
        private float _oldTopEdge;

        /// <summary>
        /// Previous distance between sensor and bottom edge of screen, used for calibration
        /// </summary>
        private float _oldBottomEdge;

        /// <summary>
        /// Storage of previous calibration values
        /// </summary>
        private static CameraSpacePoint[] _calibratePoints;

        /// <summary>
        /// For storing current position of user's hand
        /// </summary>
        private static CameraSpacePoint _pointFound;

        public Screen()
        {
            // Default values
            MouseMoveThreshold = 0.12f;
            MouseDownThreshold = 0.003f;
            MouseUpThreshold = 0.006f;
            LeftEdge = 0.6f;
            RightEdge = 0.7f;
            TopEdge = 0.19f;
            BottomEdge = -0.11f;
            _oldLeftEdge = 0.6f;
            _oldRightEdge = 0.7f;
            _oldTopEdge = 0.19f;
            _oldBottomEdge = -0.11f;

            _calibratePoints = new CameraSpacePoint[TouchWallApp.KinectWidth * TouchWallApp.KinectHeight];
        }

        public void BeginCalibration()
        {
            TouchWallApp.CurrentGestureType = 1;
            TouchWallApp.CursorStatus = 0;
            _oldBottomEdge = BottomEdge;
            _oldLeftEdge = LeftEdge;
            _oldRightEdge = RightEdge;
            _oldTopEdge = TopEdge;
            if (TouchWallApp.CalibrateStatus == 0)
            {
                TouchWallApp.CalibrateStatus = 1;
            }
        }

        public void CancelCalibration()
        {
            BottomEdge = _oldBottomEdge;
            RightEdge = _oldRightEdge;
            LeftEdge = _oldLeftEdge;
            TopEdge = _oldTopEdge;
            TouchWallApp.CurrentGestureType = 1;
            TouchWallApp.CalibrateStatus = 0;
        }

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