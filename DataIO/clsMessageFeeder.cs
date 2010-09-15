using System;
using System.Collections.Generic;
using DataIO;

namespace DataIO
{
    public class clsDataFeeder: clsThreadedDataConsumer 
    {
        private List<clsDataConsumer> mOutput = new List<clsDataConsumer>();
        public List<clsDataConsumer> OutputList
        {
            get { return mOutput; }
            set { mOutput = value; }
        }

        public clsDataFeeder()
        {
        }

        public clsDataFeeder(bool startSpinning)
            : base(startSpinning)
        {

        }

        public override void ProcessData(object obj)
        {
            FeedOutputList(obj);
        }

        public  void FeedOutputList(object obj)
        {
            //build message string by concatenation
            for (int i = 0; i < mOutput.Count; i++)
            {
                if (mOutput[i] != null)
                {
                  mOutput[i].FeedInput(obj);
                }
            }
        }

    }
}
