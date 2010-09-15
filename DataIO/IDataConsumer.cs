using System;
using System.Collections.Generic;
using System.Text;

namespace DataIO
{
    interface IDataConsumer
    {
        void FeedInput(object obj);
    }
}
