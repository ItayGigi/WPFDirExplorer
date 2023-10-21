using System;
using System.Collections.Generic;
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
using Shell32;

namespace WPFDirExplorer
{
    /// <summary>
    /// Interaction logic for ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {
        public ControlWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window1 window = new Window1();
            window.Show();
            window.Title = "Starting Clue";
        }

        public void OnSelectItem(FolderItem item)
        {
            if (item == null)
            {
                label.Content = string.Empty;
                return;
            }

            if (item.Type == "File" && item.Name.EndsWith(" (Locked)"))
            {
                label.Content = "File is locked.";
            }
        }
    }
}
