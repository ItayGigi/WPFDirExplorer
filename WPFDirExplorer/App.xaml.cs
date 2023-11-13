using Microsoft.Win32;
using SHDocVw;
using Shell32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Security.AccessControl;

namespace WPFDirExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")] public static extern int SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern int GetWindowLong(IntPtr hwnd, int index);

        private BackgroundWorker _eventListener;
        public string GameFolder = "C:\\Users\\Itay\\Desktop\\Game";
        public string GameRefFolder = String.Empty;
        public string CaseName;

        ControlWindow _window;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = new ExitWindow();
            MainWindow.Show();

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                GameFolder = dialog.FileName;
                MessageBox.Show("You selected: " + dialog.FileName);
            }
            else
            {
                Application.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
            }

            _window = new ControlWindow();
            _window.Show();
            _window.Hide();

            _window.SourceInitialized += delegate { ApplyWindowStyle(_window); };

            // Initialize and configure the BackgroundWorker
            _eventListener = new BackgroundWorker();
            _eventListener.DoWork += AsyncMain;

            // Start the BackgroundWorker
            _eventListener.RunWorkerAsync();

            _window.ResizeMode = ResizeMode.NoResize;
            _window.WindowStyle = WindowStyle.None;
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
            FolderItem lastItem = null;

            while (true)
            {
                IntPtr hwnd = IntPtr.Zero;

                InternetExplorer? gameWindow = GetGameWindow(ref hwnd);

                if (hwnd != lastHwnd)
                {
                    Dispatcher.Invoke(() =>
                    {
                        //listView.Items.Add(hwnd);
                        SetParent(new WindowInteropHelper(_window).Handle, hwnd);

                        if (hwnd != IntPtr.Zero)
                        {
                            _window.Show();
                            _window.Top = gameWindow.Height - _window.Height - gameWindow.Top - 10;
                            _window.Left = gameWindow.Width - _window.Width - 10;
                        }
                        else
                        {
                            _window.Hide();
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
                        _window.Top = gameWindow.Height - _window.Height - gameWindow.Top - 10;
                        _window.Left = gameWindow.Width - _window.Width - 10;
                    });

                    lastSize = size;
                }

                FolderItems selectedItems = ((IShellFolderViewDual2)gameWindow.Document).SelectedItems();

                if (selectedItems.Count == 1)
                {
                    if (selectedItems.Item(0) != lastItem)
                    {
                        lastItem = selectedItems.Item(0);
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _window.OnSelectItem(lastItem);
                            });
                        }
                        catch (Exception) { }
                    }
                }
                else
                {
                    if (lastItem != null)
                    {
                        lastItem = null;

                        Dispatcher.Invoke(() =>
                        {
                            _window.OnSelectItem(null);
                        });
                    }
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
                    if (!path.StartsWith(GameFolder)) continue;

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

        public void ApplyWindowStyle(Window window)
        {
            var interopHelper = new WindowInteropHelper(window);
            int exStyle = GetWindowLong(interopHelper.Handle, GWL_EXSTYLE);
            SetWindowLong(interopHelper.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
        }

        public void GenerateGame()
        {
            string exePath = Directory.GetCurrentDirectory();//Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
                    File.Delete(path.Remove(path.Length- " (Locked)".Length));
                }
            }
        }

        public void Quit()
        {
            if (!string.IsNullOrEmpty(GameRefFolder))
            {
                Directory.Delete(GameRefFolder, true);
                try { Directory.Delete(System.IO.Path.Combine(GameFolder, CaseName), true); }
                catch { }
            }

            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
