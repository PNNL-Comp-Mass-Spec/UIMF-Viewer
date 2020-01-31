using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;

namespace UIMFViewer.FrameControl
{
    public class FrameControlViewModel : ReactiveObject
    {
        private string uimfFile;
        private UIMFDataWrapper.ReadFrameType selectedFrameType;
        private int minimumFrameIndex;
        private int maximumFrameIndex;
        private int currentFrameIndex;
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

        public ObservableCollectionExtended<UIMFDataWrapper.ReadFrameType> FrameTypes { get; }

        public string UimfFile
        {
            get => uimfFile;
            set => this.RaiseAndSetIfChanged(ref uimfFile, value);
        }

        public UIMFDataWrapper.ReadFrameType SelectedFrameType
        {
            get => selectedFrameType;
            set => this.RaiseAndSetIfChanged(ref selectedFrameType, value);
        }

        public int MinimumFrameIndex
        {
            get => minimumFrameIndex;
            set => this.RaiseAndSetIfChanged(ref minimumFrameIndex, value);
        }

        public int MaximumFrameIndex
        {
            get => maximumFrameIndex;
            set => this.RaiseAndSetIfChanged(ref maximumFrameIndex, value);
        }

        public int CurrentFrameIndex
        {
            get => currentFrameIndex;
            set
            {
                if (value - MinimumFrameIndex + 1 < SummedFrames)
                {
                    value = MinimumFrameIndex + SummedFrames - 1;
                    if (value == currentFrameIndex)
                    {
                        this.RaisePropertyChanged();
                        return;
                    }
                }
                this.RaiseAndSetIfChanged(ref currentFrameIndex, value);
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

        public ReactiveCommand<Unit, Unit> PlayFramesForwardCommand { get; }
        public ReactiveCommand<Unit, Unit> PlayFramesBackwardCommand { get; }
        public ReactiveCommand<Unit, Unit> StopPlayingFramesCommand { get; }

        public FrameControlViewModel()
        {
            MinimumFrameIndex = 0;
            MaximumFrameIndex = 4;
            SummedFrames = 1;
            MinimumSummedFrame = 0;
            MaximumSummedFrame = 0;

            FrameTypes = new ObservableCollectionExtended<UIMFDataWrapper.ReadFrameType>(Enum.GetValues(typeof(UIMFDataWrapper.ReadFrameType)).Cast<UIMFDataWrapper.ReadFrameType>());
            SelectedFrameType = UIMFDataWrapper.ReadFrameType.AllFrames;

            isSumming = this.WhenAnyValue(x => x.SummedFrames).Select(x => x > 1).ToProperty(this, x => x.IsSumming, false);
            frameControlsVisible = this.WhenAnyValue(x => x.ShowChromatogramLabel).Select(x => !x).ToProperty(this, x => x.FrameControlsVisible, true);
            showFrameSelectControls = this.WhenAnyValue(x => x.MinimumFrameIndex, x => x.MaximumFrameIndex).Select(x => x.Item2 - x.Item1 > 1).ToProperty(this, x => x.ShowFrameSelectControls, true);
            maxSummedFrames = this.WhenAnyValue(x => x.MinimumFrameIndex, x => x.MaximumFrameIndex).Select(x => x.Item2 - x.Item1 + 1).ToProperty(this, x => x.MaxSummedFrames);

            PlayFramesForwardCommand = ReactiveCommand.Create(PlayFramesForward);
            PlayFramesBackwardCommand = ReactiveCommand.Create(PlayFramesBackward);
            StopPlayingFramesCommand = ReactiveCommand.Create(StopPlayingCinema);
        }

        public event EventHandler PlayLeft;
        public event EventHandler StopCinema;
        public event EventHandler PlayRight;

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
    }
}
