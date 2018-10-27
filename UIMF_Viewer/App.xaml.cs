using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UIMFViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex singleInstanceMutex = null;

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                var path = e.Args[0];
                if (File.Exists(path) || Directory.Exists(path))
                {
                    var uimf = MainWindowViewModel.GetUimfFileInPath(path);
                    if (!string.IsNullOrWhiteSpace(uimf))
                    {
                        var dataViewer = new UIMF_File.DataViewer(uimf, true);
                        dataViewer.Closed += DataViewer_Closed;

                        dataViewer.Show();
                    }
                }

                return;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            MainLoad();
        }

        private void DataViewer_Closed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Exception: {e.Exception}");
            e.Handled = true;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Exception: {e.ExceptionObject}");
        }

        private void MainLoad()
        {
            var mutexName = "Global\\" + Assembly.GetExecutingAssembly().GetName().Name;
            // Use a mutex to ensure a single copy of program is running. If we can create a new mutex then
            //      no instance of the application is running. Otherwise, we exit.
            // Code adapted from K. Scott Allen's OdeToCode.com at
            //      http://odetocode.com/Blogs/scott/archive/2004/08/20/401.aspx
            //singleInstanceMutex = new Mutex(false, mutexName);
            singleInstanceMutex = new Mutex(false, "UIMF_Viewer_Mutex");
            try
            {
                if (!singleInstanceMutex.WaitOne(0, false))
                {
                    MessageBox.Show("A copy of UIMF Viewer is already running.");
                    return;
                }
            }
            catch (AbandonedMutexException)
            {
                // The last UIMF Viewer session did not close properly (AbandonedMutexException)
            }

            var main = new MainWindow() { DataContext = new MainWindowViewModel() };
            Application.Current.MainWindow = main;
            MainWindow = main;
            main.Show();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            singleInstanceMutex?.ReleaseMutex();
            singleInstanceMutex?.Dispose();
        }
    }
}
