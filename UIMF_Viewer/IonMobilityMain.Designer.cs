using System.Linq;
using System.Windows.Forms;

namespace IonMobility
{
    partial class IonMobilityMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region MICROSOFT FORM DATA
        private MainMenu mainMenu_Desktop;
        private MenuItem menubar_File;
        private MenuItem menubar_Graph;
        private FolderBrowserDialog folderBrowserDialogExperiment;
        private MenuItem menuGraph_SelectExperiment;
        private MenuItem menuProcess_SelectExperiment;
        private MenuItem menuProcess_Batch;
        private PictureBox pb_PNNLLogo;
        private MenuItem menuItem_About;
        private MenuItem menuitem_OpenFile;
        #endregion

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var exp in this.open_Experiments.Where(x => !x.IsDisposed))
                {
                    exp.Close();
                }

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
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
    }
}