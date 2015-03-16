using System;
using Microsoft.Kinect;

namespace TouchWall
{
    public class Frame
    {
        /// <summary>
        /// Any new gestures are stored and handled by Gesture
        /// </summary>
        public Gesture[] Gestures = new Gesture[4];

        /// <summary>
        /// Intermediate storage for frame data converted to colour
        /// </summary>
        public readonly byte[] DepthPixels;
        
        /// <summary>
        /// Should be KinectHeight * KinectWidth
        /// </summary>
        private readonly uint _depthFrameDataSize;

        /// <summary>
        /// Pointer to the raw depth buffer
        /// </summary>
        private readonly IntPtr _depthFrameData;

        /// <summary>
        /// Points on 3D map
        /// </summary>
        private readonly CameraSpacePoint[] _spacePoints;
        private CameraSpacePoint _pointFound;

        internal Frame(IntPtr depthFrameData, uint depthFrameDataSize, FrameDescription depthFrameDescription)
        {
            _depthFrameData = depthFrameData;
            _depthFrameDataSize = depthFrameDataSize;
            _spacePoints = new CameraSpacePoint[_depthFrameDataSize / sizeof(ushort)];
            DepthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height]; // Allocate space to put received pixels
            SetupCanvasFrame();
            UseDepthFrameData();
        }

        /// <summary>
        /// Takes in the depth frame, converts it to camera space
        /// </summary>
        public unsafe void SetupCanvasFrame()
        {
            ushort* frameData = (ushort*) _depthFrameData;

            for (int y = 0; y < TouchWallApp.KinectHeight; y++)
            {
                if (y == TouchWallApp.KinectHeight / 2)
                {
                    for (int x = 0; x < TouchWallApp.KinectWidth; x++)
                    {
                        DepthPixels[y * TouchWallApp.KinectWidth + x] = (255);
                    }
                }
                else
                {
                    for (int x = 0; x < TouchWallApp.KinectWidth; x++)
                    {
                        ushort depth = frameData[y * TouchWallApp.KinectWidth + x];
                        DepthPixels[y * TouchWallApp.KinectWidth + x] =
                            (byte) (depth >= 500 && depth <= 5000 ? (depth*256/5000) : 0);
                    }
                }
            }

            CoordinateMapper m = TouchWallApp.KinectSensor.CoordinateMapper;
            ushort[] frameUshorts = new ushort[_depthFrameDataSize/sizeof (ushort)];
            for (int i = 0; i < _depthFrameDataSize/sizeof (ushort); i++)
            {
                frameUshorts[i] = frameData[i];
            }

            m.MapDepthFrameToCameraSpace(frameUshorts, _spacePoints); // X,Y,Z in terms of the CAMERA, not the user
            // Now spacePoints contains a 3d virtualisation of where everything is.
        }

        private void UseDepthFrameData()
        {
            switch (TouchWallApp.CalibrateStatus)
            {
                case 1: // Begin Calibration
                    Screen.CreateReferenceFrame(_spacePoints, _depthFrameDataSize);
                    break;
                case 2: // Right Edge
                    Screen.PrepareHorizontalCalibration(ref _pointFound, _spacePoints, _depthFrameDataSize);
                    Screen.CalibrateRightEdge(_pointFound);
                    break;
                case 3: // Left Edge
                    Screen.PrepareHorizontalCalibration(ref _pointFound, _spacePoints, _depthFrameDataSize);
                    Screen.CalibrateLeftEdge(_pointFound);
                    break;
                case 4: // Top Edge
                    Screen.PrepareVerticalCalibration(ref _pointFound, _spacePoints, _depthFrameDataSize);
                    Screen.CalibrateTopEdge(_pointFound);
                    break;
                case 5: // Bottom Edge
                    Screen.PrepareVerticalCalibration(ref _pointFound, _spacePoints, _depthFrameDataSize);
                    Screen.CalibrateBottomEdge(_pointFound);
                    break;
                default: // Not in calibration mode
                    FindGestures();
                    break;
            }
        }

        /// <summary>
        /// Looks for user's hand and creates new gestures
        /// </summary>
        private void FindGestures()
        {
            _pointFound.X = 0.0f;
            _pointFound.Y = Screen.MouseMoveThreshold;
            _pointFound.Z = 0.0f;

            int gesturesFound = 0;
            for (int i = 0; i < _depthFrameDataSize / sizeof(ushort); i++)
            {
                if (_spacePoints[i].X > Screen.BottomEdge && _spacePoints[i].X < Screen.TopEdge && _spacePoints[i].Y > 0 && _spacePoints[i].Y < _pointFound.Y
                        && _spacePoints[i].Z > Screen.LeftEdge && _spacePoints[i].Z < Screen.RightEdge)
                {
                    _pointFound.X = _spacePoints[i].X;
                    _pointFound.Y = _spacePoints[i].Y;
                    _pointFound.Z = _spacePoints[i].Z;

                    if (TouchWallApp.CurrentGestureType == 0)
                    {
                        TouchWallApp.CurrentGestureType = 1;
                    }

                    switch (gesturesFound) // Allowing up to four gestures in MultiTouch mode
                    {
                        case 0:
                            Gestures[0] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                            if (TouchWallApp.MultiTouchMode == 0)
                            {
                                return;
                            }
                            gesturesFound++;
                            break;
                        case 1:
                            if (Math.Abs(_pointFound.Z - Gestures[0].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[0].Y) > 0.05f)
                            {
                                Gestures[1] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                                gesturesFound++;
                            }
                            break;
                        case 2:
                            if (Math.Abs(_pointFound.Z - Gestures[0].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[0].Y) > 0.05f
                                && Math.Abs(_pointFound.Z - Gestures[1].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[1].Y) > 0.05f)
                            {
                                Gestures[2] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                                gesturesFound++;
                            }
                            break;
                        case 3:
                            if (Math.Abs(_pointFound.Z - Gestures[0].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[0].Y) > 0.05f
                                && Math.Abs(_pointFound.Z - Gestures[1].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[1].Y) > 0.05f
                                && Math.Abs(_pointFound.Z - Gestures[2].X) > 0.07f &&
                                Math.Abs(_pointFound.X - Gestures[2].Y) > 0.05f)
                            {
                                Gestures[3] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                                gesturesFound++;
                            }
                            break;
                    }
                }
            }
        }
    }
}
        