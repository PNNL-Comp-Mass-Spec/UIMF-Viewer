using System;
using System.Reactive;
using ReactiveUI;

namespace UIMF_DataViewer.FrameInfo
{
    public class FrameInfoViewModel : ReactiveObject
    {
        private bool cursorTabSelected = true;
        private bool calibrationTabSelected = true;
        private double cursorMobilityScanNumber;
        private double cursorMobilityScanTime;
        private double cursorTofValue;
        private double cursorMz;
        private int timeOffsetNs;
        private DateTime calibrationDate = DateTime.Today;
        private string calibratorType = "mz = (k*(t-t0))^2";
        private double calibrationK;
        private double calibrationT0;
        private bool canRevertCalDefaults;
        private bool canSetCalDefaults;
        private bool changeCalibrationKFailed;
        private bool changeCalibrationT0Failed;

        public bool CursorTabSelected
        {
            get => cursorTabSelected;
            set => this.RaiseAndSetIfChanged(ref cursorTabSelected, value);
        }

        public bool CalibrationTabSelected
        {
            get => calibrationTabSelected;
            set => this.RaiseAndSetIfChanged(ref calibrationTabSelected, value);
        }

        public double CursorMobilityScanNumber
        {
            get => cursorMobilityScanNumber;
            set => this.RaiseAndSetIfChanged(ref cursorMobilityScanNumber, value);
        }

        public double CursorMobilityScanTime
        {
            get => cursorMobilityScanTime;
            set => this.RaiseAndSetIfChanged(ref cursorMobilityScanTime, value);
        }

        public double CursorTOFValue
        {
            get => cursorTofValue;
            set => this.RaiseAndSetIfChanged(ref cursorTofValue, value);
        }

        public double CursorMz
        {
            get => cursorMz;
            set => this.RaiseAndSetIfChanged(ref cursorMz, value);
        }

        public int TimeOffsetNs
        {
            get => timeOffsetNs;
            set => this.RaiseAndSetIfChanged(ref timeOffsetNs, value);
        }

        public DateTime CalibrationDate
        {
            get => calibrationDate;
            set => this.RaiseAndSetIfChanged(ref calibrationDate, value);
        }

        public string CalibratorType
        {
            get => calibratorType;
            set => this.RaiseAndSetIfChanged(ref calibratorType, value);
        }

        public double CalibrationK
        {
            get => calibrationK;
            set => this.RaiseAndSetIfChanged(ref calibrationK, value);
        }

        public double CalibrationT0
        {
            get => calibrationT0;
            set => this.RaiseAndSetIfChanged(ref calibrationT0, value);
        }

        public bool CanRevertCalDefaults
        {
            get => canRevertCalDefaults;
            set => this.RaiseAndSetIfChanged(ref canRevertCalDefaults, value);
        }

        public bool CanSetCalDefaults
        {
            get => canSetCalDefaults;
            set => this.RaiseAndSetIfChanged(ref canSetCalDefaults, value);
        }

        public bool ChangeCalibrationKFailed
        {
            get => changeCalibrationKFailed;
            set => this.RaiseAndSetIfChanged(ref changeCalibrationKFailed, value);
        }

        public bool ChangeCalibrationT0Failed
        {
            get => changeCalibrationT0Failed;
            set => this.RaiseAndSetIfChanged(ref changeCalibrationT0Failed, value);
        }

        public ReactiveCommand<Unit, Unit> SetCalDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> RevertCalDefaultsCommand { get; }

        public FrameInfoViewModel()
        {
            SetCalDefaultsCommand = ReactiveCommand.Create(FireSetCalDefaults);
            RevertCalDefaultsCommand = ReactiveCommand.Create(FireRevertCalDefaults);
        }

        public event EventHandler SetCalDefaults;
        public event EventHandler RevertCalDefaults;

        public void HideCalibrationButtons()
        {
            CanSetCalDefaults = false;
            CanRevertCalDefaults = false;
        }

        public void ShowCalibrationButtons()
        {
            CanSetCalDefaults = true;
            CanRevertCalDefaults = true;
        }

        private void FireSetCalDefaults()
        {
            SetCalDefaults?.Invoke(this, EventArgs.Empty);
        }

        private void FireRevertCalDefaults()
        {
            RevertCalDefaults?.Invoke(this, EventArgs.Empty);
        }
    }
}
