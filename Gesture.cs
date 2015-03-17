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
        /// Storage for moving average of X values
        /// </summary>
        private static readonly int[] PrevMouseX = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// Storage for moving average of Y values
        /// </summary>
        private static readonly int[] PrevMouseY = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        
        public Gesture(float y, float z, float x) // Coordinates in camera Space converted to USER space
        {
            X = x;
            Y = y;
            Z = z;
            ProcessGesture();
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

            int myX = (int) (Convert.ToDouble((X - Screen.LeftEdge)*65535)/width);
            int myY = (int) (Convert.ToDouble((Screen.TopEdge - Y)*65535)/height);

            int oldValueX = 0, oldValueY = 0;
            for (int i = 0; i < PrevMouseX.Length/sizeof (int); i++)
            {
                oldValueX += PrevMouseX[i];
            }
            for (int i = 0; i < PrevMouseY.Length/sizeof (int); i++)
            {
                oldValueY += PrevMouseY[i];
            }

            if (TouchWallApp.MultiTouchMode == 0)
            {
                InteractWithCursor(myX, myY, oldValueX, oldValueY);
            }
        }

        private void InteractWithCursor(int myX, int myY, int oldValueX, int oldValueY)
        {
            switch (TouchWallApp.CurrentGestureType)
            {
                case 1:
                    if (Z < Screen.MouseDownThreshold && TouchWallApp.CursorStatus == 2)
                    {
                        // Left mouse button has gone down 
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftDown,
                            ((oldValueX + myX)/(PrevMouseX.Length/sizeof (int) + 1)),
                            ((oldValueY + myY)/(PrevMouseY.Length/sizeof (int) + 1)), 0, 0);
                        TouchWallApp.CurrentGestureType = 2;
                    }
                    else
                    {
                        if (TouchWallApp.CursorStatus != 0)
                        {
                            mouse_event(MouseeventfAbsolute | MouseeventfMove,
                                ((oldValueX + myX)/(PrevMouseX.Length/sizeof (int) + 1)),
                                ((oldValueY + myY)/(PrevMouseY.Length/sizeof (int) + 1)), 0, 0);
                        }

                        for (int i = PrevMouseX.Length/sizeof (int) - 1; i > 0; i--)
                        {
                            PrevMouseX[i] = PrevMouseX[i - 1];
                        }

                        for (int i = PrevMouseY.Length/sizeof (int) - 1; i > 0; i--)
                        {
                            PrevMouseY[i] = PrevMouseY[i - 1];
                        }
                        PrevMouseX[0] = myX;
                        PrevMouseY[0] = myY;
                    }
                    break;
                case 2:
                    // User has just pressed down. Do not move cursor until it has moved a certain distance away
                    double tempDistance = Math.Sqrt((myX - PrevMouseX[0])*(myX - PrevMouseX[0]) + (myY - PrevMouseY[0])*(myY - PrevMouseY[0]));

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
                        mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftUp,
                            ((oldValueX + myX)/(PrevMouseX.Length/sizeof (int) + 1)),
                            ((oldValueY + myY)/(PrevMouseY.Length/sizeof (int) + 1)), 0, 0);
                        TouchWallApp.CurrentGestureType = 1;
                    }
                    else
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove,
                            ((oldValueX + myX)/(PrevMouseX.Length/sizeof (int) + 1)),
                            ((oldValueY + myY)/(PrevMouseY.Length/sizeof (int) + 1)), 0, 0);

                        for (int i = PrevMouseX.Length/sizeof (int) - 1; i > 0; i--)
                        {
                            PrevMouseX[i] = PrevMouseX[i - 1];
                        }

                        for (int i = PrevMouseY.Length/sizeof (int) - 1; i > 0; i--)
                        {
                            PrevMouseY[i] = PrevMouseY[i - 1];
                        }

                        PrevMouseX[0] = myX;
                        PrevMouseY[0] = myY;
                    }
                    break;
            }
        }
    }
}