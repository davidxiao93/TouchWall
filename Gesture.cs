using System;

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
        /// Id property. Initial method of keeping multiple points unique and allow tracking (not fully implemented)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Previous Values. Kept alive by TempGesture
        /// </summary>
        private static readonly float[] PrevX = { -10000, -10000, -10000, -10000 };
        private static readonly float[] PrevY = { -10000, -10000, -10000, -10000 };

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
        /// Empty constructor for TempGesture in FindGesture
        /// </summary>
        public Gesture()
        {
        }

        /// <summary>
        /// Applies Exponential Filtering
        /// </summary>
        private void ProcessGesture()
        {
            
            float prevX = PrevX[Id];
            float prevY = PrevY[Id];

            if (prevX < -1000 || prevY < -1000)
            {
                PrevX[Id] = X;
                PrevY[Id] = Y;
            }
            else
            {
                // Using the difference between current vvalues of X and Y with PrevX and PrevY to determine smoothing factor instead of using a constant
                float tempDistance = (float)Math.Sqrt((X - prevX) * (X - prevX) + (Y - prevY) * (Y - prevY));
                float smoothingFactor = tempDistance * 10 + 0.01f;
                if (smoothingFactor > 1)
                {
                    smoothingFactor = 1;
                }

                // apply exponential filter
                X = smoothingFactor * X + (1 - smoothingFactor) * prevX;
                Y = smoothingFactor * Y + (1 - smoothingFactor) * prevY;
                PrevX[Id] = X;
                PrevY[Id] = Y;
            }
            ICursor iCursor = CursorFactory.GetICursor();
            if (iCursor.InteractWithMouse(X, Y, Z) == 1)
            {
                ResetPrevX(Id);
                ResetPrevY(Id);
            }
        }
    }
}