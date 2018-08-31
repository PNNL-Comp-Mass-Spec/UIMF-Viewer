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
    public class IonMobilityMain : System.Windows.Forms.Form
    {
        #region MICROSOFT FORM DATA
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.MainMenu mainMenu_Desktop;
        private System.Windows.Forms.MenuItem menubar_File;
        private System.Windows.Forms.MenuItem menubar_Graph;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogExperiment;
        private System.Windows.Forms.MenuItem menuGraph_SelectExperiment;
        private System.Windows.Forms.MenuItem menuProcess_SelectExperiment;
        #endregion

        private UIMF_File.DataViewer frame_dataViewer;

        private const string SETTINGS_FILE = @"settings.xml";

        public bool flag_Stopped = false;

        private System.Windows.Forms.MenuItem menuProcess_Batch;

        private string current_experiment_path = "";
        private ArrayList open_Experiments;
        private PictureBox pb_PNNLLogo;
        private MenuItem menuItem_About;
        private MenuItem menuitem_OpenFile;

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
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < this.open_Experiments.Count; i++)
                {
                    ((UIMF_File.DataViewer)this.open_Experiments[i]).Close();
                }

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IonMobilityMain));
            this.mainMenu_Desktop = new System.Windows.Forms.MainMenu(this.components);
            this.menubar_File = new System.Windows.Forms.MenuItem();
            this.menuitem_OpenFile = new System.Windows.Forms.MenuItem();
            this.menubar_Graph = new System.Windows.Forms.MenuItem();
            this.menuGraph_SelectExperiment = new System.Windows.Forms.MenuItem();
            this.menuItem_About = new System.Windows.Forms.MenuItem();
            this.menuProcess_SelectExperiment = new System.Windows.Forms.MenuItem();
            this.menuProcess_Batch = new System.Windows.Forms.MenuItem();
            this.folderBrowserDialogExperiment = new System.Windows.Forms.FolderBrowserDialog();
            this.pb_PNNLLogo = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pb_PNNLLogo)).BeginInit();
            this.SuspendLayout();
            //
            // mainMenu_Desktop
            //
            this.mainMenu_Desktop.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menubar_File,
            this.menubar_Graph,
            this.menuItem_About});
            //
            // menubar_File
            //
            this.menubar_File.Index = 0;
            this.menubar_File.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuitem_OpenFile});
            this.menubar_File.Text = "File";
            //
            // menuitem_OpenFile
            //
            this.menuitem_OpenFile.Index = 0;
            this.menuitem_OpenFile.Text = "Open";
            //
            // menubar_Graph
            //
            this.menubar_Graph.Index = 1;
            this.menubar_Graph.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuGraph_SelectExperiment});
            this.menubar_Graph.Text = "&Graph";
            //
            // menuGraph_SelectExperiment
            //
            this.menuGraph_SelectExperiment.Index = 0;
            this.menuGraph_SelectExperiment.Text = "&Experiment...";
            this.menuGraph_SelectExperiment.Click += new System.EventHandler(this.menuGraph_SelectExperiment_Click);
            //
            // menuItem_About
            //
            this.menuItem_About.Index = 2;
            this.menuItem_About.Text = "About";
            this.menuItem_About.Click += new System.EventHandler(this.menuItem_About_Click);
            //
            // menuProcess_SelectExperiment
            //
            this.menuProcess_SelectExperiment.Index = -1;
            this.menuProcess_SelectExperiment.Text = "";
            //
            // menuProcess_Batch
            //
            this.menuProcess_Batch.Index = -1;
            this.menuProcess_Batch.Text = "";
            //
            // folderBrowserDialogExperiment
            //
            this.folderBrowserDialogExperiment.Description = "Select an experiment folder";
            this.folderBrowserDialogExperiment.SelectedPath = "C:\\IonMobilityData";
            this.folderBrowserDialogExperiment.ShowNewFolderButton = false;
            //
            // pb_PNNLLogo
            //
            this.pb_PNNLLogo.BackColor = System.Drawing.Color.White;
            this.pb_PNNLLogo.BackgroundImage = global::IonMobility.Properties.Resources.PNNL_Color_Logo_Horizontal;
            this.pb_PNNLLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pb_PNNLLogo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pb_PNNLLogo.ImageLocation = "";
            this.pb_PNNLLogo.Location = new System.Drawing.Point(16, 12);
            this.pb_PNNLLogo.Name = "pb_PNNLLogo";
            this.pb_PNNLLogo.Size = new System.Drawing.Size(188, 84);
            this.pb_PNNLLogo.TabIndex = 39;
            this.pb_PNNLLogo.TabStop = false;
            //
            // IonMobilityMain
            //
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(222, 263);
            this.Controls.Add(this.pb_PNNLLogo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.Menu = this.mainMenu_Desktop;
            this.MinimumSize = new System.Drawing.Size(200, 100);
            this.Name = "IonMobilityMain";
            this.Text = "UIMF Viewer";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.IonMobilityAcqMain_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.IonMobilityAcqMain_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.IonMobilityAcqMain_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.pb_PNNLLogo)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

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
