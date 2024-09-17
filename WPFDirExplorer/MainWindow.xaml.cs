using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

namespace WPFDirExplorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Closing += onWindowClosing;
        }

        public void SetFolder(string folder)
        {
            folderText.Text = "Game folder:\n" + folder;
        }

        private void openClick(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Open();
        }

        private void quitClick(object sender, RoutedEventArgs e)
        {
            if (quitPrompt() == MessageBoxResult.Yes)
                ((App)Application.Current).Quit();
        }

        private void onWindowClosing(object? sender, CancelEventArgs e)
        {
            if (quitPrompt() == MessageBoxResult.Yes)
                ((App)Application.Current).Quit();
            else
                e.Cancel = true;
        }

        private MessageBoxResult quitPrompt()
        {
            return MessageBox.Show("Do you want to quit the game? The case folder will be deleted.", "", MessageBoxButton.YesNo);
        }
    }
}
