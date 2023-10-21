using SHDocVw;
using Shell32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFDirExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public const int GWL_EXSTYLE = -20;
		public const int WS_EX_LAYERED = 0x00080000;
		public const int WS_EX_TRANSPARENT = 0x00000020;
		public const int WS_EX_TOOLWINDOW = 0x00000080;

		[DllImport("user32.dll")] public static extern int SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern int GetWindowLong(IntPtr hwnd, int index);

		private BackgroundWorker _eventListener;
		string _gameFolder = "C:\\Users\\Itay\\Desktop\\Game";

		public MainWindow()
		{
			InitializeComponent();

			SourceInitialized += delegate { ApplyWindowStyle(); } ;

			// Initialize and configure the BackgroundWorker
			_eventListener = new BackgroundWorker();
			_eventListener.DoWork += AsyncMain;

			// Start the BackgroundWorker
			_eventListener.RunWorkerAsync();

			ResizeMode = ResizeMode.NoResize;
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Minimized;
		}

		public void AsyncMain(object sender, DoWorkEventArgs e)
		{
			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
			{
				MainSTAThread();
			}
			else
			{
				Thread staThread = new Thread(new ThreadStart(MainSTAThread));
				staThread.SetApartmentState(ApartmentState.STA);
				staThread.Start();
				staThread.Join();
			}
		}

		void MainSTAThread()
		{
			string lastPath = string.Empty;
			IntPtr lastHwnd = IntPtr.Zero;
			Vector2 lastSize = Vector2.Zero;

			while (true)
			{
				IntPtr hwnd = IntPtr.Zero;

				InternetExplorer? gameWindow = GetGameWindow(ref hwnd);

				if (hwnd != lastHwnd)
				{
					Dispatcher.Invoke(() =>
					{
						//listView.Items.Add(hwnd);
						SetParent(new WindowInteropHelper(this).Handle, hwnd);

						if (hwnd != IntPtr.Zero)
						{
							WindowState = WindowState.Normal;
							Top = gameWindow.Height - Height - gameWindow.Top;
							Left = gameWindow.Width - Width;
						}
						else
						{
							WindowState = WindowState.Minimized;
						}
					});

					lastHwnd = hwnd;
				}

				try
				{
					if (gameWindow == null) continue;
					if (gameWindow.Document == null) continue;
				}
				catch (Exception) { continue; }

				Folder folder = ((IShellFolderViewDual2)gameWindow.Document).Folder;
				string path = ((Folder3)folder).Self.Path;
				
				if (path != lastPath)
				{
					//Dispatcher.Invoke(() =>
					//{
					//	listView.Items.Add(path);
					//});

					lastPath = path;
				}

				Vector2 size = new Vector2(gameWindow.Width, gameWindow.Height);

				if (size != lastSize)
				{
					Dispatcher.Invoke(() =>
					{
						Top = gameWindow.Height - Height - gameWindow.Top;
						Left = gameWindow.Width - Width;
					});

					lastSize = size;
				}
			}
		}

		InternetExplorer? GetGameWindow(ref IntPtr hwnd)
		{
			InternetExplorer gameWindow = null;
			foreach (InternetExplorer window in new ShellWindows()) //find window
			{
				string filename = System.IO.Path.GetFileNameWithoutExtension(window.FullName).ToLower();
				if (filename.ToLowerInvariant() != "explorer") continue;

				try
				{
					Folder folder = ((IShellFolderViewDual2)window.Document).Folder;
					string path = ((Folder3)folder).Self.Path;
					if (!path.StartsWith(_gameFolder)) continue;

					if (hwnd == (IntPtr)window.HWND) gameWindow = window;

					if (hwnd == IntPtr.Zero)
					{
						hwnd = (IntPtr)window.HWND;
						gameWindow = window;
					}
					else if (hwnd != (IntPtr)window.HWND)
					{
						window.Quit();
					}
				}
				catch (Exception) { }
			}

			if (hwnd != IntPtr.Zero && gameWindow == null)
			{
				hwnd = IntPtr.Zero;
			}

			return gameWindow;
		}

		public void ApplyWindowStyle()
		{
			var interopHelper = new WindowInteropHelper(this);
			int exStyle = GetWindowLong(interopHelper.Handle, GWL_EXSTYLE);
			SetWindowLong(interopHelper.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
		}
	}
}
