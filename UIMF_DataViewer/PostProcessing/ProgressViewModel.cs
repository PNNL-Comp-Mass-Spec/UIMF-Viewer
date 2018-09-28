using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using ReactiveUI;

namespace UIMF_DataViewer.PostProcessing
{
    public class ProgressViewModel : ReactiveObject
    {
        private double timeMSec = 0;
        private double progressValue;
        private string status;
        private bool canCancel = true;
        private bool canContinue;

        public double TimeMSec
        {
            get => timeMSec;
            set => this.RaiseAndSetIfChanged(ref timeMSec, value);
        }

        public double ProgressMaximum { get; }

        public double ProgressValue
        {
            get => progressValue;
            set => this.RaiseAndSetIfChanged(ref progressValue, value);
        }

        public string Status
        {
            get => status;
            set => this.RaiseAndSetIfChanged(ref status, value);
        }

        public bool CanCancel
        {
            get => canCancel;
            set => this.RaiseAndSetIfChanged(ref canCancel, value);
        }

        public bool CanContinue
        {
            get => canContinue;
            set => this.RaiseAndSetIfChanged(ref canContinue, value);
        }

        public bool HasErrors { get; private set; }

        public bool Success => !HasErrors;

        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> ContinueCommand { get; }

        private readonly CancellationTokenSource cancelTokenSource;

        /// <summary>
        /// For WPF Designer use only
        /// </summary>
        [Obsolete("For WPF Designer use only", true)]
        public ProgressViewModel() : this(100, default(CancellationTokenSource))
        {
        }

        public ProgressViewModel(int maximum, CancellationTokenSource canceller)
        {
            ProgressMaximum = maximum;
            cancelTokenSource = canceller;
            CancelCommand = ReactiveCommand.Create(Cancel, this.WhenAnyValue(x => x.CanCancel));
            ContinueCommand = ReactiveCommand.Create(Continue, this.WhenAnyValue(x => x.CanContinue));
        }

        public void Cancel()
        {
            cancelTokenSource.Cancel();
        }

        private void Continue()
        {
            canCancel = true;
        }

        public void SetProgress(int newValue, double milliseconds)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                TimeMSec = milliseconds;
                ProgressValue = Math.Max(newValue, ProgressMaximum);
            });
        }

        public void AddStatus(string message, bool isError)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var error = "";
                if (isError)
                {
                    error = "ERROR: ";
                    HasErrors = true;
                }

                // TODO: Show latest last, auto-scroll to bottom, highlight errors red?
                // TODO: Maybe a listview would be better for this purpose...
                Status = $"{error}{message}\n{Status}";
            });
        }
    }
}
