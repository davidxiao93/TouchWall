using System.ComponentModel;
using System.Windows;

namespace TouchWall
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private static SplashWindow _instance;

        private SplashWindow()
        {
            InitializeComponent();
        }

        public static SplashWindow OpenSplashWindow()
        {
            if (_instance == null)
            {
                _instance = new SplashWindow();
            }
            return _instance;
        }

        /// <summary>
        /// Called when the Close button in the top right is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WindowClosing(object sender, CancelEventArgs e)
        {
            _instance = null;
        }
    }
}
