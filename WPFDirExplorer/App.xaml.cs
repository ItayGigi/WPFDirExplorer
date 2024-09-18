using SHDocVw;
using Shell32;
using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;


namespace WPFDirExplorer
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        public static extern int SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr handle, int nCmdShow);


        public string GameFolder;
        public string GameRefFolder = String.Empty;
        public string CaseName;

        public delegate void ItemSelectEventHandler(FolderItem? item);
        public event ItemSelectEventHandler? OnSelectItem;

        private ControlWindow _controlWindow;
        private BackgroundWorker _eventListener;
        private InternetExplorer? _explorerWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Select main game folder";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                GameFolder = dialog.FileName;
            }
            else
            {
                Application.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
                return;
            }

            MainWindow = new MainWindow();
            ((MainWindow)MainWindow).SetFolder(GameFolder);
            MainWindow.Show();

            _controlWindow = new ControlWindow();
            _controlWindow.Show();
            _controlWindow.Hide();

            // Initialize and configure the BackgroundWorker
            _eventListener = new BackgroundWorker();
            _eventListener.DoWork += AsyncMain;

            // Start the BackgroundWorker
            _eventListener.RunWorkerAsync();
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
            Vector2 lastSize = Vector2.Zero;
            FolderItem? lastItem = null;

            while (true)
            {
                try
                {
                    _explorerWindow = scanExplorers(_explorerWindow);

                    if (_explorerWindow == null) continue;
                    if (_explorerWindow.Document == null) continue;

                    // handle explorer resize
                    if (_explorerWindow.Width != lastSize.X || _explorerWindow.Height != lastSize.Y)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _controlWindow.Top = _explorerWindow.Height - _controlWindow.Height - _explorerWindow.Top - 10;
                            _controlWindow.Left = _explorerWindow.Width - _controlWindow.Width - 10;
                        });

                        lastSize = new Vector2(_explorerWindow.Width, _explorerWindow.Height);
                    }

                    FolderItems? selectedItems = ((IShellFolderViewDual2)_explorerWindow.Document).SelectedItems();

                    FolderItem? selectedItem = (selectedItems != null && selectedItems.Count == 1) ? selectedItems.Item(0) : null;

                    if (selectedItem != lastItem)
                    {
                        Dispatcher.Invoke(() => { OnSelectItem?.Invoke(selectedItem); });

                        lastItem = selectedItem;
                    }
                }
                catch (Exception ex)
                {
                    //Task.Run(() => { MessageBox.Show(ex.Message); });
                    continue;
                }
            }
        }

        InternetExplorer? scanExplorers(InternetExplorer? currExplorer)
        {
            InternetExplorer? gameWindow = null;
            foreach (InternetExplorer window in new ShellWindows()) //find window
            {
                string filename = System.IO.Path.GetFileName(window.FullName).ToLower();
                if (filename != "explorer.exe") continue;

                Folder folder = ((IShellFolderViewDual2)window.Document).Folder;
                string path = ((Folder3)folder).Self.Path;
                if (!path.StartsWith(GameFolder)) continue;

                if (gameWindow != null || (currExplorer != null && (IntPtr)currExplorer.HWND != (IntPtr)window.HWND)) // not our match
                {
                    window.Quit();
                    continue;
                }

                // match
                gameWindow = window;

                if (currExplorer == null) // new match
                {
                    gameWindow.OnQuit += explorerOnQuit;
                    gameWindow.NavigateComplete2 += explorerOnNavigate;

                    Dispatcher.Invoke(() =>
                    {
                        SetParent(new WindowInteropHelper(_controlWindow).Handle, (IntPtr)gameWindow.HWND);

                        _controlWindow.Show();
                    });
                }
            }

            return gameWindow;
        }

        void explorerOnQuit()
        {
            _explorerWindow = null;

            Dispatcher.Invoke(() =>
            {
                _controlWindow.Hide();
                SetParent(new WindowInteropHelper(_controlWindow).Handle, IntPtr.Zero);
            });
        }

        void explorerOnNavigate([MarshalAs(UnmanagedType.IDispatch)] object pDisp, [MarshalAs(UnmanagedType.Struct)] ref object URL)
        {
            if (URL.ToString() == null || !URL.ToString().StartsWith(GameFolder)) // no longer in game folder
            {
                _explorerWindow.OnQuit -= explorerOnQuit;
                _explorerWindow.NavigateComplete2 -= explorerOnNavigate;

                explorerOnQuit();

                return;
            }

            // folder change
        }

        public void GenerateGame()
        {
            string exePath = Directory.GetCurrentDirectory();
            string zipPath = exePath + @"\Cases\1.zip";
            GameRefFolder = exePath + @"\Cases\1\";
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, GameRefFolder);

            CaseName = Path.GetFileName(Directory.GetDirectories(GameRefFolder)[0]);
            string casePath = Path.Combine(GameFolder, CaseName);

            if (Directory.Exists(casePath)) Directory.Delete(casePath, true);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, GameFolder);

            File.Delete(casePath + @"\StartingClue.txt");

            foreach (string path in Directory.EnumerateFiles(casePath, "", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(path).EndsWith(" (Locked)"))
                {
                    File.WriteAllText(path, string.Empty);
                    File.Delete(path.Remove(path.Length - " (Locked)".Length));
                }
            }
        }

        public void Open()
        {
            if (_explorerWindow != null)
            {
                ShowWindow((IntPtr)_explorerWindow.HWND, 9);
                SetForegroundWindow((IntPtr)_explorerWindow.HWND);
            }
            else
            {
                Process.Start("explorer.exe", GameFolder);
            }
        }

        public void Quit()
        {
            if (!string.IsNullOrEmpty(GameRefFolder))
            {
                Directory.Delete(GameRefFolder, true);

                string caseFolder = System.IO.Path.Combine(GameFolder, CaseName);
                if (Directory.Exists(caseFolder))
                    Directory.Delete(caseFolder, true);
            }

            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
