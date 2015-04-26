using System.ComponentModel;
using System.Windows;

namespace TouchWall
{

    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        private static HelpWindow _instance;
        private HelpWindow()
        {
            InitializeComponent();
        }

        public static HelpWindow OpenHelpWindow()
        {
            if (_instance == null)
            {
                _instance = new HelpWindow();
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
