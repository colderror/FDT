using System;
using System.Collections.Generic;
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
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();            
            Plaq.Text = "Developed and Designed by\nReginald Brown";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            VersionInfo.Text = "FDT Build Version " + version;
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AboutUI.Hide();
        }

        private void AboutUI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            AboutUI.Hide();
        }
        private void AboutUI_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        public void OpenAbout()
        {
            AboutUI.Show();
        }
    }
}
