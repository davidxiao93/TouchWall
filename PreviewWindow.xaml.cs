using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TouchWall
{
    /// <summary>
    /// Interaction logic for PreviewWindow.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        private static TouchWallApp _touchWall;
        private static PreviewWindow _instance;

        private PreviewWindow(TouchWallApp TouchWall)
        {
            _touchWall = TouchWall;
            //_touchWall.FrameDataManager.DepthFrameReader.FrameArrived += _touchWall.FrameDataManager.Reader_FrameArrived;
            InitializeComponent();
        }

        public static PreviewWindow OpenPreviewWindow(TouchWallApp TouchWall)
        {
            if (_instance == null)
            {
                _instance = new PreviewWindow(TouchWall);
            }
            return _instance;
        }

        public ImageSource ImageSource
        {
            get { return FrameDataManager.DepthBitmap; }
        }
    }
}
