using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
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
        /// Determines if the cursor can be moved. 0 = Disabled, 1 = Movement, 2 = Movement + Click
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
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private readonly SpeechRecognitionEngine _speechEngine;

        public TouchWallApp()
        {
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
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // Create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            RecognizerInfo recognizerInfo = TryGetKinectRecognizer();

            if (recognizerInfo != null)
            {
                _speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);

                var directions = new Choices();
                directions.Add(new SemanticResultValue("Kinect Calibrate", "CALIBRATE"));
                directions.Add(new SemanticResultValue("Kinect Cancel", "CANCELCALIBRATE"));

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

        public void ToggleCursor()
        {
            switch (CursorStatus)
            {
                case 1: // Switch to cursor with click
                    CursorStatus = 2;
                    break;
                case 2: // Switch to disabled
                    CursorStatus = 0;
                    break;
                default: // Switch to cursor without click
                    CursorStatus = 1;
                    break;
            }
        }

        public void BeginCalibration()
        {
            _screen.BeginCalibration();
        }

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

        public void DisableKinect()
        {
            if (KinectSensor != null)
            {
                KinectSensor.Close();
                KinectSensor = null;
            }
        }

        #region Speech2
        
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
                    case "CALIBRATE":
                        _screen.BeginCalibration();
                        CursorStatus = 0;
                        break;
                    case "CANCELCALIBRATE":
                        _screen.CancelCalibration();
                        CursorStatus = 1;
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