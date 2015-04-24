using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace TouchWall
{
    public class TouchWallApp
    {
        #region Static Statuses
        
        /// <summary>
        /// Determines if the program is in calibration mode. 0 = Disabled, 1 = Enabled, 2 - 5 = Right Left Top Bottom
        /// </summary>
        public static int CalibrateStatus { get; set; }

        /// <summary>
        /// Describes current input. 0 = None, 1 = Movement, 2 = Click only, 3 = Click + Drag
        /// </summary>
        public static int CurrentGestureType { get; set; }

        /// <summary>
        /// Determines if the cursor can be moved. 0 = Disabled, 1 = Movement, 2 = Movement + Click, 3 = Movement + Scroll
        /// </summary>
        public static int CursorStatus { get; set; }

        /// <summary>
        /// Describes whether MultiTouch plotting mode is on of off. 0 = Disabled, 1 = Enabled
        /// </summary>
        public static int MultiTouchMode { get; set; }

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public static KinectSensor KinectSensor;

        #endregion
        
        /// <summary>
        /// Width resolution of the Kinect Sensor
        /// </summary>
        public const int KinectWidth = 512;

        /// <summary>
        /// Height resolution of the Kinect Sensor
        /// </summary>
        public const int KinectHeight = 424;

        /// <summary>
        /// Collects and processes frame data from sensor
        /// </summary>
        public readonly FrameDataManager FrameDataManager;

        /// <summary>
        /// Responsible for calibrating and knowing dimensions of the screen
        /// </summary>
        private readonly Screen _screen;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private readonly KinectAudioStream _convertStream;

        /// <summary>
        /// Handle for the mainWindow. Sorry for implementing it in this way
        /// </summary>
        public readonly MainWindow ParentMainWindow;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private readonly SpeechRecognitionEngine _speechEngine;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="mainWindow"></param>
        public TouchWallApp(MainWindow mainWindow)
        {
            ParentMainWindow = mainWindow;

            // Get the _kinectSensor object
            KinectSensor = KinectSensor.GetDefault();
            
            // Create the screen for calibration
            _screen = new Screen();
            
            // Create a manager for processing sensor data from the kinect
            FrameDataManager = new FrameDataManager();

            // Open the sensor
            KinectSensor.Open();

            // Set the statuses: Calibration disabled, No current gesture, Cursor with movement, MultiTouch disabled
            CalibrateStatus = 0;
            CurrentGestureType = 0;
            CursorStatus = 1;
            MultiTouchMode = 0;

            #region Speech

            // Grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = KinectSensor.AudioSource.AudioBeams;
            Stream audioStream = audioBeamList[0].OpenInputStream();

            // Create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            RecognizerInfo recognizerInfo = TryGetKinectRecognizer();

            if (recognizerInfo != null)
            {
                _speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("Kinect Calibrate Enable", "CALIBRATE_FULL"));
                directions.Add(new SemanticResultValue("Kinect Calibrate Disable", "CALIBRATE_CANCEL"));
                directions.Add(new SemanticResultValue("Kinect Cursor Enable", "CURSOR_ENABLE"));
                directions.Add(new SemanticResultValue("Kinect Cursor Disable", "CURSOR_DISABLE"));
                directions.Add(new SemanticResultValue("Kinect Click Enable", "CLICK_ENABLE"));
                directions.Add(new SemanticResultValue("Kinect Click Disable", "CLICK_DISABLE"));
                directions.Add(new SemanticResultValue("Kinect Scroll Enable", "SCROLL_ENABLE"));
                directions.Add(new SemanticResultValue("Kinect Scroll Disable", "SCROLL_DISABLE"));
                directions.Add(new SemanticResultValue("Kinect Depth Enable", "DEPTH_START"));
                directions.Add(new SemanticResultValue("Kinect Depth Disable", "DEPTH_END"));
                directions.Add(new SemanticResultValue("Kinect Multi Enable", "MULTI_START"));
                directions.Add(new SemanticResultValue("Kinect Multi Disable", "MULTI_END"));
                directions.Add(new SemanticResultValue("Kinect Open TouchDevelop", "TOUCHDEVELOP"));


                var gb = new GrammarBuilder { Culture = recognizerInfo.Culture };
                gb.Append(directions);
                var g = new Grammar(gb);
                _speechEngine.LoadGrammar(g);

                _speechEngine.SpeechRecognized += SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // Let the convertStream know speech is going active
                _convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                //speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                _speechEngine.SetInputToAudioStream(_convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }

            #endregion
        }

        /// <summary>
        /// switch the cursor mode
        /// </summary>
        public void ToggleCursor()
        {
            CursorStatus ++;
            if (CursorStatus == 4)
            {
                CursorStatus = 0;
            }
          
        }

        /// <summary>
        /// Initiate the calibration process
        /// </summary>
        public void BeginCalibration()
        {
            _screen.BeginCalibration();
        }

        /// <summary>
        /// Kills the speech recognition when the window gets closed
        /// </summary>
        public void DisableSpeech()
        {
            if (_convertStream != null)
            {
                _convertStream.SpeechActive = false;
            }

            if (_speechEngine != null)
            {
                _speechEngine.SpeechRecognized -= SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                _speechEngine.RecognizeAsyncStop();
            }
        }

        /// <summary>
        /// Kills the Kinect connection when the window gets closed
        /// </summary>
        public void DisableKinect()
        {
            if (KinectSensor != null)
            {
                KinectSensor.Close();
                KinectSensor = null;
            }
        }

        #region SpeechMethods
        
        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to process audio from Kinect
        /// </summary>
        /// <returns>
        /// <code>RecognizerInfo</code> if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected.
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            return null;
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double confidenceThreshold = 0.3;

            if (e.Result.Confidence >= confidenceThreshold)
            {


                switch (e.Result.Semantics.Value.ToString())
                {
                    case "CALIBRATE_FULL":
                        ParentMainWindow.CalibrateClick();
                        _screen.BeginCalibration();
                        CursorStatus = 0;
                        break;
                    case "CALIBRATE_CANCEL":
                        _screen.CancelCalibration();
                        CursorStatus = 1;
                        break;
                    case "CURSOR_DISABLE":
                        CursorStatus = 0;
                        break;
                    case "CURSOR_ENABLE":
                        if (CursorStatus == 0 && MultiTouchMode == 0)
                        {
                            CursorStatus = 1;
                        }
                        break;
                    case "CLICK_DISABLE":
                    case "SCROLL_DISABLE":
                        if (CursorStatus != 0 && MultiTouchMode == 0)
                        {
                            CursorStatus = 1;
                        }
                        break;
                    case "CLICK_ENABLE":
                        if (MultiTouchMode == 0)
                        {
                            CursorStatus = 2;
                        }
                        break;
                    case "SCROLL_ENABLE":
                        if (MultiTouchMode == 0)
                        {
                            CursorStatus = 3;
                        }
                        break;
                    case "DEPTH_START":
                        if (MultiTouchMode != 2)
                        {
                            ParentMainWindow.OpenDepthTouchWindow();
                            MultiTouchMode = 2;
                        }
                        break;
                    case "DEPTH_END":
                        if (MultiTouchMode == 2)
                        {
                            ParentMainWindow.CloseDepthTouchWindow();
                            MultiTouchMode = 0;
                        }
                        break;
                    case "MULTI_START":
                        if (MultiTouchMode != 1)
                        {
                            ParentMainWindow.OpenMultiTouchWindow();
                            MultiTouchMode = 1;
                        }
                        break;
                    case "MULTI_END":
                        if (MultiTouchMode == 1)
                        {
                            ParentMainWindow.CloseMultiTouchWindow();
                            MultiTouchMode = 0;
                        }
                        break;
                    case "TOUCHDEVELOP":
                        Process.Start("https://www.touchdevelop.com/app/");
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
        }
        
        #endregion
    }
}