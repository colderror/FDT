using RoboSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace FDT
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow
    {
        public ObservableCollection<TransferStatus> TransferStatuses = new ObservableCollection<TransferStatus>();
        public ObservableCollection<string> ErrorLogCollection = new ObservableCollection<string>();
        ProgressBar progress;

        public ProgressWindow()
        {
            InitializeComponent();
            DataContext = this;
            TransferProgress.ItemsSource = TransferStatuses;
            ErrorLogBox.ItemsSource = ErrorLogCollection;
        }

        private void ProgressUI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ProgressUI.Hide();
        }

        public void PassProgress(string directory, int currentProgress, int transferID)
        {
            TransferStatuses[transferID] = new TransferStatus { Name = directory, Progress = currentProgress };
        }

        private void CancelTransfersButton_Click(object sender, RoutedEventArgs e)
        {
            Process[] proc = null;
            try
            {
                proc = Process.GetProcessesByName("robocopy");
            }
            catch (Exception exc)
            {

            }
            if (proc != null)
            {
                foreach (Process p in proc)
                {
                    p.Kill();
                }
            }
        }
    }
    public class TransferStatus
    {
        public string Name { get; set; }
        public double Progress { get; set; }
        public int FileCount { get; set; }
        public int FileTotal { get; set; }
        public string CurrentStatus { get; set; }        

        public void PauseTransfer(RoboCommand robocmd)
        {
            robocmd.Stop();
        }

        public bool TransferCancelled(RoboCommand robocmd)
        {
            if (robocmd.IsPaused)
            {
                return true;
            }
            else
                return false;
        }
    }
}
