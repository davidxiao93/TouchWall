﻿using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

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
        /// Empty constructor for TempGesture
        /// </summary>
        public Gesture() // Coordinates in camera Space converted to USER space
        {
        }

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
                ICursor iCursor = CursorFactory.GetICursor();
                iCursor.InteractWithCursor(X, Y, Z);
            }
        }
    }

    
}