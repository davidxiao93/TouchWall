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
        private readonly CameraSpacePoint[] _foundGestures;
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
                case 0: // Not in calibration mode
                    FindGestures();
                    break;
                case 1: // Begin Calibration
                    Screen.CreateReferenceFrame(_spacePoints, _depthFrameDataSize);
                    break;
                default: // Use frame data to calibrate an edge of screen
                    Screen.LookForPoints(_spacePoints, _depthFrameDataSize);
                    Screen.CalibrateEdge();
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
            for (int i = (int)(_depthFrameDataSize / sizeof(ushort)) - 1; i >= 0; i--)
            {
                if (_spacePoints[i].X > Screen.BottomEdge && _spacePoints[i].X < Screen.TopEdge
                    && _spacePoints[i].Y > 0 && _spacePoints[i].Y < Screen.MouseMoveThreshold
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
                            Gestures[gesturesFound] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                            if (TouchWallApp.MultiTouchMode == 0)
                            {
                                return;
                            }
                            gesturesFound++;
                            break;
                        //case 1: case 2: case 3:
                        default:
                            Boolean foundNewPoint = true;
                            float xTolerance = 0.07f;
                            float yTolerance = 0.05f;
                            for (int a = 0; a < gesturesFound; a++)
                            {
                                if ((Math.Abs(_pointFound.Z - Gestures[a].X) < xTolerance
                                          && Math.Abs(_pointFound.X - Gestures[a].Y) < yTolerance))
                                {
                                    foundNewPoint = false;
                                    break;
                                }

                            }
                            if (foundNewPoint == true)
                            {
                                Gestures[gesturesFound] = new Gesture(_pointFound.X, _pointFound.Y, _pointFound.Z);
                                gesturesFound++;
                                if (gesturesFound >= 4)
                                {
                                    return;
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Looks for user's hand and creates new gestures
        /// Testing version!!!!
        /// </summary>
        private void FindGestures2()
        {
            for (int i = 0; i < 4; i++)
            {
                _foundGestures[i].X = 0.0f;
                _foundGestures[i].Y = Screen.MouseMoveThreshold;
                _foundGestures[i].Z = 0.0f;
            }

            int gesturesFound = 0;
            for (int i = (int) (_depthFrameDataSize/sizeof (ushort)) - 1; i >= 0; i--)
            {
                if (_spacePoints[i].X > Screen.BottomEdge && _spacePoints[i].X < Screen.TopEdge
                    && _spacePoints[i].Y > 0 && _spacePoints[i].Y < Screen.MouseMoveThreshold
                    && _spacePoints[i].Z > Screen.LeftEdge && _spacePoints[i].Z < Screen.RightEdge)
                {
                    Boolean foundNewPoint = true;
                    float xTolerance = 0.07f;
                    float yTolerance = 0.05f;
                    for (int a = 0; a < gesturesFound; a++)
                    {
                        if ((Math.Abs(_spacePoints[i].Z - Gestures[a].X) < xTolerance
                             && Math.Abs(_spacePoints[i].X - Gestures[a].Y) < yTolerance))
                        {
                            foundNewPoint = false;
                            break;
                        }

                    }
                    if (foundNewPoint == true)
                    {
                        // we've found a new point of interest
                        if (gesturesFound == 4)
                        {
                            // we've reached our limit of 4 guestures.
                            // check if this new point is closer to the screen than any of the existing points
                            // this means that the program will look for the nearest up to 4 points
                            for (int j = 0; j < gesturesFound; j++)
                            {
                                if (_foundGestures[gesturesFound].Y > _spacePoints[i].Y)
                                {
                                    _foundGestures[gesturesFound].X = _spacePoints[i].X;
                                    _foundGestures[gesturesFound].Y = _spacePoints[i].Y;
                                    _foundGestures[gesturesFound].Z = _spacePoints[i].Z;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // we've havent reached 4 points yet. Lets add new point
                            _foundGestures[gesturesFound].X = _spacePoints[i].X;
                            _foundGestures[gesturesFound].Y = _spacePoints[i].Y;
                            _foundGestures[gesturesFound].Z = _spacePoints[i].Z;
                            gesturesFound++;
                        }
                    }
                }
            }
            for (int j = 0; j < gesturesFound; j++)
            {
                // gotten all the points. Lets make them
                Gestures[j] = new Gesture(_foundGestures[j].X, _foundGestures[j].Y, _foundGestures[j].Z);
            }
            
        }
    }
}
        