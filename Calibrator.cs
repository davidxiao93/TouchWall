namespace TouchWall
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;

    public class Calibrator
    {
        /// <summary>
        /// Distance between sensor and left edge of screen in metres
        /// </summary>
        public float LeftOfScreen { get; set; }

        /// <summary>
        /// Distance between sensor and right edge of screen in metres
        /// </summary>
        public float RightOfScreen { get; set; }

        /// <summary>
        /// Distance between sensor and top edge of screen in metres
        /// </summary>
        public float TopOfScreen { get; set; }

        /// <summary>
        /// Distance between sensor and bottom edge of screen in metres
        /// </summary>
        public float BottomOfScreen { get; set; }

        /// <summary>
        /// Previous distance between sensor and left edge of screen in metres, used for calibration
        /// </summary>
        public float OldLeftOfScreen { get; set; }

        /// <summary>
        /// Previous distance between sensor and right edge of screen in metres, used for calibration
        /// </summary>
        public float OldRightOfScreen { get; set; }

        /// <summary>
        /// Previous distance between sensor and top edge of screen in metres, used for calibration
        /// </summary>
        public float OldTopOfScreen { get; set; }

        /// <summary>
        /// Previous distance between sensor and bottom edge of screen in metres, used for calibration
        /// </summary>
        public float OldBottomOfScreen { get; set; }

        /// <summary>
        /// Distance from screen before moving the mouse in meters
        /// </summary>
        public float MouseMoveThreshold { get; set; }

        /// <summary>
        /// Distance from screen before registering a left click down in meters
        /// </summary>
        public float MouseDownThreshold { get; set; }

        /// <summary>
        /// Distance from screen before registering a left click up in meters
        /// </summary>
        public float MouseUpThreshold { get; set; }

        public Calibrator()
        {
            this.LeftOfScreen = 0.6f;
            this.RightOfScreen = 0.7f;
            this.TopOfScreen = 0.19f;
            this.BottomOfScreen = -0.11f;
            this.OldLeftOfScreen = 0.6f;
            this.OldRightOfScreen = 0.7f;
            this.OldTopOfScreen = 0.19f;
            this.OldBottomOfScreen = -0.11f;
            this.MouseMoveThreshold = 0.12f;
            this.MouseDownThreshold = 0.003f;
            this.MouseUpThreshold = 0.006f;
        }
    }
}