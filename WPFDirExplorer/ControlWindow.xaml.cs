using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
	public partial class ControlWindow : Window
	{
		string[] _lockedFileContent;
		FolderItem? _selectedItem;

		public ControlWindow()
		{
			InitializeComponent();

			lockedCanvas.Visibility = Visibility.Hidden;
			clueButton.Visibility = Visibility.Hidden;

			((App)Application.Current).OnSelectItem += onSelectItem;
        }

        private void numberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
		{
			ClueWindow window = new ClueWindow(((App)Application.Current).GameRefFolder + ((App)Application.Current).CaseName + @"\StartingClue.txt");
			window.Show();
			window.Title = "Starting Clue";
		}

		private void onSelectItem(FolderItem? item)
		{
			_selectedItem = item;

            lockedCanvas.Visibility = Visibility.Hidden;

            if (item == null)
				return;

			if (item.Type == "File" && item.Name.EndsWith(" (Locked)"))
			{
				lockedCanvas.Visibility = Visibility.Visible;
				_lockedFileContent = File.ReadAllLines(((App)Application.Current).GameRefFolder + item.Path.Substring(((App)Application.Current).GameFolder.Length));
				hintLabel.Content = "Saved hint: " + _lockedFileContent[0];
			}
		}

		private void generateButton_Click(object sender, RoutedEventArgs e)
		{
			((App)App.Current).GenerateGame();
			clueButton.Visibility = Visibility.Visible;
			generateButton.IsEnabled = false;
		}

		private void quitButton_Click(object sender, RoutedEventArgs e)
		{
			((App)Application.Current).Quit();
		}

		private void passwordButton_Click(object sender, RoutedEventArgs e)
		{
			if (_lockedFileContent == null || _lockedFileContent.Length < 2) return;

			for (int i = 1; i < _lockedFileContent.Length; i++)
			{
				if (passwordBox.Text == _lockedFileContent[i]) // match
				{
                    Task.Run(() => { MessageBox.Show("File Unlocked."); });

                    string dest = _selectedItem.Path.Remove(_selectedItem.Path.Length - " (Locked)".Length);
                    string source = ((App)Application.Current).GameRefFolder + dest.Substring(((App)Application.Current).GameFolder.Length);
                    File.Copy(source, dest);

                    File.Delete(_selectedItem.Path);
                    _selectedItem = null;

					return;
				}
			}

            Task.Run(() => { MessageBox.Show("Password is incorrect."); });
		}
    }
}
