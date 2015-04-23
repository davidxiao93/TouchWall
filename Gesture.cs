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


        private static float[] prevX = { -10000, -10000, -10000, -10000 };
        private static float[] prevY = { -10000, -10000, -10000, -10000 };

        public float getPrevX(int id)
        {
            return prevX[id];
        }

        public float getPrevY(int id)
        {
            return prevY[id];
        }

        public void resetPrevX(int id)
        {
            prevX[id] = -10000;
        }

        public void resetPrevY(int id)
        {
            prevY[id] = -10000;
        }
        
        public Gesture(float y, float z, float x, int id) // Coordinates in camera Space converted to USER space
        {
            X = x;
            Y = y;
            Z = z;
            Id = id;
            ProcessGesture();
        }

        public Gesture() // Coordinates in camera Space converted to USER space
        {
        }

        // Cursor Control events
        //[DllImport("user32")]
        //public static extern int SetCursorPos(int x, int y);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        private void ProcessGesture()
        {
            float width = Screen.RightEdge - Screen.LeftEdge;
            float height = Screen.TopEdge - Screen.BottomEdge;

            const float smoothingFactor = 0.25f;

            if (prevX[Id] < 0 || prevY[Id] < 0)
            {
                prevX[Id] = X;
                prevY[Id] = Y;
            }
            else
            {
                X = smoothingFactor * X + (1 - smoothingFactor) * prevX[Id];
                Y = smoothingFactor * Y + (1 - smoothingFactor) * prevY[Id];
                prevX[Id] = X;
                prevY[Id] = Y;
            }
            if (TouchWallApp.MultiTouchMode == 0)
            {
                int myX = (int)(Convert.ToDouble((X - Screen.LeftEdge) * 65535) / width);
                int myY = (int)(Convert.ToDouble((Screen.TopEdge - Y) * 65535) / height);
                InteractWithCursor(myX, myY);
            }
        }

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
                    }
                    else if (Z < Screen.MouseMoveThreshold - 0.05f)
                    {
                        if (TouchWallApp.CursorStatus != 0)
                        {
                            mouse_event(MouseeventfAbsolute | MouseeventfMove, x, y, 0, 0);
                        }
                    }
                    else
                    {
                        prevX[Id] = -10000;
                        prevY[Id] = -10000;
                    }
                    break;
                case 2:
                    // User has just pressed down. Do not move cursor until it has moved a certain distance away
                    double tempDistance = Math.Sqrt((x - prevX[Id]) * (x - prevX[Id]) + (y - prevY[Id]) * (y - prevY[Id]));

                    if (tempDistance > 3000)
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