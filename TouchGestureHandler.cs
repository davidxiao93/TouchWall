using System;
using System.Runtime.InteropServices;

namespace TouchWall
{
    public class TouchGestureHandler
    {
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
        private readonly int[] _prevMouseX = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// Storage for moving average of Y values
        /// </summary>
        private readonly int[] _prevMouseY = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        
        /// <summary>
        /// Variable describing status of mouse. 0 = no finger, 1 = move only, 2 = click only, 3 = click and drag
        /// </summary>
        private int _mouseStatus;

        /// <summary>
        /// Determines if the mouse can be moved. 0 = no movement, 1 = movemement, 2 = movement and clicking
        /// </summary>
        private int _mouseAllowed;

        public TouchGestureHandler()
        {
            for (int i = 0; i < _prevMouseX.Length / sizeof(int); i++)
            {
                _prevMouseX[i] = 0;
            }
            
            _mouseStatus = 0;
            _mouseAllowed = 1;
        }

        public int MouseStatus
        {
            get { return _mouseStatus; }
            set { _mouseStatus = value; }
        }

        public int MouseAllowed
        {
            get { return _mouseAllowed; }
            set { _mouseAllowed = value; }
        }

        // Cursor Control events
        //[DllImport("user32")]
        //public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// This method takes in the coordinates of the detected finger in CAMERA space, and converts it into mouse coordinates, as well as deciding if a mouse click/grag should occur
        /// </summary>
        /// <param name="spaceX">X coordinate in CAMERA space (Y in userspace)</param>
        /// <param name="spaceY">Y coordinate in CAMERA space (Z in userspace)</param>
        /// <param name="spaceZ">Z coordinate in CAMERA space (X in userspace)</param>
        public void ProcessGesture(float spaceX, float spaceY, float spaceZ, Screen screen)
        {
            float width = screen.RightEdge - screen.LeftEdge;
            float height = screen.TopEdge - screen.BottomEdge;

            int myX = (int)(Convert.ToDouble((spaceZ - screen.LeftEdge) * 65535) / width);
            int myY = (int)(Convert.ToDouble((screen.TopEdge - spaceX) * 65535) / height);

            if (_mouseStatus == 1)
            {
                // Cursor move mode only
                int oldValueX = 0, oldValueY = 0;

                for (int i = 0; i < _prevMouseX.Length / sizeof(int); i++)
                {
                    oldValueX += _prevMouseX[i];
                }

                for (int i = 0; i < _prevMouseY.Length / sizeof(int); i++)
                {
                    oldValueY += _prevMouseY[i];
                }

                if (spaceY < screen.MouseDownThreshold && _mouseAllowed == 2)
                {
                    // Left mouse button has gone down 
                    mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftDown,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);
                    _mouseStatus = 2;
                }
                else
                {
                    if (_mouseAllowed != 0)
                    {
                        mouse_event(MouseeventfAbsolute | MouseeventfMove,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);
                    }

                    for (int i = _prevMouseX.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseX[i] = _prevMouseX[i - 1];
                    }

                    for (int i = _prevMouseY.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseY[i] = _prevMouseY[i - 1];
                    }
                    _prevMouseX[0] = myX;
                    _prevMouseY[0] = myY;
                }
            }
            else if (_mouseStatus == 2)
            {
                // User has just pressed down. Do not move cursor until it has moved a certain distance away
                double tempDistance = Math.Sqrt((myX - _prevMouseX[0]) * (myX - _prevMouseX[0]) + (myY - _prevMouseY[0]) * (myY - _prevMouseY[0]));
                //InfoLabel.Content = tempDistance;

                if (tempDistance > 3000)
                {
                    // If distance moved has moved beyond a certain threshold, then the user has intended to click and drag
                    _mouseStatus = 3;
                }
            }
            else if (_mouseStatus == 3)
            {
                // User has pressed down and dragged at the same time
                int oldValueX = 0, oldValueY = 0;

                for (int i = 0; i < _prevMouseX.Length / sizeof(int); i++)
                {
                    oldValueX += _prevMouseX[i];
                }

                for (int i = 0; i < _prevMouseY.Length / sizeof(int); i++)
                {
                    oldValueY += _prevMouseY[i];
                }

                if (spaceY > screen.MouseUpThreshold)
                {
                    mouse_event(MouseeventfAbsolute | MouseeventfMove | MouseeventfLeftUp,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);
                    _mouseStatus = 1;
                }
                else
                {
                    mouse_event(MouseeventfAbsolute | MouseeventfMove,
                        ((oldValueX + myX) / (_prevMouseX.Length / sizeof(int) + 1)),
                        ((oldValueY + myY) / (_prevMouseY.Length / sizeof(int) + 1)), 0, 0);

                    for (int i = _prevMouseX.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseX[i] = _prevMouseX[i - 1];
                    }

                    for (int i = _prevMouseY.Length / sizeof(int) - 1; i > 0; i--)
                    {
                        _prevMouseY[i] = _prevMouseY[i - 1];
                    }

                    _prevMouseX[0] = myX;
                    _prevMouseY[0] = myY;
                }
            }
        }
    }
}