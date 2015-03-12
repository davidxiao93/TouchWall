using Microsoft.Kinect;

namespace TouchWall
{
    public class Screen
    {
        /// <summary>
        /// Determines if the program is in calibration mode
        /// </summary>
        public int CalibrateStatus { get; set; }

        /// <summary>
        /// Distance (metres) from screen before moving the mouse
        /// </summary>
        public float MouseMoveThreshold { get; set; }

        /// <summary>
        /// Distance (metres) from screen before registering a left click down
        /// </summary>
        public float MouseDownThreshold { get; set; }

        /// <summary>
        /// Distance (metres) from screen before registering a left click up
        /// </summary>
        public float MouseUpThreshold { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and left edge of screen
        /// </summary>
        public float LeftEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and right edge of screen
        /// </summary>
        public float RightEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and top edge of screen
        /// </summary>
        public float TopEdge { get; set; }

        /// <summary>
        /// Distance (metres) between sensor and bottom edge of screen
        /// </summary>
        public float BottomEdge { get; set; }

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
        private readonly CameraSpacePoint[] _calibratePoints;

        public Screen()
        {
            CalibrateStatus = 0;
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
            _calibratePoints = new CameraSpacePoint[KinectConstants.KinectWidth * KinectConstants.KinectHeight];
        }

        public void BeginCalibration()
        {
            _oldBottomEdge = BottomEdge;
            _oldLeftEdge = LeftEdge;
            _oldRightEdge = RightEdge;
            _oldTopEdge = TopEdge;
            if (CalibrateStatus == 0)
            {
                CalibrateStatus = 1;
            }
        }

        public void CancelCalibration()
        {
            BottomEdge = _oldBottomEdge;
            RightEdge = _oldRightEdge;
            LeftEdge = _oldLeftEdge;
            TopEdge = _oldTopEdge;
            CalibrateStatus = 0;
        }

        internal void CreateReferenceFrame(CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            for (int i = 0; i < depthFrameDataSize / sizeof(ushort); i++)
            {
                _calibratePoints[i].X = spacePoints[i].X;
                _calibratePoints[i].Y = spacePoints[i].Y;
                _calibratePoints[i].Z = spacePoints[i].Z;
            }
            CalibrateStatus = 3;
        }

        internal void PrepareHorizontalCalibration(TouchGestureHandler gestures, ref CameraSpacePoint userPoint, CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            // Getting right coordinates
            userPoint.Y = MouseMoveThreshold;
            userPoint.Z = 4.0f;
            gestures.MouseAllowed = 0;
            float tolerance = 0.1f;
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

        internal void PrepareVerticalCalibration(TouchGestureHandler gestures, ref CameraSpacePoint userPoint, CameraSpacePoint[] spacePoints, uint depthFrameDataSize)
        {
            userPoint.Y = MouseMoveThreshold;
            gestures.MouseAllowed = 0;
            float tolerance = 0.1f;

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

        public void CalibrateRightEdge(TouchGestureHandler gestures, CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                RightEdge = userPoint.Z;

                if (userPoint.Y < MouseDownThreshold && gestures.MouseStatus == 1)
                {
                    gestures.MouseStatus = 2;
                }

                if (userPoint.Y > MouseUpThreshold && gestures.MouseStatus == 2)
                {
                    gestures.MouseStatus = 1;
                    CalibrateStatus = 4;
                }
            }
        }

        public void CalibrateLeftEdge(TouchGestureHandler gestures, CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                LeftEdge = userPoint.Z;

                if (userPoint.Y < MouseDownThreshold && gestures.MouseStatus == 1)
                {
                    gestures.MouseStatus = 2;
                }

                if (userPoint.Y > MouseUpThreshold && gestures.MouseStatus == 2)
                {
                    gestures.MouseStatus = 1;
                    CalibrateStatus = 5;
                }
            }
        }

        public void CalibrateTopEdge(TouchGestureHandler gestures, CameraSpacePoint userPoint)
        {
            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                TopEdge = userPoint.X;

                if (userPoint.Y < MouseDownThreshold && gestures.MouseStatus == 1)
                {
                    gestures.MouseStatus = 2;
                }

                if (userPoint.Y > MouseUpThreshold && gestures.MouseStatus == 2)
                {
                    gestures.MouseStatus = 1;
                    CalibrateStatus = 6;
                }
            }
        }

        public void CalibrateBottomEdge(TouchGestureHandler gestures, CameraSpacePoint userPoint)
        {

            if (userPoint.X.Equals(0) && userPoint.Y.Equals(MouseMoveThreshold) && userPoint.Z.Equals(0))
            {
                // No point has been found
            }
            else
            {
                BottomEdge = userPoint.X;
                
                if (userPoint.Y < MouseDownThreshold && gestures.MouseStatus == 1)
                {
                    gestures.MouseStatus = 2;
                }

                if (userPoint.Y > MouseUpThreshold && gestures.MouseStatus == 2)
                {
                    gestures.MouseStatus = 1;
                    gestures.MouseAllowed = 1;
                    CalibrateStatus = 0;
                }
            }
        }
    }
}