using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using UIMF_DataViewer;
using UIMF_File;

namespace IonMobility
{
    /// <summary>
    /// Summary description for IonMobilityAcqMain.
    /// </summary>
    public partial class IonMobilityMain : System.Windows.Forms.Form
    {
        private string current_experiment_path = "";
        private readonly List<DataViewer> open_Experiments;

        IonMobility.Form_About form_about = new Form_About();

        /******************************************************************************
         *  IonMobilityAcqMain Constructor
         */
        public IonMobilityMain(Form_About formAbout, string[] args)
        {
            this.form_about = formAbout;

            this.open_Experiments = new List<DataViewer>(11);

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

        private void IonMobilityAcqMain_Load(object sender, System.EventArgs e)
        {
            this.Top = 20;

            // reduce the screen down to the single panel acquire.
            this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - this.Width - 20;

            form_about.Close();
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

        private void GraphExperiment(string path)
        {
            // limit the total number of experiments open.
            // TODO: remove this limitation, and/or the "only one instance of UIMF_Viewer can be open"?
            RemoveClosedForms();
            if (this.open_Experiments.Count > 4)
            {
                MessageBox.Show("You can have 5 experiments open at a time.  Please close an experiment before opening another.");
                return;
            }

            try
            {
                var frame_dataViewer = new UIMF_File.DataViewer(path, true);
                frame_dataViewer.num_TICThreshold.Value = 300;

                this.open_Experiments.Add(frame_dataViewer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RemoveClosedForms()
        {
            var toRemove = open_Experiments.Where(x => x.IsDisposed).ToList();

            foreach (var remove in toRemove)
            {
                open_Experiments.Remove(remove);
            }
        }

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


            if (Path.GetExtension(files[0]).ToUpper() == ".UIMF")
            {
                GraphExperiment(files[0]);
                return;
            }

            RemoveClosedForms();
            if (this.open_Experiments.Count > 4)
            {
                MessageBox.Show("You can have 5 experiments open at a time.  Please close an experiment before opening another.");
                return;
            }

            if (Path.GetExtension(files[0]).ToUpper() == ".RAW")
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
        }

        private void IonMobilityAcqMain_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void menuItem_About_Click(object sender, EventArgs e)
        {
            if (this.form_about.IsDisposed)
                this.form_about = new Form_About();

            if (this.form_about.Visible)
                this.form_about.Hide();
            else
                this.form_about.Show();
            this.form_about.Update();
        }
    }
}
