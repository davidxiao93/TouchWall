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

        public Screen()
        {
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

        internal static void PrepareHorizontalCalibration(ref CameraSpacePoint userPoint, CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            userPoint.Y = MouseMoveThreshold;
            //userPoint.Z = 4.0f;
            TouchWallApp.CursorStatus = 0;
            const float tolerance = 0.1f;
            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                if (0 < spacePoints[i].Y && spacePoints[i].Y < userPoint.Y && spacePoints[i].X > -0.1f && spacePoints[i].X < 0.1f && spacePoints[i].Z > 0.5f && spacePoints[i].Z < 8.0f)
                {
                    if (spacePoints[i].X.Equals(float.NegativeInfinity) || spacePoints[i].X.Equals(float.PositiveInfinity)
                            || spacePoints[i].Y.Equals(float.NegativeInfinity) || spacePoints[i].Y.Equals(float.PositiveInfinity)
                            || spacePoints[i].Z.Equals(float.NegativeInfinity) || spacePoints[i].Z.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].X.Equals(float.NegativeInfinity) || _calibratePoints[i].X.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].Y.Equals(float.NegativeInfinity) || _calibratePoints[i].Y.Equals(float.PositiveInfinity)
                            || _calibratePoints[i].Z.Equals(float.NegativeInfinity) || _calibratePoints[i].Z.Equals(float.PositiveInfinity))
                    {
                        // For this point, the new pixel is unknown. As the point we are looking for isnt unknown, we don't want to use them
                    }
                    else if ((_calibratePoints[i].X - spacePoints[i].X) < tolerance && (_calibratePoints[i].X - spacePoints[i].X) > -1 * tolerance
                        && (_calibratePoints[i].Y - spacePoints[i].Y) < tolerance && (_calibratePoints[i].Y - spacePoints[i].Y) > -1 * tolerance
                        && (_calibratePoints[i].Z - spacePoints[i].Z) < tolerance && (_calibratePoints[i].Z - spacePoints[i].Z) > -1 * tolerance)
                    {
                        // This point is the same as in the reference. We can ignore this piece of data
                    }
                    else
                    {
                        userPoint.X = spacePoints[i].X;
                        userPoint.Y = spacePoints[i].Y;
                        userPoint.Z = spacePoints[i].Z;
                    }
                }
            }
        }

        internal static void PrepareVerticalCalibration(ref CameraSpacePoint userPoint, CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            userPoint.Y = MouseMoveThreshold;
            TouchWallApp.CursorStatus = 0;
            const float tolerance = 0.1f;

            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                if (0 < spacePoints[i].Y && spacePoints[i].Y < userPoint.Y && spacePoints[i].Z > LeftEdge && spacePoints[i].Z < RightEdge)
                {
                    if (spacePoints[i].X.Equals(float.NegativeInfinity) || spacePoints[i].X.Equals(float.PositiveInfinity)
                               || spacePoints[i].Y.Equals(float.NegativeInfinity) || spacePoints[i].Y.Equals(float.PositiveInfinity)
                               || spacePoints[i].Z.Equals(float.NegativeInfinity) || spacePoints[i].Z.Equals(float.PositiveInfinity)
                               || _calibratePoints[i].X.Equals(float.NegativeInfinity) || _calibratePoints[i].X.Equals(float.PositiveInfinity)
                               || _calibratePoints[i].Y.Equals(float.NegativeInfinity) || _calibratePoints[i].Y.Equals(float.PositiveInfinity)
                               || _calibratePoints[i].Z.Equals(float.NegativeInfinity) || _calibratePoints[i].Z.Equals(float.PositiveInfinity))
                    {
                        // For this point, the new pixel is unknown. As the point we are looking for isnt unknown, we don't want to use them
                    }
                    else if ((_calibratePoints[i].X - spacePoints[i].X) < tolerance && (_calibratePoints[i].X - spacePoints[i].X) > -1 * tolerance
                        && (_calibratePoints[i].Y - spacePoints[i].Y) < tolerance && (_calibratePoints[i].Y - spacePoints[i].Y) > -1 * tolerance
                        && (_calibratePoints[i].Z - spacePoints[i].Z) < tolerance && (_calibratePoints[i].Z - spacePoints[i].Z) > -1 * tolerance)
                    {
                        // This point is the same as in the reference. We can ignore this piece of data
                    }
                    else
                    {
                        userPoint.X = spacePoints[i].X;
                        userPoint.Y = spacePoints[i].Y;
                        userPoint.Z = spacePoints[i].Z;
                    }
                }
            }
        }

        public static void CalibrateRightEdge(CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                RightEdge = userPoint.Z;

                if (userPoint.Y < MouseDownThreshold && TouchWallApp.CurrentGestureType == 1)
                {
                    TouchWallApp.CurrentGestureType = 2;
                }

                if (userPoint.Y > MouseUpThreshold && TouchWallApp.CurrentGestureType == 2)
                {
                    TouchWallApp.CurrentGestureType = 1;
                    TouchWallApp.CalibrateStatus = 3;
                }
            }
        }

        public static void CalibrateLeftEdge(CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                LeftEdge = userPoint.Z;

                if (userPoint.Y < MouseDownThreshold && TouchWallApp.CurrentGestureType == 1)
                {
                    TouchWallApp.CurrentGestureType = 2;
                }

                if (userPoint.Y > MouseUpThreshold && TouchWallApp.CurrentGestureType == 2)
                {
                    TouchWallApp.CurrentGestureType = 1;
                    TouchWallApp.CalibrateStatus = 4;
                }
            }
        }

        public static void CalibrateTopEdge(CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                TopEdge = userPoint.X;

                if (userPoint.Y < MouseDownThreshold && TouchWallApp.CurrentGestureType == 1)
                {
                    TouchWallApp.CurrentGestureType = 2;
                }

                if (userPoint.Y > MouseUpThreshold && TouchWallApp.CurrentGestureType == 2)
                {
                    TouchWallApp.CurrentGestureType = 1;
                    TouchWallApp.CalibrateStatus = 5;
                }
            }
        }

        public static void CalibrateBottomEdge(CameraSpacePoint userPoint)
        {

            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                BottomEdge = userPoint.X;
                
                if (userPoint.Y < MouseDownThreshold && TouchWallApp.CurrentGestureType == 1)
                {
                    TouchWallApp.CurrentGestureType = 2;
                }

                if (userPoint.Y > MouseUpThreshold && TouchWallApp.CurrentGestureType == 2)
                {
                    TouchWallApp.CurrentGestureType = 1;
                    TouchWallApp.CursorStatus = 1;
                    TouchWallApp.CalibrateStatus = 0;
                }
            }
        }
    }
}