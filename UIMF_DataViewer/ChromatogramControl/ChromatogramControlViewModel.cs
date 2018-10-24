using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace UIMF_DataViewer.ChromatogramControl
{
    public class ChromatogramControlViewModel : ReactiveObject
    {
        private bool chromatogramAllowed = true;
        private bool partialPeakChromatogramChecked;
        private bool completePeakChromatogramChecked;
        private bool noChromatogramChecked = true;
        private bool canCreatePartialChromatogram = true;
        private int frameCompression;
        private int maxFrameCompression = 200;

        public bool ChromatogramAllowed
        {
            get => chromatogramAllowed;
            set => this.RaiseAndSetIfChanged(ref chromatogramAllowed, value);
        }

        public bool PartialPeakChromatogramChecked
        {
            get => partialPeakChromatogramChecked;
            set => this.RaiseAndSetIfChanged(ref partialPeakChromatogramChecked, value);
        }

        public bool CompletePeakChromatogramChecked
        {
            get => completePeakChromatogramChecked;
            set => this.RaiseAndSetIfChanged(ref completePeakChromatogramChecked, value);
        }

        public bool NoChromatogramChecked
        {
            get => noChromatogramChecked;
            set => this.RaiseAndSetIfChanged(ref noChromatogramChecked, value);
        }

        public bool CanCreatePartialChromatogram
        {
            get => canCreatePartialChromatogram;
            set => this.RaiseAndSetIfChanged(ref canCreatePartialChromatogram, value);
        }

        public int FrameCompression
        {
            get => frameCompression;
            set => this.RaiseAndSetIfChanged(ref frameCompression, value);
        }

        public int MaxFrameCompression
        {
            get => maxFrameCompression;
            set => this.RaiseAndSetIfChanged(ref maxFrameCompression, value);
        }
    }
}
