using System;
using System.Net;
using System.Runtime.InteropServices;

namespace TouchWall
{
    public abstract class ICursor
    {
        protected static ICursor Instance;
        public abstract int InteractWithCursor(float X, float Y, float Z);
    }

    class NullMouse : ICursor
    {
        public static ICursor GetNullCursor()
        {
            if (Instance == null)
            {
                Instance = new NullMouse();
            }
            return Instance;
        }
        
        public override int InteractWithCursor(float X, float Y, float Z)
        {
            // Purposefully provides no behaviour.
            return 0;
        }

    }

    class UseMouse : ICursor
    {

        public static ICursor GetUseCursor()
        {
            if (Instance == null)
            {
                Instance = new UseMouse();
            }
            return Instance;
        }

        /// <summary>
        /// Previous X and Y used to make clicking stick
        /// </summary>
        private static float _clickX;
        private static float _clickY;

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

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Takes in the corrdinates in USER SPACE and convers it to the Screen for cursor control
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns>
        ///     1 if prev[ID] needs to be reset
        ///     0 otherwise
        /// </returns>
        public override int InteractWithCursor(float X, float Y, float Z)
        {
            float width = Screen.RightEdge - Screen.LeftEdge;
            float height = Screen.TopEdge - Screen.BottomEdge;
            int x = (int)(Convert.ToDouble((X - Screen.LeftEdge) * 65535) / width);
            int y = (int)(Convert.ToDouble((Screen.TopEdge - Y) * 65535) / height);
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
                        return 1;
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
            return 0;
        }
    }
}
