using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using UIMF_File;

namespace UIMFViewer
{
    public class MainWindowViewModel : ReactiveObject
    {
        private bool useSingleProcess;
        private readonly List<DataViewer> openExperiments;

        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

        public bool UseSingleProcess
        {
            get => useSingleProcess;
            set => this.RaiseAndSetIfChanged(ref useSingleProcess, value);
        }

        public MainWindowViewModel()
        {
            openExperiments = new List<DataViewer>(11);

            OpenFileCommand = ReactiveCommand.Create(FindAndOpenFile);
        }

        public void FindAndOpenFile()
        {
            var fileDialog = new CommonOpenFileDialog();
            fileDialog.EnsureFileExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filters.Add(new CommonFileDialogFilter("UIMF file", "*.uimf"));

            var result = fileDialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                OpenUimfFile(fileDialog.FileName);
            }
        }

        private void OpenUimfFile(string path)
        {
            try
            {
                if (UseSingleProcess)
                {
                    // limit the total number of experiments open.
                    RemoveClosedForms();
                    if (this.openExperiments.Count > 9)
                    {
                        MessageBox.Show("You can have 10 experiments open at a time in single process mode. Please close an experiment before opening another.");
                        return;
                    }

                    // Old method, enabled for debug: Limit to 5 files, each file is a direct child window of IonMobilityMain
                    var dataViewer = new DataViewer(path, true);
                    dataViewer.num_TICThreshold.Value = 300;

                    this.openExperiments.Add(dataViewer);
                }
                else
                {
                    // New method: IonMobilityMain facilitates opening new UIMF files with its 'always on top' drag-n-drop window, but each file is its own process.
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var psi = new System.Diagnostics.ProcessStartInfo(exePath);
                    psi.Arguments = $"\"{path}\"";
                    System.Diagnostics.Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RemoveClosedForms()
        {
            var toRemove = openExperiments.Where(x => x.IsDisposed).ToList();

            foreach (var remove in toRemove)
            {
                openExperiments.Remove(remove);
            }
        }

        public void ItemDropped(string path)
        {
            var uimf = GetUimfFileInPath(path);

            if (string.IsNullOrWhiteSpace(uimf))
            {
                MessageBox.Show($"\"{path}\" is not, or does not contain, a UIMF file.");
                return;
            }

            OpenUimfFile(uimf);
        }

        public void CloseChildren()
        {
            foreach (var dataViewer in openExperiments)
            {
                if (!dataViewer.IsDisposed)
                {
                    dataViewer.Invoke(new System.Windows.Forms.MethodInvoker(() => dataViewer.Close()));
                }
            }
        }

        public static string GetUimfFileInPath(string path)
        {
            //detect whether its a directory or file
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                var files = Directory.GetFiles(path, "*.UIMF");
                if (files.Length == 0)
                    return null;
                path = files[0];
            }

            if (Path.GetExtension(path).ToUpper() == ".UIMF")
            {
                return path;
            }

            return null;
        }
    }
}
