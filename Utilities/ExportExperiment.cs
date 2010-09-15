using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IonMobility.Utilities
{
    public partial class ExportExperiment : Form
    {
        public ExportExperiment()
        {
            InitializeComponent();
        }

        private void btn_ExportExperimentOK_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}