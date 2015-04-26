﻿namespace TouchWall
{
    class ScreenMemento
    {
        /// <summary>
        /// Previous distance between sensor and left edge of screen, used for calibration
        /// </summary>
        public readonly float LeftEdge;

        /// <summary>
        /// Previous distance between sensor and right edge of screen, used for calibration
        /// </summary>
        public readonly float RightEdge;

        /// <summary>
        /// Previous distance between sensor and top edge of screen, used for calibration
        /// </summary>
        public readonly float TopEdge;

        /// <summary>
        /// Previous distance between sensor and bottom edge of screen, used for calibration
        /// </summary>
        public readonly float BottomEdge;

        public ScreenMemento(float currentTop, float currentLeft, float currentRight, float currentBottom)
        {
            TopEdge = currentTop;
            LeftEdge = currentLeft;
            RightEdge = currentRight;
            BottomEdge = currentBottom;
        }
    }
}
