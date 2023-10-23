﻿using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for ExitWindow.xaml
    /// </summary>
    public partial class ExitWindow : Window
    {
        public ExitWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(((App)Application.Current).GameRefFolder))
            {
                Directory.Delete(((App)Application.Current).GameRefFolder, true);
                Directory.Delete(System.IO.Path.Combine(((App)Application.Current).GameFolder, ((App)Application.Current).CaseName), true);
            }

            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
