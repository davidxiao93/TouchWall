using System;
using System.Runtime.InteropServices;

namespace TouchWall
{
    public class Gesture
    {
        /// <summary>
        /// X, Y, Z coordinates of hand from USER's point of view
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public int Id { get; set; }

        /// <summary>
        /// Constant defining the flag for left click down
        /// </summary>
        private const int MouseeventfLeftDown = 0x0002;

        /// <summary>
        /// Constant defining the flag for left click up
        /// </summary>
        private const int MouseeventfLeftUp = 0x0004;
        
        /// <summary>
        /// Constant defining the flag for mouse movement
        /// </summary>
        private const int MouseeventfMove = 0x0001;

        /// <summary>
        /// Constant defining the flag for making mouse_event move in an absolute manner
        /// </summary>
        private const int MouseeventfAbsolute = 0x8000;

        /// <summary>
        /// Previous Values. Kept alive by TempGesture
        /// </summary>
        private static readonly float[] PrevX = { -10000, -10000, -10000, -10000 };
        private static readonly float[] PrevY = { -10000, -10000, -10000, -10000 };

        /// <summary>
        /// Previous X and Y used to make clicking stick
        /// </summary>
        private static float _clickX;
        private static float _clickY;

        /// <summary>
        /// Getter for a previous value in X
        /// </summary>
        /// <param name="id">Which previous value in X</param>
        /// <returns>value of X</returns>
        public float GetPrevX(int id)
        {
            return PrevX[id];
        }

        /// <summary>
        /// Getter for a previous value in Y
        /// </summary>
        /// <param name="id">Which previous value in Y</param>
        /// <returns>value of Y</returns>
        public float GetPrevY(int id)
        {
            return PrevY[id];
        }

        /// <summary>
        /// Resets a previous value in X
        /// </summary>
        /// <param name="id">Which previous value in X</param>
        public void ResetPrevX(int id)
        {
            PrevX[id] = -10000;
        }

        /// <summary>
        /// Resets a previous value in Y
        /// </summary>
        /// <param name="id">Which previous value in Y</param>
        public void ResetPrevY(int id)
        {
            PrevY[id] = -10000;
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="y">CameraSpace X coordinate</param>
        /// <param name="z">CameraSpace Y coordinate</param>
        /// <param name="x">CameraSpace Z coordinate</param>
        /// <param name="id">ID of the point</param>
        public Gesture(float y, float z, float x, int id) // Coordinates in camera Space converted to USER space
        {
            X = x;
            Y = y;
            Z = z;
            Id = id;
            ProcessGesture();
        }

        /// <summary>
        /// Empty constructor for TempGesture
        /// </summary>
        public Gesture() // Coordinates in camera Space converted to USER space
        {
        }

        // Cursor Control events
        //[DllImport("user32")]
        //public static extern int SetCursorPos(int x, int y);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Applies Exponential Filtering
        /// </summary>
        private void ProcessGesture()
        {
            float width = Screen.RightEdge - Screen.LeftEdge;
            float height = Screen.TopEdge - Screen.BottomEdge;

            const float smoothingFactor = 0.2f;

            if (PrevX[Id] < -1000 || PrevY[Id] < -1000)
            {
                PrevX[Id] = X;
                PrevY[Id] = Y;
            }
            else
            {
                X = smoothingFactor * X + (1 - smoothingFactor) * PrevX[Id];
                Y = smoothingFactor * Y + (1 - smoothingFactor) * PrevY[Id];
                PrevX[Id] = X;
                PrevY[Id] = Y;
            }
            if (TouchWallApp.MultiTouchMode == 0)
            {
                int myX = (int)(Convert.ToDouble((X - Screen.LeftEdge) * 65535) / width);
                int myY = (int)(Convert.ToDouble((Screen.TopEdge - Y) * 65535) / height);
                InteractWithCursor(myX, myY);
            }
        }

        /// <summary>
        /// Moves the cursor
        /// </summary>
        /// <param name="x">value from 0 to 65535 representing x coordinate for mouse</param>
        /// <param name="y">value from 0 to 65535 representing y coordinate for mouse</param>
        private void InteractWithCursor(int x, int y)
        {
            switch (TouchWallApp.CurrentGestureType)
            {
                case 1:
                    if (Z < Screen.MouseDownThreshold && TouchWallApp.CursorStatus == 2)
                    {
                        // Left mouse button has gone down 
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftDown, x, y, 0, 0);
                        TouchWallApp.CurrentGestureType = 2;
                        _clickX = X;
                        _clickY = Y;
                    }
                    else if (Z < Screen.MouseMoveThreshold)
                    {
                        if (TouchWallApp.CursorStatus != 0)
                        {
                            mouse_event(MouseeventfAbsolute | MouseeventfMove, x, y, 0, 0);
                        }
                    }
                    else
                    {
                        PrevX[Id] = -10000;
                        PrevY[Id] = -10000;
                    }
                    break;
                case 2:
                    // User has just pressed down. Do not move cursor until it has moved a certain distance away
                    double tempDistance = Math.Sqrt((X - _clickX) * (X - _clickX) + (Y - _clickY) * (Y - _clickY));
                    if (Z > Screen.MouseUpThreshold)
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftUp, x, y, 0, 0);
                        TouchWallApp.CurrentGestureType = 1;
                    }
                    if (tempDistance > 0.01f)
                    {
                        // If distance moved has moved beyond a certain threshold, then the user has intended to click and drag
                        TouchWallApp.CurrentGestureType = 3;
                    }
                    break;
                case 3:
                    // User has pressed down and dragged at the same time
                    if (Z > Screen.MouseUpThreshold)
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftUp, x, y, 0, 0);
                        TouchWallApp.CurrentGestureType = 1;
                    }
                    else
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove, x, y, 0, 0);
                    }
                    break;
            }
        }
        
    }
}