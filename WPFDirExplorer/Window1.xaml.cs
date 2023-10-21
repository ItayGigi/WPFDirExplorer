using System;
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
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();

            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/StartingClue.txt";

			string text = File.ReadAllText(path);
			parseToTextBlock(text, textBlock);
		}

		void parseToTextBlock(string text, TextBlock textBlock)
		{
			textBlock.Text = string.Empty;

			text = text.Replace("[Current_Date]", DateOnly.FromDateTime(DateTime.Now).ToString());
			string[] boldSections = text.Split("[b]");

			for (int i = 0; i < boldSections.Length; i++)
			{
				string[] italicSections = boldSections[i].Split("[i]");

				for (int j = 0; j < italicSections.Length; j++)
				{
					textBlock.Inlines.Add(new Run(italicSections[j])
					{
						FontWeight = (i % 2 == 1)? FontWeights.Bold : FontWeights.Normal,
						FontStyle = (j % 2 == 1)? FontStyles.Italic : FontStyles.Normal
					});
                }
			}
        }
	}
}
