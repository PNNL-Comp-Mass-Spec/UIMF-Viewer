using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using UIMF_File;

namespace UIMF_DataViewer.FrameControl
{
    public class FrameControlViewModel : ReactiveObject
    {
        private string selectedUimfFile;
        private UIMFDataWrapper.ReadFrameType selectedFrameType;
        private int minimumFrameNumber;
        private int maximumFrameNumber;
        private int currentFrameNumber;
        private int summedFrames;
        private int minimumSummedFrame;
        private int maximumSummedFrame;
        private readonly ObservableAsPropertyHelper<bool> isSumming;
        private bool showChromatogramLabel = false;
        private readonly ObservableAsPropertyHelper<bool> frameControlsVisible;
        private readonly ObservableAsPropertyHelper<bool> showFrameSelectControls;
        private bool playingFramesBackward;
        private bool playingFramesForward;
        private readonly ObservableAsPropertyHelper<int> maxSummedFrames;
        private int ticThreshold;
        private bool showTicItems;
        private long ticValue;
        private bool showTicValue;

        public ReactiveList<string> UimfFiles { get; } = new ReactiveList<string>();
        public ReactiveList<UIMFDataWrapper.ReadFrameType> FrameTypes { get; }

        public string SelectedUimfFile
        {
            get => selectedUimfFile;
            set => this.RaiseAndSetIfChanged(ref selectedUimfFile, value);
        }

        public UIMFDataWrapper.ReadFrameType SelectedFrameType
        {
            get => selectedFrameType;
            set => this.RaiseAndSetIfChanged(ref selectedFrameType, value);
        }

        public int MinimumFrameNumber
        {
            get => minimumFrameNumber;
            set => this.RaiseAndSetIfChanged(ref minimumFrameNumber, value);
        }

        public int MaximumFrameNumber
        {
            get => maximumFrameNumber;
            set => this.RaiseAndSetIfChanged(ref maximumFrameNumber, value);
        }

        public int CurrentFrameNumber
        {
            get => currentFrameNumber;
            set
            {
                if (value - MinimumFrameNumber + 1 < SummedFrames)
                {
                    value = MinimumFrameNumber + SummedFrames - 1;
                    if (value == currentFrameNumber)
                    {
                        this.RaisePropertyChanged();
                        return;
                    }
                }
                this.RaiseAndSetIfChanged(ref currentFrameNumber, value);
            }
        }

        public int SummedFrames
        {
            get => summedFrames;
            set => this.RaiseAndSetIfChanged(ref summedFrames, value);
        }

        public bool IsSumming => isSumming.Value;

        public int MinimumSummedFrame
        {
            get => minimumSummedFrame;
            set => this.RaiseAndSetIfChanged(ref minimumSummedFrame, value);
        }

        public int MaximumSummedFrame
        {
            get => maximumSummedFrame;
            set => this.RaiseAndSetIfChanged(ref maximumSummedFrame, value);
        }

        public bool ShowChromatogramLabel
        {
            get => showChromatogramLabel;
            set => this.RaiseAndSetIfChanged(ref showChromatogramLabel, value);
        }

        public bool FrameControlsVisible => frameControlsVisible.Value;
        public bool ShowFrameSelectControls => showFrameSelectControls.Value;

        public bool PlayingFramesBackward
        {
            get => playingFramesBackward;
            set => this.RaiseAndSetIfChanged(ref playingFramesBackward, value);
        }

        public bool PlayingFramesForward
        {
            get => playingFramesForward;
            set => this.RaiseAndSetIfChanged(ref playingFramesForward, value);
        }

        public int MaxSummedFrames => maxSummedFrames.Value;

        public bool ShowTICItems
        {
            get => showTicItems;
            set => this.RaiseAndSetIfChanged(ref showTicItems, value);
        }

        public int TICThreshold
        {
            get => ticThreshold;
            set => this.RaiseAndSetIfChanged(ref ticThreshold, value);
        }

        public long TICValue
        {
            get => ticValue;
            set => this.RaiseAndSetIfChanged(ref ticValue, value);
        }

        public bool ShowTICValue
        {
            get => showTicValue;
            set => this.RaiseAndSetIfChanged(ref showTicValue, value);
        }

        public ReactiveCommand<Unit, Unit> PlayFramesForwardCommand { get; }
        public ReactiveCommand<Unit, Unit> PlayFramesBackwardCommand { get; }
        public ReactiveCommand<Unit, Unit> StopPlayingFramesCommand { get; }
        public ReactiveCommand<Unit, Unit> CalculateTICCommand { get; }

        public FrameControlViewModel()
        {
            MinimumFrameNumber = 0;
            MaximumFrameNumber = 5;
            SummedFrames = 1;

            FrameTypes = new ReactiveList<UIMFDataWrapper.ReadFrameType>(Enum.GetValues(typeof(UIMFDataWrapper.ReadFrameType)).Cast<UIMFDataWrapper.ReadFrameType>());
            SelectedFrameType = UIMFDataWrapper.ReadFrameType.AllFrames;

            isSumming = this.WhenAnyValue(x => x.SummedFrames).Select(x => x > 1).ToProperty(this, x => x.IsSumming, false);
            frameControlsVisible = this.WhenAnyValue(x => x.ShowChromatogramLabel).Select(x => !x).ToProperty(this, x => x.FrameControlsVisible, true);
            showFrameSelectControls = this.WhenAnyValue(x => x.MinimumFrameNumber, x => x.MaximumFrameNumber).Select(x => x.Item2 - x.Item1 > 1).ToProperty(this, x => x.ShowFrameSelectControls, true);
            maxSummedFrames = this.WhenAnyValue(x => x.MinimumFrameNumber, x => x.MaximumFrameNumber).Select(x => x.Item2 - x.Item1 + 1).ToProperty(this, x => x.MaxSummedFrames);
            this.WhenAnyValue(x => x.SummedFrames).Subscribe(x => ShowTICValue = false);

            PlayFramesForwardCommand = ReactiveCommand.Create(PlayFramesForward);
            PlayFramesBackwardCommand = ReactiveCommand.Create(PlayFramesBackward);
            StopPlayingFramesCommand = ReactiveCommand.Create(StopPlayingCinema);
            CalculateTICCommand = ReactiveCommand.Create(CalcTIC);
        }

        public event EventHandler PlayLeft;
        public event EventHandler StopCinema;
        public event EventHandler PlayRight;
        public event EventHandler CalculateTIC;

        public void PlayFramesForward()
        {
            if (PlayingFramesForward)
            {
                StopPlayingCinema();
                return;
            }

            PlayingFramesBackward = false;
            PlayingFramesForward = true;
            PlayRight?.Invoke(this, EventArgs.Empty);
        }

        public void PlayFramesBackward()
        {
            if (PlayingFramesBackward)
            {
                StopPlayingCinema();
                return;
            }

            PlayingFramesBackward = true;
            PlayingFramesForward = false;
            PlayLeft?.Invoke(this, EventArgs.Empty);
        }

        public void StopPlayingCinema()
        {
            PlayingFramesBackward = false;
            PlayingFramesForward = false;
            StopCinema?.Invoke(this, EventArgs.Empty);
        }

        public void CalcTIC()
        {
            CalculateTIC?.Invoke(this, EventArgs.Empty);
        }
    }
}
