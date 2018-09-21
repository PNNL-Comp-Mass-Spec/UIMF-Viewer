using System;
using System.Collections.Generic;
using System.IO;
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

namespace UIMFViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.ShowDialog();
        }

        private void Main_OnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var path = CheckValidPath(e);

            if (path == null)
            {
                MessageBox.Show("Just one file please.");
                return;
            }

            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.ItemDropped(path);
            }
        }

        private void Main_OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            if (CheckValidPath(e) != null)
            {
                e.Effects = DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private string CheckValidPath(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) &&
                e.Data.GetData(DataFormats.FileDrop, true) is string[] filePaths &&
                filePaths.Length == 1 && (File.Exists(filePaths[0]) || Directory.Exists(filePaths[0])))
            {
                return filePaths[0];
            }

            return null;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Locate the window 20 pixels from the top right corner of the primary screen
            var primaryDesktopWorkArea = SystemParameters.WorkArea;
            Top = primaryDesktopWorkArea.Top + 20;
            Left = primaryDesktopWorkArea.Right - Width - 20;
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            Topmost = true;
            Activate();
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void MainWindow_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.CloseChildren();
            }
        }
    }
}
