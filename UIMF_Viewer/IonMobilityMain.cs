//#define THERMO

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using UIMF_DataViewer;

// ******************************************************************************************************
// * Programmer:  William Danielson
// *
// * Description:  Main Central Object that controls everything for the desktop.
// *
// * Revisions:
// *    090130 - Added the ability to do TIC Threshold Counting.  I expect to remove it or somehow prevent
// *             the code from defaulting to calculate it everytime.  Need for speed!
// *    090130 - Made the btn_cmdStart change state faster and highlight the cb_DisableSpectrometer field red
// *             when attempting to start the mass spectrometer having the voltages disabled.
// *
// *
namespace IonMobility
{
    /// <summary>
    /// Summary description for IonMobilityAcqMain.
    /// </summary>
    public partial class IonMobilityMain : System.Windows.Forms.Form
    {

        private UIMF_File.DataViewer frame_dataViewer;

        private const string SETTINGS_FILE = @"settings.xml";

        public bool flag_Stopped = false;

        private string current_experiment_path = "";
        private ArrayList open_Experiments;

        IonMobility.Form_About ptr_form_about = new Form_About();

        /******************************************************************************
         *  IonMobilityAcqMain Constructor
         */
        public IonMobilityMain(Form_About form_about, string[] args)
        {
            ptr_form_about = form_about;

            this.open_Experiments = new ArrayList();

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            this.Height = (this.Height - this.ClientSize.Height) + this.pb_PNNLLogo.Top + this.pb_PNNLLogo.Height - 6;

            if ((args != null) && (args.Length > 0))
                this.GraphExperiment(args[0]);
        }

