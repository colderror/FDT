using Microsoft.Win32;
using RoboSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FDT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        //Variables
        AboutWindow GetAboutWindow = new AboutWindow();
        CatFacts catFacts = new CatFacts();
        ProgressWindow GetProgressWindow = new ProgressWindow();
        List<CheckBox> ComputerCheckBoxList { get; set; } = new List<CheckBox>();
        List<CheckBox> ExternalCheckBoxList { get; set; } = new List<CheckBox>();
        List<string> FolderNamesList { get; set; } = new List<string>();
        List<string> CatFacts;
        Dictionary<string, string> FolderNamePath = new Dictionary<string, string>();
        IProgress<int> fileCountCurrent = new Progress<int>();
        IProgress<TransferStatus> TransferStatusProgress;
        IProgress<string> ErrorLogReport;
        IProgress<string> FactReport;
        DateTime expirationDate = DateTime.Parse("06/01/2029");
        RoboCommand roboCommand;
        Random random = new Random();
        System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
        Timer timer;
        string assetModel, assetSerial, assetTag, currentDirectory = Directory.GetCurrentDirectory(), selectedUser = "", externalDriveLetterPath, manualSourcePath, manualDestinationPath;
        int rand;
        public MainWindow()
        {
            InitializeComponent();
        }
        //Main UI Loaded Event
        private void MainUI_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!ValidateProgram())
            //{
            //    ExpiredDisable();
            //    MessageBox.Show("There was a problem encountered when starting the application. Ensure you are connected to the Network otherwise contact the developer.");
            //    Application.Current.Shutdown();
            //}
            foreach (KeyValuePair<string, string> keyValuePair in UserDirectories.InitializeDictionary(selectedUser))
            {
                FolderNamesList.Add(keyValuePair.Key);
            }
            selectedUser = Environment.UserName;
            UserDirectories.InitializeDictionary(selectedUser);
            assetModel = Registry.GetValue("HKEY_LOCAL_MACHINE\\HARDWARE\\DESCRIPTION\\System\\BIOS", "SystemProductName", null).ToString();
            assetTag = Environment.MachineName;
            assetSerial = GetSerialNumber();
            AssetModel.Text = assetModel;
            AssetSerial.Text = assetSerial;
            AssetTag.Text = assetTag;
            GetUsers();
            GetDrives();
            UsersNames.Text = selectedUser;
            BuildCheckBoxes();
            externalDriveLetterPath = GetExternalDrivePath();
            TransferStatusProgress = new Progress<TransferStatus>(n => GetProgressWindow.TransferStatuses.Add(n));
            ErrorLogReport = new Progress<string>(n => GetProgressWindow.ErrorLogCollection.Add(n));
            FactReport = new Progress<string>(n => GetProgressWindow.DidYouKnow.Text = n);
            CatFacts = catFacts.PopulateFactsList();
        }
        //Get External Drive Path
        public string GetExternalDrivePath()
        {
            ComboBoxItem ComboItem = (ComboBoxItem)DrivesList.SelectedItem;
            string extLetter = ComboItem.Content.ToString();
            int index = extLetter.IndexOf(@"\") + 1;
            extLetter = extLetter.Substring(0, index);
            return extLetter;
        }
        //Get Serial Number
        public string GetSerialNumber()
        {
            string serial = "";
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            foreach (ManagementObject obj in objectCollection)
            {
                foreach (PropertyData data in obj.Properties)
                    serial = String.Format("{0}", data.Value);
            }
            return serial;
        }
        //Main UI Window Closing Event
        private void MainUI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            if (timer != null)
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
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
            try
            {
                if (proc[0].HasExited)
                {
                    Process[] newproc;
                    try
                    {
                        newproc = Process.GetProcessesByName("robocopy");
                        foreach (Process p in newproc)
                        {
                            p.Kill();
                        }
                    }
                    catch (Exception exc)
                    {

                    }
                }
            }
            catch (Exception)
            {

            }

        }
        //New Radio Button Checked Event
        private void NewRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int zeroCount = 0;
            int totalCount = ExternalCheckBoxList.Count;
            foreach (CheckBox checkBox in ComputerCheckBoxList)
            {
                zeroCount++;
                checkBox.IsChecked = true;
                if (zeroCount == totalCount)
                {
                    checkBox.IsChecked = false;
                }
            }
            foreach (CheckBox checkBox in ExternalCheckBoxList)
            {
                checkBox.IsChecked = false;
            }
        }
        //Old Radio Button Checked Event
        private void OldRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int zeroCount = 0;
            int totalCount = ExternalCheckBoxList.Count;
            foreach (CheckBox checkBox in ExternalCheckBoxList)
            {
                zeroCount++;
                checkBox.IsChecked = true;
                if(zeroCount == totalCount)
                {
                    checkBox.IsChecked = false;
                }
            }
            foreach (CheckBox checkBox in ComputerCheckBoxList)
            {
                checkBox.IsChecked = false;
            }
        }
        //User Name ComboBox Selection Changed Event
        private void UsersNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainUI.IsLoaded)
            {
                ComboBoxItem ComboItem = (ComboBoxItem)UsersNames.SelectedItem;
                selectedUser = ComboItem.Content.ToString();
                UserDirectories.InitializeDictionary(selectedUser);
            }

        }
        //Drives List Selection Changed Event
        private void DrivesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrivesList.IsLoaded)
            {
                externalDriveLetterPath = GetExternalDrivePath();
            }
        }
        //Save Button Click Event
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This option is currently not available in this version.\nThe developer has been quite busy.", "Wait until next version", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        //Run As Admin Button Click Event
        private void RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This option is currently not available in this version.\nThe developer has been quite busy.", "Wait until next version", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        //Border Click Event
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        //About Button Clicked Event
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            GetAboutWindow.AboutUI.Owner = this;
            GetAboutWindow.AboutUI.Show();
        }
        //View Transfers Click
        private void ViewTransfers_Click(object sender, RoutedEventArgs e)
        {
            GetProgressWindow.Show();
        }


        //Start Button Click Event
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!StartButton.IsEnabled)
            {
                MessageBox.Show("There is a transfer already in progress", "FDT", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            }
            List<string> CheckedFolderNamesList = new List<string>();
            if (externalDriveLetterPath == LocalDrive.Text)
            {
                MessageBox.Show("External and Local Drive cannot be the same. \nPlease select another Drive from menu.", "Check External", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                return;
            }
            if (String.IsNullOrEmpty(selectedUser))
            {
                MessageBox.Show("No User was Selected.");
                return;
            }
            if (!OldRadioButton.IsChecked.Value && !NewRadioButton.IsChecked.Value)
            {
                MessageBox.Show("Specify what kind of transfer you are doing.", "FDT", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                return;
            }
            if (OldRadioButton.IsChecked.HasValue || NewRadioButton.IsChecked.HasValue)
            {
                //Old Transfer
                if (OldRadioButton.IsChecked.Value)
                {
                    MessageBoxResult result = MessageBox.Show("Are you sure you want to transfer from\nLocal Drive: " + LocalDrive.Text + " To External Drive: " + DrivesList.Text.Substring(0, 3) + "?\n\n(Warning: Make sure Network Drives are Online.)", "Confirm Transfer", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                    BackgroundWorker OldTransferWorker = new BackgroundWorker();
                    OldTransferWorker.DoWork += OldBackgroundWorker_DoWork;
                    OldTransferWorker.RunWorkerCompleted += OldBackgroundWorker_RunWorkerCompleted;
                    OldTransferWorker.WorkerReportsProgress = true;
                    //Create Directories If They dont Exist
                    if (!Directory.Exists(externalDriveLetterPath + @"FDT\" + selectedUser + @"\SAP"))
                    {
                        CreateDirectoriesOnExternal();
                    }
                    GetProgressWindow.TransferStatuses.Clear();
                    CheckedFolderNamesList.Clear();
                    foreach (CheckBox checkedBox in ExternalCheckBoxList)
                    {
                        if (checkedBox.IsChecked.Value)
                        {
                            CheckedFolderNamesList.Add(checkedBox.Content.ToString());
                        }
                    }
                    StartButton.IsEnabled = false;
                    ManualTransferButton.IsEnabled = true;
                    AboutButton.IsEnabled = true;
                    GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
                    GetProgressWindow.Show();
                    OldTransferWorker.RunWorkerAsync(CheckedFolderNamesList);
                }
                //New Transfer
                if (NewRadioButton.IsChecked.Value)
                {
                    MessageBoxResult result = MessageBox.Show("Are you sure you want to transfer from\nExternal Drive: " + DrivesList.Text.Substring(0, 3) + " To Local Drive: " + LocalDrive.Text + "?\n\n(Warning: Make sure Network Drives are Online.)", "Confirm Transfer", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        MessageBox.Show("Cancelled");
                        return;
                    }
                    BackgroundWorker NewTransferWorker = new BackgroundWorker();
                    NewTransferWorker.DoWork += NewBackgroundWorker_DoWork;
                    NewTransferWorker.RunWorkerCompleted += NewBackgroundWorker_RunWorkerCompleted;
                    NewTransferWorker.WorkerReportsProgress = true;
                    if (!Directory.Exists(externalDriveLetterPath + @"FDT\" + selectedUser))
                    {
                        MessageBox.Show("No user information was found on the selected External for \n" + selectedUser +
                            "\nPlease verify that you are\nperforming the right operation", "No Information Found", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                        return;
                    }
                    GetProgressWindow.TransferStatuses.Clear();
                    CheckedFolderNamesList.Clear();
                    foreach (CheckBox checkedBox in ComputerCheckBoxList)
                    {
                        if (checkedBox.IsChecked.Value)
                        {
                            CheckedFolderNamesList.Add(checkedBox.Content.ToString());
                        }
                    }
                    StartButton.IsEnabled = false;
                    ManualTransferButton.IsEnabled = false;
                    AboutButton.IsEnabled = true;
                    GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
                    GetProgressWindow.Show();
                    NewTransferWorker.RunWorkerAsync(CheckedFolderNamesList);
                }
            }
        }

        //Source Button Click Event
        private void SourceFileBrowser_Click(object sender, RoutedEventArgs e)
        {
            folderBrowser.Description = "Select Folder as a Source";
            folderBrowser.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ManualSource.Text = folderBrowser.SelectedPath;
                manualSourcePath = folderBrowser.SelectedPath;
            }
        }

        //Destination Button Click Event
        private void DestinationFileBrowser_Click(object sender, RoutedEventArgs e)
        {
            folderBrowser.Description = "Select Folder as a Destination";
            folderBrowser.ShowNewFolderButton = true;
            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ManualDestination.Text = folderBrowser.SelectedPath;
                manualDestinationPath = folderBrowser.SelectedPath;
            }
        }

        //Manual Transfer Button Click Event
        private void ManualTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ManualSource.Text) || String.IsNullOrEmpty(ManualDestination.Text))
            {
                MessageBox.Show("Please fill the Source and Destination Boxes.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (ManualSource.Text == ManualDestination.Text)
            {
                MessageBox.Show("Source and Destination cannot be the same.", "Source Matches Destination", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBoxResult result = MessageBox.Show("Are you sure you want to transfer\nFrom: " + ManualSource.Text + "\nTo: " + ManualDestination.Text + "?\n\n", "Confirm Transfer", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            StartButton.IsEnabled = false;
            ManualTransferButton.IsEnabled = false;
            GetProgressWindow.TransferStatuses.Clear();
            GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
            GetProgressWindow.Show();
            BackgroundWorker ManualTransfer = new BackgroundWorker();
            ManualTransfer.DoWork += ManualTransferBackgroundWorker_DoWork;
            ManualTransfer.RunWorkerCompleted += ManualTransferBackgroundWorker_RunWorkerCompleted;
            ManualTransfer.RunWorkerAsync();
        }        

        //Build CheckBoxes and Add to Panels
        public void BuildCheckBoxes()
        {
            foreach (string name in FolderNamesList)
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = name,
                };
                checkBox.Checked += ComputerCheckBox_Checked;
                ComputerCheckBoxList.Add(checkBox);
                ComputerPanel.Children.Add(checkBox);
            }
            foreach (string name in FolderNamesList)
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = name
                };
                checkBox.Checked += ExternalCheckBox_Checked;
                ExternalCheckBoxList.Add(checkBox);
                ExternalPanel.Children.Add(checkBox);
            }
        }
        //External Panel Checkbox Checked Event
        private void ExternalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OldRadioButton.IsChecked = true;
        }
        //Computer Panel Checkbox Checked Event
        private void ComputerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            NewRadioButton.IsChecked = true;
        }
        //Get Users
        public void GetUsers()
        {
            foreach (var d in System.IO.Directory.GetDirectories(@"C:\Users"))
            {
                var Dir = new DirectoryInfo(d);
                var UserDirectoryName = Dir.Name;
                ComboBoxItem BoxItem = new ComboBoxItem
                {
                    Content = UserDirectoryName
                };
                UsersNames.Items.Add(BoxItem);
            }
        }
        //Get Drives
        public void GetDrives()
        {
            string BoxItemContent = "";
            DriveInfo[] AllDrives = DriveInfo.GetDrives();
            DrivesList.Items.Clear();
            foreach (DriveInfo d in AllDrives)
            {
                if (d.IsReady == true)
                {
                    BoxItemContent = String.Format("{0} Label: {1}  Free Space: {2} GB", d.Name, d.VolumeLabel, d.AvailableFreeSpace / 1000 / 1000 / 1000);
                    ComboBoxItem BoxItem = new ComboBoxItem
                    {
                        Content = BoxItemContent
                    };
                    DrivesList.Items.Add(BoxItem);
                    DrivesList.SelectedIndex = 1;
                }
            }
        }
        //Get Printers
        public void GetPrinters()
        {
            string printerPath = externalDriveLetterPath + @"FDT\" + selectedUser + "\\Printers\\Printers.txt";
            if (!System.IO.File.Exists(printerPath))
            {
                PrinterSettings settings = new PrinterSettings();
                using (var tw = new StreamWriter(printerPath, true))
                {
                    ManagementObjectSearcher searcherP = new ManagementObjectSearcher("ROOT\\cimv2", "SELECT * FROM Win32_Printer WHERE Default='True'");
                    foreach (ManagementObject queryObj in searcherP.Get())
                    {
                        tw.WriteLine("Default Printer: " + queryObj["Name"].ToString());
                        tw.WriteLine("");
                    }
                }
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    using (var tw = new StreamWriter(printerPath, true))
                    {
                        tw.WriteLine(printer);
                    }
                }
            }
        }
        //Get Network
        public void GetNetwork()
        {
            ManagementObjectSearcher searcherN = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_LogicalDisk WHERE DriveType = 4");
            foreach (ManagementObject queryObj in searcherN.Get())
            {
                string folderDest = "Network";
                string dest = externalDriveLetterPath + @"FDT\" + selectedUser + @"\" + folderDest;
                string targetPath = queryObj["ProviderName"].ToString();
                string networkDriveLetter = queryObj["Name"].ToString();
                string networkLetter = networkDriveLetter.Replace(":", "");
                //Map Drive
                string networkPath = externalDriveLetterPath + @"FDT\" + selectedUser + @"\Network\Network.txt";
                using (var nw = new StreamWriter(networkPath, true))
                {
                    nw.WriteLine("net use " + networkDriveLetter + " " + "\"" + targetPath + "\"");
                }
            }
        }
        //Set Network
        public void SetNetwork()
        {
            string dest = externalDriveLetterPath + @"FDT\" + selectedUser + @"\Network";
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(dest + @"\Network.txt");
            while ((line = file.ReadLine()) != null)
            {
                Process process = new Process();
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C " + line;
                process.Start();
            }
            file.Close();
        }

        //Get Asset Info
        public void GetAssetInfo()
        {
            string path = externalDriveLetterPath + @"FDT\" + selectedUser + @"\AssetInfo.txt";
            if (!System.IO.File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = System.IO.File.CreateText(path))
                {
                    sw.WriteLine(selectedUser + "@shell.com");
                    sw.WriteLine(assetModel);
                    sw.WriteLine(assetSerial);
                    sw.WriteLine(assetTag);
                }
                return;
            }
            if (System.IO.File.Exists(path))
            {
                using (var tw = new StreamWriter(path, true))
                {
                    tw.WriteLine(" ");
                    tw.WriteLine(assetModel);
                    tw.WriteLine(assetSerial);
                    tw.WriteLine(assetTag);
                }
                return;
            }
        }

        public void ChromeBookmarkTransfer(string source, string destination)
        {
            File.Copy(source, destination, true);
        }

        //Create Directories
        public void CreateDirectoriesOnExternal()
        {
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(externalDriveLetterPath);
            var newDirectory = Directory.CreateDirectory(externalDriveLetterPath + @"FDT\" + selectedUser);
            foreach (string subdir in FolderNamesList)
            {
                newDirectory.CreateSubdirectory(subdir);
            }
            newDirectory.CreateSubdirectory("Network");
            newDirectory.CreateSubdirectory("Printers");
            newDirectory.CreateSubdirectory("SAP");
        }

        //Get Total File Count
        public int GetTotalFileCount(DirectoryInfo folderPath)
        {
            int count = 0;
            foreach (FileInfo info in folderPath.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (info.Extension != ".ost" || info.Extension != ".nst")
                {
                    count++;
                }
            }
            return count;
        }

        //Transfer to New Computer Background Worker Complete
        private void NewBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
            GetProgressWindow.Status.Text = "Performing Transfer...";
            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Visible;
        }

        //Transfer to New Computer Background Worker Do Work
        private void NewBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            rand = random.Next(0, 40);
            FactReport.Report(CatFacts[rand]);
            timer = new System.Timers.Timer(60000);
            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += delegate (object obj, ElapsedEventArgs args)
            {
                rand = random.Next(0, 40);
                FactReport.Report(CatFacts[rand]);
            };
            timer.AutoReset = true;
            timer.Enabled = true;
            ErrorLogReport.Report("Transfer started at " + DateTime.Now.ToString("h:mm:ss tt"));
            CloseApplications();
            GetAssetInfo();
            SetNetwork();
            DirectoryInfo sourcePath;
            DirectoryInfo destinationPath;
            int successCount = 0;
            List<string> FolderNames = e.Argument as List<string>;
            foreach (string folderName in FolderNames)
            {
                int sourceFileCount = 0;
                sourcePath = new DirectoryInfo(externalDriveLetterPath + @"FDT\" + selectedUser + @"\" + folderName);
                destinationPath = new DirectoryInfo(UserDirectories.InitializeDictionary(selectedUser)[folderName]);
                if (folderName == "Chrome Bookmarks")
                {
                    TransferStatus chromeTransferStatus = new TransferStatus
                    {
                        Name = folderName,
                        Progress = 0,
                        FileCount = 0,
                        FileTotal = sourceFileCount,
                        CurrentStatus = "Transferring..."
                    };
                    TransferStatusProgress.Report(chromeTransferStatus);
                    try
                    {
                        ChromeBookmarkTransfer(externalDriveLetterPath + @"FDT\" + selectedUser + @"\Chrome Bookmarks\Bookmarks", @"C:\Users\" + selectedUser + @"\AppData\Local\Google\Chrome\User Data\Default\Bookmarks");
                        chromeTransferStatus.Progress = 100;
                        chromeTransferStatus.CurrentStatus = "Done";
                    }
                    catch
                    {
                        chromeTransferStatus.Progress = 100;
                        chromeTransferStatus.CurrentStatus = "Error Transferring File";
                    }
                    successCount++;
                    if (successCount == FolderNames.Count)
                    {
                        ErrorLogReport.Report("Transfer finished at " + DateTime.Now.ToString("h:mm:ss tt"));
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            GetProgressWindow.TransferProgress.Items.Refresh();
                            GetProgressWindow.LoadingPanel.Visibility = Visibility.Hidden;
                            StartButton.IsEnabled = true;
                            ManualTransferButton.IsEnabled = true;
                            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Collapsed;
                        }));
                        timer.Stop();
                    }
                    continue;
                }
                if (Directory.Exists(sourcePath.FullName))
                {
                    sourceFileCount = GetTotalFileCount(sourcePath);
                }
                TransferStatus transferStatus = new TransferStatus
                {
                    Name = folderName,
                    Progress = 0,
                    FileCount = 0,
                    FileTotal = sourceFileCount,
                    CurrentStatus = "Transferring..."
                };
                TransferStatusProgress.Report(transferStatus);
                roboCommand = new RoboCommand();
                roboCommand.CopyOptions.Source = sourcePath.FullName;
                roboCommand.CopyOptions.Destination = destinationPath.FullName;
                roboCommand.OnFileProcessed += delegate (object obj, FileProcessedEventArgs args)
                {

                    transferStatus.FileCount++;
                    transferStatus.Progress = 100 * ((double)transferStatus.FileCount / transferStatus.FileTotal);
                    //File.Exists()                        
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        GetProgressWindow.TransferProgress.Items.Refresh();
                    }));
                };
                roboCommand.OnCommandCompleted += delegate (object obj, RoboCommandCompletedEventArgs args)
                {
                    transferStatus.Progress = 100;
                    transferStatus.CurrentStatus = "Done";
                    successCount++;
                    if (successCount == FolderNames.Count)
                    {
                        ErrorLogReport.Report("Transfer finished at " + DateTime.Now.ToString("h:mm:ss tt"));
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            GetProgressWindow.TransferProgress.Items.Refresh();
                            GetProgressWindow.LoadingPanel.Visibility = Visibility.Hidden;
                            StartButton.IsEnabled = true;
                            ManualTransferButton.IsEnabled = true;
                            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Collapsed;
                        }));
                        timer.Stop();
                    }
                };
                roboCommand.OnCopyProgressChanged += delegate (object obj, CopyProgressEventArgs args)
                {
                    //if (args.CurrentFileProgress == 100)
                    //{
                    //    Console.WriteLine(args.CurrentFileProgress);
                    //    transferStatus.FileCount++;
                    //    transferStatus.Progress = 100 * ((double)transferStatus.FileCount / transferStatus.FileTotal);
                    //    //File.Exists()                        
                    //    Dispatcher.BeginInvoke((Action)(() =>
                    //    {
                    //        GetProgressWindow.TransferProgress.Items.Refresh();
                    //    }));
                    //}
                };
                roboCommand.OnError += delegate (object obj, RoboSharp.ErrorEventArgs args)
                {
                    ErrorLogReport.Report(args.Error);
                    transferStatus.FileCount--;
                };
                roboCommand.OnCommandError += delegate (object obj, RoboSharp.ErrorEventArgs args)
                {
                    ErrorLogReport.Report(args.Error);
                };
                roboCommand.LoggingOptions.NoDirectoryList = true;
                roboCommand.LoggingOptions.NoFileClasses = true;
                roboCommand.LoggingOptions.NoFileSizes = true;
                roboCommand.LoggingOptions.NoJobHeader = true;
                roboCommand.LoggingOptions.NoDirectoryList = true;
                roboCommand.LoggingOptions.NoJobSummary = true;
                roboCommand.RetryOptions.RetryCount = 0;
                roboCommand.RetryOptions.RetryWaitTime = 0;
                roboCommand.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
                roboCommand.CopyOptions.MultiThreadedCopiesCount = 64;
                roboCommand.CopyOptions.Mirror = false;
                roboCommand.SelectionOptions.ExcludeFiles = "*.ost *.nst";
                roboCommand.CopyOptions.Purge = false;
                roboCommand.Start();
            }
            //ChromeBookmarkTransfer(externalDriveLetterPath + @"FDT\" + selectedUser + @"\Chrome Bookmarks\Bookmarks.File", @"C:\Users\" + selectedUser + @"\AppData\Local\Google\Chrome\User Data\Default\Bookmarks.File");
        }

        //Transfer to Old Computer Background Worker Complete
        private void OldBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
            GetProgressWindow.Status.Text = "Performing Transfer...";
            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Visible;
        }

        //Transfer to Old Computer Background Worker Do Work
        private void OldBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            rand = random.Next(0, 40);
            FactReport.Report(CatFacts[rand]);
            timer = new System.Timers.Timer(60000);
            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += delegate (object obj, ElapsedEventArgs args)
            {
                rand = random.Next(0, 40);
                FactReport.Report(CatFacts[rand]);
            };
            timer.AutoReset = true;
            timer.Enabled = true;

            ErrorLogReport.Report("Transfer started at " + DateTime.Now.ToString("h:mm:ss tt"));
            CloseApplications();
            GetPrinters();
            GetNetwork();
            GetAssetInfo();
            DirectoryInfo sourcePath;
            DirectoryInfo destinationPath;
            int successCount = 0;
            List<string> FolderNames = e.Argument as List<string>;

            foreach (string folderName in FolderNames)
            {                                
                int sourceFileCount = 0;
                sourcePath = new DirectoryInfo(UserDirectories.InitializeDictionary(selectedUser)[folderName]);
                destinationPath = new DirectoryInfo(externalDriveLetterPath + @"FDT\" + selectedUser + @"\" + folderName);
                if (folderName == "Chrome Bookmarks")
                {
                    TransferStatus chromeTransferStatus = new TransferStatus
                    {
                        Name = folderName,
                        Progress = 0,
                        FileCount = 0,
                        FileTotal = sourceFileCount,
                        CurrentStatus = "Transferring..."
                    };
                    TransferStatusProgress.Report(chromeTransferStatus);
                    try
                    {
                        ChromeBookmarkTransfer(@"C:\Users\" + selectedUser + @"\AppData\Local\Google\Chrome\User Data\Default\Bookmarks", externalDriveLetterPath + @"FDT\" + selectedUser + @"\Chrome Bookmarks\Bookmarks");
                        chromeTransferStatus.Progress = 100;
                        chromeTransferStatus.CurrentStatus = "Done";
                    }
                    catch
                    {
                        chromeTransferStatus.Progress = 100;
                        chromeTransferStatus.CurrentStatus = "Error Transferring File";
                    }
                    successCount++;
                    if (successCount == FolderNames.Count)
                    {
                        ErrorLogReport.Report("Transfer finished at " + DateTime.Now.ToString("h:mm:ss tt"));
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            GetProgressWindow.TransferProgress.Items.Refresh();
                            GetProgressWindow.LoadingPanel.Visibility = Visibility.Hidden;
                            StartButton.IsEnabled = true;
                            ManualTransferButton.IsEnabled = true;
                            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Collapsed;
                        }));
                        timer.Stop();
                    }
                    continue;
                }
                if (Directory.Exists(sourcePath.FullName))
                {
                    sourceFileCount = GetTotalFileCount(sourcePath);
                }
                TransferStatus transferStatus = new TransferStatus
                {
                    Name = folderName,
                    Progress = 0,
                    FileCount = 0,
                    FileTotal = sourceFileCount,
                    CurrentStatus = "Transferring..."
                };
                TransferStatusProgress.Report(transferStatus);
                roboCommand = new RoboCommand();
                roboCommand.CopyOptions.Source = sourcePath.FullName;
                roboCommand.CopyOptions.Destination = destinationPath.FullName;
                roboCommand.OnFileProcessed += delegate (object obj, FileProcessedEventArgs args)
                {
                    Console.WriteLine(args.ProcessedFile.FileClass + " " + args.ProcessedFile.FileClassType + " " +
                        args.ProcessedFile.Name + " " + args.ProcessedFile.Size);
                    transferStatus.FileCount++;
                    transferStatus.Progress = 100 * ((double)transferStatus.FileCount / transferStatus.FileTotal);
                    //File.Exists()                        
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        GetProgressWindow.TransferProgress.Items.Refresh();
                    }));
                };
                roboCommand.OnCommandCompleted += delegate (object obj, RoboCommandCompletedEventArgs args)
                {
                    transferStatus.Progress = 100;
                    transferStatus.CurrentStatus = "Done";
                    successCount++;
                    if (successCount == FolderNames.Count)
                    {
                        ErrorLogReport.Report("Transfer finished at " + DateTime.Now.ToString("h:mm:ss tt"));
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            GetProgressWindow.TransferProgress.Items.Refresh();
                            GetProgressWindow.LoadingPanel.Visibility = Visibility.Hidden;
                            StartButton.IsEnabled = true;
                            ManualTransferButton.IsEnabled = true;
                            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Collapsed;
                        }));
                        timer.Stop();
                    }
                };
                roboCommand.OnCopyProgressChanged += delegate (object obj, CopyProgressEventArgs args)
                {
                    //if(args.CurrentFileProgress == 100)
                    //{
                    //    Console.WriteLine(args.CurrentFileProgress);
                    //    transferStatus.FileCount++;
                    //    transferStatus.Progress = 100 * ((double)transferStatus.FileCount / transferStatus.FileTotal);
                    //    //File.Exists()                        
                    //    Dispatcher.BeginInvoke((Action)(() =>
                    //    {
                    //        GetProgressWindow.TransferProgress.Items.Refresh();
                    //    }));
                    //}
                };
                roboCommand.OnError += delegate (object obj, RoboSharp.ErrorEventArgs args)
                {
                    ErrorLogReport.Report(args.Error);
                    transferStatus.FileCount--;
                };
                roboCommand.OnCommandError += delegate (object obj, RoboSharp.ErrorEventArgs args)
                {
                    ErrorLogReport.Report(args.Error);
                };

                roboCommand.LoggingOptions.NoDirectoryList = true;
                roboCommand.LoggingOptions.NoFileClasses = false;
                roboCommand.LoggingOptions.NoFileSizes = false;
                roboCommand.LoggingOptions.NoJobHeader = true;
                roboCommand.LoggingOptions.NoDirectoryList = true;
                roboCommand.LoggingOptions.NoJobSummary = true;
                roboCommand.RetryOptions.RetryCount = 0;
                roboCommand.RetryOptions.RetryWaitTime = 0;
                roboCommand.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
                roboCommand.CopyOptions.MultiThreadedCopiesCount = 64;
                roboCommand.CopyOptions.Mirror = false;
                roboCommand.SelectionOptions.ExcludeFiles = "*.ost *.nst";
                roboCommand.CopyOptions.Purge = false;
                roboCommand.Start();
            }
            //ChromeBookmarkTransfer(@"C:\Users\" + selectedUser + @"\AppData\Local\Google\Chrome\User Data\Default\Bookmarks.File", externalDriveLetterPath + @"FDT\" + selectedUser + @"\Chrome Bookmarks\Bookmarks.File");
        }

        //Manual Transfer Background Worker Complete
        private void ManualTransferBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GetProgressWindow.LoadingPanel.Visibility = Visibility.Visible;
            GetProgressWindow.Status.Text = "Performing Transfer...";
            GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Visible;
        }

        //Manual Transfer Background Worker Do Work
        private void ManualTransferBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo sourcePath;
            DirectoryInfo destinationPath;
            int sourceFileCount = 0;
            rand = random.Next(0, 40);
            FactReport.Report(CatFacts[rand]);
            timer = new System.Timers.Timer(60000);
            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += delegate (object obj, ElapsedEventArgs args)
            {
                rand = random.Next(0, 40);
                FactReport.Report(CatFacts[rand]);
            };
            timer.AutoReset = true;
            timer.Enabled = true;
            ErrorLogReport.Report("Transfer started at " + DateTime.Now.ToString("h:mm:ss tt"));
            sourcePath = new DirectoryInfo(manualSourcePath);
            destinationPath = new DirectoryInfo(manualDestinationPath + @"\" + sourcePath.Name);
            if (Directory.Exists(sourcePath.FullName))
            {
                sourceFileCount = GetTotalFileCount(sourcePath);
            }
            TransferStatus transferStatus = new TransferStatus
            {
                Name = sourcePath.Name,
                Progress = 0,
                FileCount = 0,
                FileTotal = sourceFileCount,
                CurrentStatus = "Transferring..."
            };
            TransferStatusProgress.Report(transferStatus);
            roboCommand = new RoboCommand();
            roboCommand.CopyOptions.Source = sourcePath.FullName;
            roboCommand.CopyOptions.Destination = destinationPath.FullName;
            roboCommand.OnFileProcessed += delegate (object obj, FileProcessedEventArgs args)
            {
                Console.WriteLine(args.ProcessedFile.FileClass + " " + args.ProcessedFile.FileClassType + " " +
                    args.ProcessedFile.Name + " " + args.ProcessedFile.Size);
                transferStatus.FileCount++;
                transferStatus.Progress = 100 * ((double)transferStatus.FileCount / transferStatus.FileTotal);
                //File.Exists()                        
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    GetProgressWindow.TransferProgress.Items.Refresh();
                }));
            };
            roboCommand.OnCommandCompleted += delegate (object obj, RoboCommandCompletedEventArgs args)
            {
                transferStatus.Progress = 100;
                transferStatus.CurrentStatus = "Done";
                ErrorLogReport.Report("Transfer finished at " + DateTime.Now.ToString("h:mm:ss tt"));
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    GetProgressWindow.TransferProgress.Items.Refresh();
                    GetProgressWindow.LoadingPanel.Visibility = Visibility.Hidden;
                    StartButton.IsEnabled = true;
                    ManualTransferButton.IsEnabled = true;
                    GetProgressWindow.CancelTransfersButton.Visibility = Visibility.Collapsed;
                }));
                timer.Stop();

            };
            roboCommand.OnCopyProgressChanged += delegate (object obj, CopyProgressEventArgs args)
            {

            };
            roboCommand.OnError += delegate (object obj, RoboSharp.ErrorEventArgs args)
            {
                ErrorLogReport.Report(args.Error);
                transferStatus.FileCount--;
            };
            roboCommand.OnCommandError += delegate (object obj, RoboSharp.ErrorEventArgs args)
            {
                ErrorLogReport.Report(args.Error);
            };

            roboCommand.LoggingOptions.NoDirectoryList = true;
            roboCommand.LoggingOptions.NoFileClasses = false;
            roboCommand.LoggingOptions.NoFileSizes = false;
            roboCommand.LoggingOptions.NoJobHeader = true;
            roboCommand.LoggingOptions.NoDirectoryList = true;
            roboCommand.LoggingOptions.NoJobSummary = true;
            roboCommand.RetryOptions.RetryCount = 0;
            roboCommand.RetryOptions.RetryWaitTime = 0;
            roboCommand.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            roboCommand.CopyOptions.MultiThreadedCopiesCount = 64;
            roboCommand.CopyOptions.Mirror = false;
            roboCommand.SelectionOptions.ExcludeFiles = "*.ost *.nst";
            roboCommand.CopyOptions.Purge = false;
            roboCommand.Start();
        }

        public DateTime GetNistTime()
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
            var response = myHttpWebRequest.GetResponse();
            string todaysDates = response.Headers["date"];
            return DateTime.ParseExact(todaysDates,
                                       "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                       CultureInfo.InvariantCulture.DateTimeFormat,
                                       DateTimeStyles.AssumeUniversal);
        }

        public bool ValidateProgram()
        {
            try
            {
                DateTime nistTime = new DateTime();
                nistTime = GetNistTime();
                int val = nistTime.CompareTo(expirationDate);
                if (val == 1)
                {
                    //Program not valid
                    return false;
                }
            }
            catch (System.Windows.Markup.XamlParseException sEMXE3)
            {
                //Still possibly not connected to internet
                return false;
            }
            catch (System.Net.WebException sNWE)
            {
                //Not connected to Internet
                return false;
            }
            return true;
        }

        public void ExpiredDisable()
        {
            StartButton.IsEnabled = false;
            ViewTransfers.IsEnabled = false;
        }

        public void CloseApplications()
        {
            //Kill Skype
            try
            {
                Process[] proc = Process.GetProcessesByName("lync");
                proc[0].Kill();
            }
            catch (Exception exc)
            {

            }

            //Kill Outlook
            try
            {
                Process[] proc = Process.GetProcessesByName("outlook");
                proc[0].Kill();
            }
            catch (Exception exc)
            {

            }
            //Kill Chrome
            try
            {
                Process[] proc = Process.GetProcessesByName("chrome");
                proc[0].Kill();
            }
            catch (Exception exc)
            {

            }
        }
    }
}