using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace TouchWall
{
    /// <summary>
    /// Abstract template class for the null cursor and the normal cursor
    /// </summary>
    public abstract class ICursor
    {
        protected static ICursor Instance;
        public abstract int InteractWithMouse(float X, float Y, float Z);
    }

    class NullMouse : ICursor
    {
        /// <summary>
        /// Returns a singleton instance of NullMouse
        /// </summary>
        /// <returns></returns>
        public static ICursor GetNullCursor()
        {
            if (Instance == null)
            {
                Instance = new NullMouse();
            }
            return Instance;
        }

        public override int InteractWithMouse(float X, float Y, float Z)
        {
            // Purposefully provides no behaviour.
            return 0;
        }

    }

    class UseMouse : ICursor
    {
        /// <summary>
        /// Returns a singleton instance of UseMouse
        /// </summary>
        /// <returns></returns>
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


        private const int MouseeventfWheel = 0x0800;


        // Lets import mouse_event in order to enable cursor control
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Takes in the corrdinates and passes it to either cursor movement or scroll movement
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns>
        ///     1 if prev[ID] needs to be reset. This occurs when we don't want to keep the previous value
        ///             For example, when no points are detected
        ///     0 otherwise
        /// </returns>
        public override int InteractWithMouse(float X, float Y, float Z)
        {
            if (X < Screen.RightEdge + 0.05f)
            {
                return InteractWithCursor(X, Y, Z);
            }
            else
            {
                InteractWithScroll(X, Y, Z);
                return 0;
            }
            

        }

        /// <summary>
        /// Takes in the coordinates, converts to scroll wheel movement
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        private void InteractWithScroll(float X, float Y, float Z)
        {
            switch (TouchWallApp.CurrentGestureType)
            {
                case 1:
                    //point has been detected in the scroll space
                    if (Z < Screen.MouseDownThreshold)// && TouchWallApp.CursorStatus == 2)
                    {
                        // begin scrolling
                        TouchWallApp.CurrentGestureType = 5;
                        _clickX = X;
                        _clickY = Y;
                    }
                    break;
                case 5:
                    // user has pressed down in scroll space
                    double tempDistance = (Y - _clickY);
                    if (Z > Screen.MouseUpThreshold)
                    {
                        TouchWallApp.CurrentGestureType = 1;
                    }
                    if (tempDistance > 0.02f)
                    {
                        // implement a scroll down
                        mouse_event(MouseeventfWheel, 0, 0, -120, 0);
                        _clickX = X;
                        _clickY = Y;
                    }
                    else if (tempDistance < -0.02f)
                    {
                        // scroll up
                        mouse_event(MouseeventfWheel, 0, 0, 120, 0);
                        _clickX = X;
                        _clickY = Y;
                    }
                    break;
            }

        }


        /// <summary>
        /// Takes in coordinates, and converts it to x and y for the cursor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns>
        ///     1 if prev[ID] needs to be reset. This occurs when we don't want to keep the previous value
        ///             For example, when no points are detected
        ///     0 otherwise
        /// </returns>
        private int InteractWithCursor(float X, float Y, float Z)
        {
            float width = Screen.RightEdge - Screen.LeftEdge;
            float height = Screen.TopEdge - Screen.BottomEdge;

            // Find the x and y coordinates of the point in the units that mouse_event wants
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
                        // user has moved their hand far enough from the screen to cause a left click up
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftUp, x, y, 0, 0);
                        TouchWallApp.CurrentGestureType = 1;
                    }
                    else
                    {
                        // dragging the cursor
                        mouse_event(MouseeventfAbsolute | MouseeventfMove, x, y, 0, 0);
                    }
                    break;
                default:
                    TouchWallApp.CurrentGestureType = 0;
                    break;
            }
            return 0;
        }
    }
}