        private void Check_Minimized(object obj, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                for (int i = 0; i < this.open_Experiments.Count; i++)
                    ((System.Windows.Forms.Form)this.open_Experiments[i]).WindowState = FormWindowState.Minimized;
            }
        }
        private void IonMobilityAcqMain_Load(object sender, System.EventArgs e)
        {
            this.Top = 20;

            // reduce the screen down to the single panel acquire.
            int screen_button_X = this.Left + this.Left;

            this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - this.Width - 20;

            ptr_form_about.Close();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool created;
            System.Threading.Mutex mtx = new System.Threading.Mutex(false,
                "UIMF_Viewer_Mutex", out created);

            if (created)
            {
                try
                {
                    Form_About form_about = new Form_About();

                    Application.Run(new IonMobilityMain(form_about, args));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString() + "\n\nStack Trace:  \n" + ex.StackTrace.ToString());
                }
            }
            else
            {
                MessageBox.Show("Another instance of " + AppDomain.CurrentDomain.FriendlyName + " is currently running.");
            }
            mtx.Close();
        }

        // Select an experiment folder and graph it
        private void menuGraph_SelectExperiment_Click(object sender, System.EventArgs e)
        {
            this.folderBrowserDialogExperiment.Description = "Select Experiment Folder to Graph";

            DialogResult res = folderBrowserDialogExperiment.ShowDialog(this);
            if (res != DialogResult.OK)
                return;

            if (sender == this.menuGraph_SelectExperiment)
                this.GraphExperiment(folderBrowserDialogExperiment.SelectedPath);
        }

        // Graph the current experiment
        private void menuGraph_CurrentExperiment_Click(object sender, System.EventArgs e)
        {
            string path = Path.Combine(this.current_experiment_path, Path.GetFileName(this.current_experiment_path));
            path += ".xml";

            if (File.Exists(path))
                this.GraphExperiment(this.current_experiment_path);
            else
                this.menuGraph_SelectExperiment_Click((object)null, (System.EventArgs)null);
        }

        private void GraphExperiment(string path)
        {
            // limit the total number of experiments open.
            if (this.open_Experiments.Count > 4)
            {
                int total_experiments = this.open_Experiments.Count;
                for (int i = total_experiments - 1; i >= 0; i--)
                    if (((System.Windows.Forms.Form)this.open_Experiments[i]).IsDisposed)
                        this.open_Experiments.RemoveAt(i);

                if (this.open_Experiments.Count > 4)
                {
                    MessageBox.Show("You can have 5 experiments open at a time.  Please close an experiment before opening another.");
                    return;
                }
            }

            try
            {
                this.frame_dataViewer = new UIMF_File.DataViewer(path, true);
                this.frame_dataViewer.num_TICThreshold.Value = 300;

                this.open_Experiments.Add(this.frame_dataViewer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        string raw_filename = "";
        private void IonMobilityAcqMain_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 1)
            {
                MessageBox.Show("Just one file please.");
                return;
            }

            //detect whether its a directory or file
            FileAttributes attr = File.GetAttributes(files[0]);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                files = System.IO.Directory.GetFiles(files[0], "*.UIMF");
                if (files.Length == 0)
                    return;
            }

            if (this.open_Experiments.Count > 4)
            {
                int total_experiments = this.open_Experiments.Count;
                for (int i = total_experiments - 1; i >= 0; i--)
                    if (((System.Windows.Forms.Form)this.open_Experiments[i]).IsDisposed)
                        this.open_Experiments.RemoveAt(i);

                if (this.open_Experiments.Count > 4)
                {
                    MessageBox.Show("You can have 5 experiments open at a time.  Please close an experiment before opening another.");
                    return;
                }
            }

            if (Path.GetExtension(files[0]).ToUpper() == ".UIMF")
            {
                try
                {
                    this.frame_dataViewer = new UIMF_File.DataViewer(files[0], true);
                    this.open_Experiments.Add(this.frame_dataViewer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else if (Path.GetExtension(files[0]).ToUpper() == ".RAW")
            {
                if (MessageBox.Show("Convert RAW file to UIMF? ", "File Conversion", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        UIMF_DataViewer.Raw2UIMF raw2uimf = new Raw2UIMF(files[0]);
                        raw2uimf.ConvertRAWtoUIMF(Path.Combine(Path.GetDirectoryName(files[0]), Path.GetFileNameWithoutExtension(files[0]) + ".UIMF"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }

#if false
   // MessageBox.Show(this, "here");
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 1)
            {
                MessageBox.Show("Just one file please.");
                return;
            }

            if (this.open_Experiments.Count > 4)
            {
                int total_experiments = this.open_Experiments.Count;
                for (int i = total_experiments - 1; i >= 0; i--)
                    if (((System.Windows.Forms.Form)this.open_Experiments[i]).IsDisposed)
                        this.open_Experiments.RemoveAt(i);

                if (this.open_Experiments.Count > 4)
                {
                    MessageBox.Show("We allow 5 experiments open at a time.  Please close an experiment before opening another.");
                    return;
                }
            }
#if THERMO
            if (Path.GetExtension(files[0]).ToLower() == ".raw")
            {
                MessageBox.Show(this, "raw");
                this.raw_filename = files[0];
                Invoke(new ThreadStart(invoke_RAWFile));
                return;
            }
#endif
            // Load the DataViewer
            this.frame_dataViewer = new UIMF_File.DataViewer(files[0], true);

            this.frame_dataViewer.num_TICThreshold.Value = 300;

            // DragDrop: set the imfReader and it will never be changed.
            //           set the current_frame_number to -1
            // this.frame_dataViewerInt.Disposed += new EventHandler(frame_dataViewer_Disposed);
            this.open_Experiments.Add(this.frame_dataViewer);
#endif
        }

#if false
        public void invoke_RAWFile()
        {
            MessageBox.Show("invoke");
            try
            {
                UIMF_File.RAW_Data rawData = new UIMF_File.RAW_Data();
                MessageBox.Show("hmm:  " + this.raw_filename);
                rawData.OpenFile(this.raw_filename);
                if (rawData.isOpen())
                {
                    MessageBox.Show("success");
                }
                else
                    MessageBox.Show("fail");

//                clsRawData rawData = new clsRawData();
             //   rawData.LoadFile(this.raw_filename, DeconToolsV2.Readers.FileType.FINNIGAN);
              //  MessageBox.Show(rawData.GetScanSize().ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
#endif

        private void IonMobilityAcqMain_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void menuItem_About_Click(object sender, EventArgs e)
        {
            if (this.ptr_form_about.IsDisposed)
                this.ptr_form_about = new Form_About();

            if (this.ptr_form_about.Visible)
                this.ptr_form_about.Hide();
            else
                this.ptr_form_about.Show();
            this.ptr_form_about.Update();
        }
    }
}


namespace IonMobility
{
    public interface IRegistryPersist
    {
        void RegistrySave(RegistryKey key);
        void RegistryLoad(RegistryKey key);
    }
}
