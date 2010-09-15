using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;

namespace DataIO
{
    public partial class ctlMessageViewer : UserControl    
    {
        private class clsViewerFeeder : clsThreadedDataConsumer
        {
            ctlMessageViewer mViewer = null;

            public clsViewerFeeder(ctlMessageViewer viewer)
            {
                mViewer = viewer;
            }

            public override void ProcessData(object obj)
            {
                mViewer.MsgText = obj as string;
            }
        }

        public delegate void MessageArrivedDelegate(string msgStr);
        public event MessageArrivedDelegate MessageArrived = null;

        private clsViewerFeeder mInput = null;
        public clsThreadedDataConsumer Input
        {
            get { return mInput as clsThreadedDataConsumer; }
        }

        public override  string Text
        {
            get { return rtb.Text; }
            set { rtb.Text = value; }
        }

        /// <summary>
        /// number of messages since last cleared
        /// </summary>
        private int mMsgCount = 0;
        public int MsgCount
        {
            get { return mMsgCount; }
            set 
            { 
                mMsgCount = value;
                lblMessageCount.Text = mMsgCount.ToString();
            }
        }

        /// <summary>
        /// message text
        /// </summary>
        private Queue mMsgQ = new Queue();
      // private string mMsgText = "";
        public string MsgText
        {
            get { return rtb.Text; }
            set
            {
                mMsgQ.Enqueue(value);
                if (rtb.InvokeRequired)
                    //rtb.Invoke(new MethodInvoker(this.ShowMessage));
                    rtb.BeginInvoke(new MethodInvoker(this.ShowMessage));
                else
                    this.ShowMessage();
            }
        }

        private void ShowMessage()
        {
            
            while (mMsgQ.Count > 0)
            {
                string msgStr = mMsgQ.Dequeue() as string;
                if (msgStr != null)
                {
                    MsgCount++;
                    rtb.Text = msgStr;
                    if (MessageArrived != null)
                    {
                        MessageArrived(msgStr);
                    }
                }
            }
        }

        public ctlMessageViewer()
        {
            InitializeComponent();

            mInput = new clsViewerFeeder(this);

            Clear();
        }

        public void Clear()
        {
            rtb.Clear();
            MsgCount = 0;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
           Clear();
        }
    }
}
