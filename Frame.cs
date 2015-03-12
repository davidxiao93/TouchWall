using System;
using Microsoft.Kinect;

namespace TouchWall
{
    public class Frame
    {
        private readonly KinectSensor _kinectSensor;
        private readonly IntPtr _depthFrameData;
        private readonly uint _depthFrameDataSize;
        private readonly FrameDescription _depthFrameDescription;
        private readonly Screen _screen;
        private readonly TouchGestureHandler _gestures;
        public CameraSpacePoint UserPoint = new CameraSpacePoint();

        /// <summary>
        /// Intermediate storage for frame data converted to colour
        /// </summary>
        public readonly byte[] DepthPixels;

        public Frame(IntPtr depthFrameData, uint depthFrameDataSize, KinectSensor kinectSensor,
            FrameDescription depthFrameDescription, Screen screen, TouchGestureHandler gestures)
        {
            _kinectSensor = kinectSensor;
            _screen = screen;
            _gestures = gestures;
            _depthFrameData = depthFrameData;
            _depthFrameDataSize = depthFrameDataSize;
            _depthFrameDescription = depthFrameDescription;

            // Allocate space to put the pixels being received and converted
            DepthPixels = new byte[_depthFrameDescription.Width*_depthFrameDescription.Height];
        }

        /// <summary>
        /// This function takes in the depth frame, converts it to camera space, and looks for the user finger.
        /// "depthFrameData" = Pointer to the raw depth buffer
        /// "depthFrameDataSize" = How large the depth frame is. should be KinectHeight*KinectWidth
        /// </summary>
        public unsafe void ConvertProcessDepthFrameData()
        {
            ushort* frameData = (ushort*) _depthFrameData;

            #region SetupCanvasFrame

            for (int y = 0; y < KinectConstants.KinectHeight; y++)
            {
                if (y == KinectConstants.KinectHeight/2)
                {
                    for (int x = 0; x < KinectConstants.KinectWidth; x++)
                    {
                        DepthPixels[y*KinectConstants.KinectWidth + x] = (255);
                    }
                }
                else
                {
                    for (int x = 0; x < KinectConstants.KinectWidth; x++)
                    {
                        ushort depth = frameData[y*KinectConstants.KinectWidth + x];
                        DepthPixels[y*KinectConstants.KinectWidth + x] =
                            (byte) (depth >= 500 && depth <= 5000 ? (depth*256/5000) : 0);
                    }
                }
            }

            #endregion

            CoordinateMapper m = _kinectSensor.CoordinateMapper;
            ushort[] frameUshorts = new ushort[_depthFrameDataSize/sizeof (ushort)];
            for (int i = 0; i < _depthFrameDataSize/sizeof (ushort); i++)
            {
                frameUshorts[i] = frameData[i];
            }
            CameraSpacePoint[] spacePoints = new CameraSpacePoint[_depthFrameDataSize/sizeof (ushort)];
            // X,Y,Z in terms of the camera, not the user
            m.MapDepthFrameToCameraSpace(frameUshorts, spacePoints);

            // Now spacePoints contains a 3d virtualisation of where everything is.

            UserPoint.X = 0.0f;
            UserPoint.Y = 0.0f;
            UserPoint.Z = 0.0f;

            switch (_screen.CalibrateStatus)
            {
                case 1: // Begin Calibration
                    _screen.CreateReferenceFrame(spacePoints, _depthFrameDataSize);
                    break;
                case 3: // Right Edge
                    _screen.PrepareHorizontalCalibration(_gestures, ref UserPoint, spacePoints, _depthFrameDataSize);
                    _screen.CalibrateRightEdge(_gestures, UserPoint);
                    break;
                case 4: // Left Edge
                    _screen.PrepareHorizontalCalibration(_gestures, ref UserPoint, spacePoints, _depthFrameDataSize);
                    _screen.CalibrateLeftEdge(_gestures, UserPoint);
                    break;
                case 5: // Top Edge
                    _screen.PrepareVerticalCalibration(_gestures, ref UserPoint, spacePoints, _depthFrameDataSize);
                    _screen.CalibrateTopEdge(_gestures, UserPoint);
                    break;
                case 6: // Bottom Edge
                    _screen.PrepareVerticalCalibration(_gestures, ref UserPoint, spacePoints, _depthFrameDataSize);
                    _screen.CalibrateBottomEdge(_gestures, UserPoint);
                    break;
                default: // Not in calibration mode
                    UserPoint.Y = _screen.MouseMoveThreshold;
                    for (int i = 0; i < _depthFrameDataSize/sizeof (ushort); i++)
                    {
                        if (0 < spacePoints[i].Y && spacePoints[i].Y < UserPoint.Y &&
                                _screen.BottomEdge < spacePoints[i].X && spacePoints[i].X < _screen.TopEdge
                                && _screen.LeftEdge < spacePoints[i].Z && spacePoints[i].Z < _screen.RightEdge)
                        {
                            UserPoint.X = spacePoints[i].X;
                            UserPoint.Y = spacePoints[i].Y;
                            UserPoint.Z = spacePoints[i].Z;
                        }
                    }
                    if (UserPoint.X.Equals(0) && UserPoint.Y.Equals(_screen.MouseMoveThreshold) &&
                            UserPoint.Z.Equals(0))
                    {
                        _gestures.MouseStatus = 0; // No point has been found
                    }
                    else
                    {
                        if (_gestures.MouseStatus == 0)
                        {
                            _gestures.MouseStatus = 1;
                        }
                        _gestures.ProcessGesture(UserPoint.X, UserPoint.Y, UserPoint.Z, _screen);
                    }
                    break;
            }
        }
    }
}
        