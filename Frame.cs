using System;
using System.Collections.Generic;
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
        /// Global Gesture that is always alive, and used to keep the previous values in memory
        /// </summary>
        public Gesture TempGesture = new Gesture();

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
        private readonly CameraSpacePoint[] _foundGestures = new CameraSpacePoint[4];


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
                            (byte)(depth >= Screen.LeftEdge * 1000 && depth <= Screen.RightEdge * 1000 ? 
                                ((depth - Screen.LeftEdge*1000) * 256 / ((Screen.RightEdge - Screen.LeftEdge) * 1000)) 
                                : 0);
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

        /// <summary>
        /// Called on every frame update. This function decides what to do
        /// </summary>
        private void UseDepthFrameData()
        {
            switch (TouchWallApp.CalibrateStatus)
            {
                case 0: // Not in calibration mode
                    if (TouchWallApp.MultiTouchMode != 1)
                    {
                        FindSingleGesture();
                    }
                    else
                    {
                        FindGestures();
                    }
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
        /// Newer version of FindGestures that uses more advanced techniques to determine where to plot the user's finger.
        /// Sadly, it isnt able to do multiple points at the moment.
        /// </summary>
        private void FindSingleGesture()
        {
            // list of SpacePoints that are inside the screen space
            List<SpacePoint> pointsFound = new List<SpacePoint>();
            // cameraSpacePoint that contains the coordinates of the point that is closest to the screen
            CameraSpacePoint smallestYPoint = new CameraSpacePoint();
            smallestYPoint.Y = 1.0f;

            // go through each point.
            for (int i = (int) (_depthFrameDataSize/sizeof (ushort)) - 1; i >= 0; i--)
            {
                // if the point is within screen space...

                if (_spacePoints[i].X > Screen.BottomEdge - 0.05f && _spacePoints[i].X < Screen.TopEdge + 0.05f
                    && _spacePoints[i].Y > 0 && _spacePoints[i].Y < Screen.DetectThreshold
                    && _spacePoints[i].Z > Screen.LeftEdge - 0.05f && _spacePoints[i].Z < Screen.RightEdge + 0.05f + 0.15f)
                {
                    // ... add that point to the pointsFound List
                    var newPoint = new SpacePoint(_spacePoints[i], 0);
                    // check if this point is the closest to the screen
                    if (_spacePoints[i].Y < smallestYPoint.Y)
                    {
                        smallestYPoint = _spacePoints[i];
                    }
                    pointsFound.Add(newPoint);
                }
            }
            // pointsFound contains all the points in the screen space
            // now refine it
            List<SpacePoint> refinedPointsFound = new List<SpacePoint>();
            for (int i = 0; i < pointsFound.Count; i++)
            {
                if (Math.Abs(pointsFound[i].Point3D.X - smallestYPoint.X) < 0.1f && Math.Abs(pointsFound[i].Point3D.Z - smallestYPoint.Z) < 0.1f)
                {
                    refinedPointsFound.Add(pointsFound[i]);
                }
            }
            // refinedPointsFound contains a slither of the screen space

            int maxPoints = 2;
            List<SpacePoint> pointsToUse = new List<SpacePoint>();
            // go through each point in refinedPointsFound
            for (int i = 0; i < refinedPointsFound.Count; i++)
            {
                // and again
                for (int j = i + 1; j < refinedPointsFound.Count; j++)
                {
                    // check if the two points are near each other
                    float deltaX = refinedPointsFound[i].Point3D.X - refinedPointsFound[j].Point3D.X;
                    float deltaZ = refinedPointsFound[i].Point3D.Z - refinedPointsFound[j].Point3D.Z;
                    if (Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ) < 0.03f)
                    {
                        // if they are, then add to their total
                        refinedPointsFound[i].PointNear++;
                        refinedPointsFound[j].PointNear++;
                        if (refinedPointsFound[i].PointNear >= maxPoints)
                        {
                            // if the number of points near is greater than maxPoints, then add to pointsToUse
                            pointsToUse.Add(refinedPointsFound[i]);
                            // then close the j loop
                            j = refinedPointsFound.Count;
                        }
                    }
                }
            }
            // at this point, pointsToUse contains all the possible points. 
            // for single touch mode, simply find the one that has the smallest Y

            if (TouchWallApp.CurrentGestureType == 0)
            {
                TouchWallApp.CurrentGestureType = 1;
            }

            if (TouchWallApp.MultiTouchMode != 1)
            {
                // single point only
                float nearestY = 1.0f;
                int indexToUse = -1;
                for (int i = 0; i < pointsToUse.Count; i++)
                {
                    if (pointsToUse[i].Point3D.Y < nearestY)
                    {
                        indexToUse = i;
                        nearestY = pointsToUse[i].Point3D.Y;
                    }
                }
                if (indexToUse != -1)
                {
                    Gestures[0] = new Gesture(pointsToUse[indexToUse].Point3D.X, pointsToUse[indexToUse].Point3D.Y,
                        pointsToUse[indexToUse].Point3D.Z, 0);
                }
            }

        }


        /// <summary>
        /// Looks for user's hand and creates new gestures
        /// Old messy version, kept only because it can sort of do multipoints
        /// </summary>
        private void FindGestures()
        {
            for (int i = 0; i < 4; i++)
            {
                _foundGestures[i].X = -100000.0f;
                _foundGestures[i].Y = Screen.DetectThreshold;
                _foundGestures[i].Z = -100000.0f;
            }

            int gesturesFound = 0;
            for (int i = (int) (_depthFrameDataSize/sizeof (ushort)) - 1; i >= 0; i--)
            {
                if (_spacePoints[i].X > Screen.BottomEdge && _spacePoints[i].X < Screen.TopEdge
                    && _spacePoints[i].Y > 0 && _spacePoints[i].Y < Screen.DetectThreshold
                    && _spacePoints[i].Z > Screen.LeftEdge && _spacePoints[i].Z < Screen.RightEdge)
                {
                    Boolean foundNewPoint = true;
                    const float xTolerance = 0.07f;
                    const float yTolerance = 0.05f;
                    for (int a = 0; a < gesturesFound; a++)
                    {
                        if ((Math.Abs(_spacePoints[i].Z - _foundGestures[a].Z) < xTolerance
                             && Math.Abs(_spacePoints[i].X - _foundGestures[a].X) < yTolerance))
                        {
                            foundNewPoint = false;
                            break;
                        }

                    }
                    if (foundNewPoint)
                    {
                        if (TouchWallApp.CurrentGestureType == 0)
                        {
                            TouchWallApp.CurrentGestureType = 1;
                        }
                        if (gesturesFound == 1 && TouchWallApp.MultiTouchMode != 1)
                        {
                            break;
                        }
                        if (gesturesFound == 4)
                        {
                            // we've reached our limit of 4 guestures.
                            // check if this new point is closer to the screen than any of the existing points
                            // this means that the program will look for the nearest up to 4 points
                            int pointToReplace = 0;
                            // first find the point that is furthest from the screen
                            float furthestY = 0.0f;
                            for (int j = 0; j < gesturesFound; j++)
                            {
                                if (_foundGestures[j].Y > furthestY)
                                {
                                    pointToReplace = j;
                                    furthestY = _foundGestures[j].Y;
                                }
                            }
                            if (_foundGestures[pointToReplace].Y > _spacePoints[i].Y)
                            {
                                _foundGestures[pointToReplace].X = _spacePoints[i].X;
                                _foundGestures[pointToReplace].Y = _spacePoints[i].Y;
                                _foundGestures[pointToReplace].Z = _spacePoints[i].Z;
                            }
                            break;

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
            int[] goesTo = {-1, -1, -1, -1};

            if (gesturesFound != 1)
            {

                // goesTo[i] = j means that prev[j] -> _foundGestures[i]

                for (int i = 0; i < 4; i++)
                {
                    float prevX = TempGesture.GetPrevX(i);
                    float prevY = TempGesture.GetPrevY(i);
                    if (prevX > 0 && prevY > 0)
                    {
                        int closest = -1;
                        float closestDist = 0.05f; // threshold value
                        for (int j = 0; j < gesturesFound; j++)
                        {
                            if (goesTo[j] < 0)
                            {
                                // find the one that is closest to a previous gesture
                                // and log it into goTo
                                float dist =
                                    (float)
                                        Math.Sqrt((_foundGestures[j].Z - prevX)*(_foundGestures[j].Z - prevX) +
                                                  (_foundGestures[j].X - prevY)*(_foundGestures[j].X - prevY));
                                if (dist < closestDist)
                                {
                                    closest = j;
                                    closestDist = dist;
                                }
                            }
                        }
                        if (closest != -1)
                        {
                            goesTo[closest] = i;
                        }
                    }
                }
                // then for each goTo that is equal to -1 and is a new gesture, assign an id that isnt being used, and reset that id
                for (int j = 0; j < gesturesFound; j++)
                {
                    if (goesTo[j] < 0)
                    {
                        // find an unused id
                        for (int a = 0; a < 4; a++)
                        {
                            Boolean unusedId = true;
                            for (int b = 0; b < 4; b++)
                            {
                                if (goesTo[a] == b)
                                {
                                    unusedId = false;
                                    break;
                                }
                            }
                            if (unusedId)
                            {
                                TempGesture.ResetPrevX(a);
                                TempGesture.ResetPrevY(a);
                                goesTo[j] = a;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                goesTo[0] = 0;
            }
            for (int j = 0; j < gesturesFound; j++)
            {
                
                Gestures[j] = new Gesture(_foundGestures[j].X, _foundGestures[j].Y, _foundGestures[j].Z, goesTo[j]);
            }
            
        }
    }
}
        