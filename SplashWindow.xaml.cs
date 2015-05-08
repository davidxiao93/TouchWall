using System.ComponentModel;
using System.Windows;

namespace TouchWall
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        /// <summary>
        /// Static instance to make sure only one instance exists
        /// </summary>
        private static SplashWindow _instance;

        private SplashWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when mainWindow requests this window to open
        /// </summary>
        /// <returns></returns>
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
