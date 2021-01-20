using VB = Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WinForms = System.Windows.Forms;
using System.Windows.Documents;
using System.Timers;
using System.Windows.Controls;

namespace Minecraft_Server_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Warnings = new string[6], Errors = new string[2];

        enum TypeOfWarnings
        {
            FetchRamUsageFailiure,
            CountWorlds,
            CountModsPlugins,
            ModsAndPluginsInSameInstance,
            MissingModsPlugins,
            LoadPreferences
        }

        enum TypeOfErrors
        {
            InvalidServerPath,
            InvalidRamSettings
        }

        string LastPanel = "Console", ServerPath = "", WorkingDirectory = "", PublicIP, LocalIP;
        bool ServerIsRunning = false;

        Stopwatch ServerUpTime = new Stopwatch();
        Timer UpdateServerUptime = new Timer(1000);
        Timer UpdateResourceUsage = new Timer(100);
        FileSystemWatcher WorldFileWatcher = new FileSystemWatcher();
        FileSystemWatcher ModsPluginsFileWatcher = new FileSystemWatcher();

        Color WarningOutputColor = Colors.Yellow, ErrorOutputColor = Colors.Red, PlayerEventOutputColor = Colors.AliceBlue, ServerDoneLoadingColor = Colors.Lime, DefaultOutputColor = Colors.White;

        Process ServerProcess = new Process();
        ProcessStartInfo ServerArgs;

        public MainWindow()
        {
            InitializeComponent();
            //Get Public IP
            try
            {
                PublicIP = new WebClient().DownloadString(new Uri("http://ipinfo.io/ip")).Trim();
                InfoPublicIP.Text = PublicIP;
                CopyPublicIP.IsEnabled = true;
                ShowHidePublicIP.IsEnabled = true;
                ShowHidePublicIPIcon.Opacity = 1;
            }
            catch (Exception) { }

            //Get Local IP
            try
            {
                IPHostEntry ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] address = ipHostEntry.AddressList;
                LocalIP = address[3].ToString();
                InfoLocalIP.Text = LocalIP;
                CopyLocalIP.IsEnabled = true;
                ShowHideLocalIP.IsEnabled = true;
                ShowHideLocalIPIcon.Opacity = 1;
            }
            catch (Exception) { }

            //Load Preferences
            try
            {
                LoadPreferences();
            }
            catch (Exception)
            {
                Warnings[(int)TypeOfWarnings.LoadPreferences] = "Failed to load preferences";
            }

            //Get Max Available System Memory
            GetTotalPhysicalMemory();

            //Set Timers
            UpdateServerUptime.AutoReset = true;
            UpdateServerUptime.Elapsed += UpdateServerUptime_Elapsed;
            UpdateResourceUsage.AutoReset = true;
            UpdateResourceUsage.Elapsed += UpdateResourceUsage_Elapsed;

            WorldFileWatcher.Changed += WorldFileChanged;
            WorldFileWatcher.Created += WorldFileChanged;
            WorldFileWatcher.Deleted += WorldFileChanged;
            WorldFileWatcher.Renamed += WorldFileChanged;

            ModsPluginsFileWatcher.Changed += ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Created += ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Deleted += ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Renamed += ModsPluginsFileChanged;
        }

        private void GetRamUsage()
        {
            if (ServerIsRunning)
            {
                Dispatcher.Invoke(() =>
                {
                    long RAMUsage = 0;
                    ServerProcess.Refresh();
                    switch (InfoRamUsageUnit.SelectedIndex)
                    {
                        case 0:
                            RAMUsage = ServerProcess.WorkingSet64;
                            break;
                        case 1:
                            RAMUsage = ServerProcess.WorkingSet64 / 1024;
                            break;
                        case 2:
                            RAMUsage = ServerProcess.WorkingSet64 / 1024 / 1024;
                            break;
                        case 3:
                            RAMUsage = (long)Math.Round(Convert.ToDouble(ServerProcess.WorkingSet64 / 1024 / 1024 / 1024), 1);
                            break;
                    }

                    RAMUsageIndicator.Content = "RAM Usage: " + RAMUsage + "MB";
                    InfoRamUsage.Text = RAMUsage.ToString();
                });
            }
        }

        private void GetTotalPhysicalMemory()
        {
            switch (InfoSystemRAMUnit.SelectedIndex)
            {
                case 0:
                    InfoSystemRAM.Text = new VB.ComputerInfo().TotalPhysicalMemory.ToString();
                    break;
                case 1:
                    InfoSystemRAM.Text = (new VB.ComputerInfo().TotalPhysicalMemory / 1024).ToString();
                    break;
                case 2:
                    InfoSystemRAM.Text = (new VB.ComputerInfo().TotalPhysicalMemory / 1024 / 1024).ToString();
                    break;
                case 3:
                    InfoSystemRAM.Text = Math.Round((new VB.ComputerInfo().TotalPhysicalMemory / 1024f / 1024f / 1024f), 1).ToString();
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateServerUptime.Elapsed -= UpdateServerUptime_Elapsed;
            UpdateResourceUsage.Elapsed -= UpdateResourceUsage_Elapsed;

            WorldFileWatcher.Changed -= WorldFileChanged;
            WorldFileWatcher.Created -= WorldFileChanged;
            WorldFileWatcher.Deleted -= WorldFileChanged;
            WorldFileWatcher.Renamed -= WorldFileChanged;

            ModsPluginsFileWatcher.Changed -= ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Created -= ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Deleted -= ModsPluginsFileChanged;
            ModsPluginsFileWatcher.Renamed -= ModsPluginsFileChanged;

            if (ServerIsRunning)
            {
                ServerProcess.StandardInput.WriteLine("stop");
            }
        }

        private void SavePreferences(bool SaveToGlobal)
        {
            string[] PreferenceOptions = new string[6];
            try { if (File.Exists(ServerPath) && ServerPath.Length > 0) { PreferenceOptions[0] = "Server Path=" + ServerPath; } } catch (Exception) { }
            try { PreferenceOptions[1] = "RAM=" + ServerRamBox.Value; } catch (Exception) { }

            if (SaveToGlobal)
            {

            }

            if (!SaveToGlobal)
            {
                string PrefFilePath = Directory.GetCurrentDirectory() + "\\Minecraft_Server_GUI.txt";

                if (File.Exists(PrefFilePath))
                {
                    File.WriteAllLines(PrefFilePath, PreferenceOptions);
                }
                else
                {
                    File.Create(PrefFilePath);
                    File.WriteAllLines(PrefFilePath, PreferenceOptions);
                }
            }
        }

        private void LoadPreferences()
        {
            string[] prefs = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Minecraft_Server_GUI.txt");
            if (prefs[0] != "")
            {
                ServerPathBox.Text = prefs[0].Split('=')[1];
            }
        }

        private void ModsPluginsFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() => {
                ModsPluginsListBox.Items.Clear();
                if (Directory.Exists(WorkingDirectory + @"\plugins"))
                {
                    ModsPluginsLabel.Content = "Plugins";
                    string[] _Plugins = Directory.GetFiles(WorkingDirectory + @"\plugins");
                    foreach (var item in _Plugins)
                    {
                        ModsPluginsListBox.Items.Add(Path.GetFileNameWithoutExtension(item));
                    }
                }

                if (Directory.Exists(WorkingDirectory + @"\mods"))
                {
                    ModsPluginsLabel.Content = "Mods";
                    string[] _Mods = Directory.GetFiles(WorkingDirectory + @"\mods");
                    foreach (var item in _Mods)
                    {
                        ModsPluginsListBox.Items.Add(Path.GetFileNameWithoutExtension(item));
                    }
                }

                ModsPluginsCountLabel.Content = ModsPluginsListBox.Items.Count;
            });
        }

        private void WorldFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() => {
                WorldListBox.Items.Clear();
                foreach (var folder in Directory.EnumerateDirectories(WorkingDirectory.ToString()))
                {
                    if (File.Exists(folder + @"\level.dat"))
                    {
                        string[] temp = folder.Split('\\');
                        WorldListBox.Items.Add(temp[temp.Length - 1]);
                    }
                }
            });
        }

        private void UpdateFaultList()
        {

            int Faults = 0;

            if (Warnings.Length == 0)
            {
                InfoWarningList.Items.Clear();
                InfoWarningList.Items.Add("No Warnings");
            }
            else
            {
                InfoWarningList.Items.Clear();
                foreach (string item in Warnings) { if (item != null) { InfoWarningList.Items.Add(item); Faults++; } }
                
            }

            if (Errors.Length == 0)
            {
                InfoErrorList.Items.Clear();
                InfoErrorList.Items.Add("No Errors");
            }
            else
            {
                InfoErrorList.Items.Clear();
                foreach (string item in Errors) { if (item != null) { InfoErrorList.Items.Add(item); Faults++; } }
            }

            InfoFaultCount.Content = Faults;
            if (Faults == 0) { ShowInfoNotifBadge.Visibility = Visibility.Collapsed; } else { ShowInfoNotifBadge.Visibility = Visibility.Visible; }
        }

        private void UpdateResourceUsage_Elapsed(object sender, ElapsedEventArgs e) => GetRamUsage();

        private void UpdateServerUptime_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ServerUptimeIndicator.Content = "Uptime: " + ServerUpTime.Elapsed.ToString("hh\\:mm\\:ss");
                InfoServerUptime.Text = ServerUpTime.Elapsed.ToString("hh\\:mm\\:ss");
            });
        }

        private void ClosePanel()
        {
            switch (LastPanel)
            {
                case "Console":
                    ConsolePanel.Visibility = Visibility.Collapsed;
                    break;

                case "WorldsModsPlugins":
                    WorldsModsPluginsPanel.Visibility = Visibility.Collapsed;
                    break;

                case "Server Info":
                    ServerInfoPanel.Visibility = Visibility.Collapsed;
                    break;

                case "NewServer":
                    NewServerPanel.Visibility = Visibility.Collapsed;
                    break;

                case "Settings":
                    SettingsPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        //Check server is ready to start
        private void IsServerReady(string LastChangedProperty)
        {
            bool IsServerPathValid = false, IsRAMValueValid = false;
            Array.Clear(Warnings, 0, Warnings.Length);
            Array.Clear(Errors, 0, Errors.Length);

            WorldFileWatcher.EnableRaisingEvents = false;
            ModsPluginsFileWatcher.EnableRaisingEvents = false;

            //Check Server Path
            if (File.Exists(ServerPathBox.Text) && Path.GetExtension(ServerPathBox.Text) == ".jar")
            {
                ServerPath = ServerPathBox.Text;
                ServerPathBox.Foreground = new SolidColorBrush(Colors.White);
                WorkingDirectory = Path.GetDirectoryName(ServerPath);
                WorldFileWatcher.Path = WorkingDirectory;

                if (Directory.Exists(WorkingDirectory + "\\plugins") && Directory.Exists(WorkingDirectory + "\\mods"))
                {
                    InfoModPluginsLabel.Content = "Could not count Mods/Plugins ";
                    Warnings[(int)TypeOfWarnings.ModsAndPluginsInSameInstance] = "Found Mods and Plugins folder in same Server";
                }
                else if (Directory.Exists(WorkingDirectory + "\\plugins"))
                {
                    InfoModPluginsLabel.Content = "Plugins Count: ";
                    ModsPluginsFileWatcher.Path = WorkingDirectory + "\\plugins";
                }
                else if (Directory.Exists(WorkingDirectory + "\\mods"))
                {
                    InfoModPluginsLabel.Content = "Mods Count: ";
                    ModsPluginsFileWatcher.Path = WorkingDirectory + "\\mods";
                }
                else
                {
                    InfoModPluginsLabel.Content = "Could not count Mods/Plugins ";
                    Warnings[(int)TypeOfWarnings.MissingModsPlugins] = "Missing Mods/Plugins folder";
                }

                //Populate World Mods and Plugins Listboxes
                Dispatcher.Invoke(() => {
                    ModsPluginsListBox.Items.Clear();
                    if (Directory.Exists(WorkingDirectory + @"\plugins"))
                    {
                        ModsPluginsLabel.Content = "Plugins";
                        string[] _Plugins = Directory.GetFiles(WorkingDirectory + @"\plugins");
                        foreach (var item in _Plugins)
                        {
                            ModsPluginsListBox.Items.Add(Path.GetFileNameWithoutExtension(item));
                        }
                    }

                    if (Directory.Exists(WorkingDirectory + @"\mods"))
                    {
                        ModsPluginsLabel.Content = "Mods";
                        string[] _Mods = Directory.GetFiles(WorkingDirectory + @"\mods");
                        foreach (var item in _Mods)
                        {
                            ModsPluginsListBox.Items.Add(Path.GetFileNameWithoutExtension(item));
                        }
                    }
                    ModsPluginsCountLabel.Content = ModsPluginsListBox.Items.Count;

                    WorldListBox.Items.Clear();
                    foreach (var folder in Directory.EnumerateDirectories(WorkingDirectory.ToString()))
                    {
                        if (File.Exists(folder + @"\level.dat"))
                        {
                            string[] temp = folder.Split('\\');
                            WorldListBox.Items.Add(temp[temp.Length - 1]);
                        }
                    }
                    ModsPluginsCountLabel.Content = ModsPluginsListBox.Items.Count;
                    InfoModPluginsCount.Content = ModsPluginsListBox.Items.Count;
                    WorldCountLabel.Content = WorldListBox.Items.Count;
                    InfoWorldCount.Content = WorldListBox.Items.Count;
                });

                IsServerPathValid = true;
                if (LastChangedProperty == "ServerPath") { StatusText.Content = "Server Path has been changed"; StatusLED.Fill = new SolidColorBrush(Colors.White); }

                InfoServerPath.Text = ServerPath;
                InfoWorkingDirectory.Text = WorkingDirectory;

                InfoServerPathShowInExplorer.IsEnabled = true;
                InfoWorkingDirectoryShowInExplorer.IsEnabled = true;
                BackupWorlds.IsEnabled = true;
                OpenServerFolder.IsEnabled = true;
                WorldFileWatcher.EnableRaisingEvents = true;
                ModsPluginsFileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                ServerPathBox.Foreground = new SolidColorBrush(Colors.Red);
                IsServerPathValid = false;
                if (LastChangedProperty == "ServerPath") { StatusText.Content = "Server Path is Invalid"; StatusLED.Fill = new SolidColorBrush(Colors.Red); }

                InfoServerPath.Text = "Not Set";
                InfoWorkingDirectory.Text = "Not Set";

                InfoServerPathShowInExplorer.IsEnabled = false;
                InfoWorkingDirectoryShowInExplorer.IsEnabled = false;
                BackupWorlds.IsEnabled = false;
                OpenServerFolder.IsEnabled = false;

                Errors[(int)TypeOfErrors.InvalidServerPath] = "Invalid Server Path";
                WorldFileWatcher.EnableRaisingEvents = false;
                ModsPluginsFileWatcher.EnableRaisingEvents = false;
            }

            //Check Ram Setting
            try
            {
                if (int.TryParse(ServerRamBox.Value.ToString(), out _))
                {
                    IsRAMValueValid = true;
                }
                else
                {
                    IsRAMValueValid = false;
                    Errors[(int)TypeOfErrors.InvalidRamSettings] = "Invalid RAM Setting";
                }
            }
            catch (Exception) { }

            UpdateFaultList();

            //Validation
            if (IsServerPathValid && IsRAMValueValid) {
                StatusLED.Fill = new SolidColorBrush(Colors.Lime);
                StartStopServer.IsEnabled = true;
            }
            else
            {
                StartStopServer.IsEnabled = false;
            }

            //Update Preferences File
            SavePreferences(false);
        }

        //Starting and Stopping Server
        private void StartServer()
        {
            ServerArgs = new ProcessStartInfo("java", " -Xmx" + ServerRamBox.Value + "M -jar \"" + ServerPath + "\" nogui")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory
            };

            ServerProcess.StartInfo = ServerArgs;
            ServerProcess.EnableRaisingEvents = true;
            ServerProcess.OutputDataReceived += new DataReceivedEventHandler(ServerOutput_OutputDataRecieved);
            ServerProcess.Exited += new EventHandler(ServerClose_Exited);
            ServerProcess.Start();
            ServerProcess.BeginOutputReadLine();

            ServerIsRunning = true;

            OpAll.IsEnabled = true;
            DeOpAll.IsEnabled = true;
            KickAll.IsEnabled = true;
            ExecuteCommand.IsEnabled = true;

            ServerUpTime.Start();
            UpdateResourceUsage.Start();
            UpdateServerUptime.Start();

            StartStopServer.Background = new SolidColorBrush(Color.FromRgb(172, 29, 29));
            StartStopServer.Content = "Stop Server";
            StatusText.Content = "Server is Running";
            InfoServerPID.Text = ServerProcess.Id.ToString();
            StatusLED.Fill = new SolidColorBrush(Colors.Lime);
        }

        private void StopServer()
        {
            StartStopServer.IsEnabled = false;
            OpAll.IsEnabled = false;
            DeOpAll.IsEnabled = false;
            KickAll.IsEnabled = false;
            ExecuteCommand.IsEnabled = false;
        }

        //Recieving Server Output
        private void ServerOutput_OutputDataRecieved(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => {
                try
                {
                    if (e.Data.Contains("WARN"))
                    {
                        ConsoleOutput.AppendText(e.Data + "\n");
                        TextRange WarnOutputTextRange = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
                        WarnOutputTextRange.Text = e.Data;
                        WarnOutputTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(WarningOutputColor));
                    }
                    else if (e.Data.Contains("ERROR"))
                    {
                        ConsoleOutput.AppendText(e.Data + "\n");
                        TextRange ErrorOutputTextRange = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
                        ErrorOutputTextRange.Text = e.Data;
                        ErrorOutputTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(ErrorOutputColor));
                    }
                    else if (e.Data.Contains("logged in with") || e.Data.Contains("left the game"))
                    {
                        ConsoleOutput.AppendText(e.Data + "\n");
                        TextRange PlayEventTextRange = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
                        PlayEventTextRange.Text = e.Data;
                        PlayEventTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(PlayerEventOutputColor));
                    }
                    else if (e.Data.Contains("Done") && e.Data.Contains("For help, type \"help\""))
                    {
                        ConsoleOutput.AppendText(e.Data + "\n");
                        TextRange PlayEventTextRange = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
                        PlayEventTextRange.Text = e.Data;
                        PlayEventTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(ServerDoneLoadingColor));
                    }
                    else
                    {
                        ConsoleOutput.AppendText(e.Data + "\n");
                        TextRange DefaultOutputTextRange = new TextRange(ConsoleOutput.Document.ContentEnd, ConsoleOutput.Document.ContentEnd);
                        DefaultOutputTextRange.Text = e.Data;
                        DefaultOutputTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(DefaultOutputColor));
                    }
                }
                catch (Exception) { }

                ConsoleOutput.ScrollToEnd();
            });
        }

        //When Server is Closed
        private void ServerClose_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => {
                ServerUpTime.Stop();
                UpdateResourceUsage.Stop();
                UpdateServerUptime.Stop();

                StartStopServer.Content = "Start Server";
                StatusText.Content = "Server Closed";
                StartStopServer.Background = new SolidColorBrush(Color.FromRgb(124, 25, 160));

                ServerProcess.CancelOutputRead();

                StatusLED.Fill = new SolidColorBrush(Colors.Orange);

                StartStopServer.IsEnabled = true;

                InfoServerPID.Text = "Server ProcessID: Server not running";

                ServerIsRunning = false;
                ServerProcess.OutputDataReceived -= new DataReceivedEventHandler(ServerOutput_OutputDataRecieved);
                ServerProcess.Exited -= new EventHandler(ServerClose_Exited);

                InfoRamUsage.Text = "0";
                RAMUsageIndicator.Content = "";
            });
        }

        //------------------------------------------Side Menu------------------------------------------
        private void OpenCloseMenu_Checked(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/Left Arrow.png")));
            OpenCloseMenu.Content = "Menu";
            WindowGrid.ColumnDefinitions[0].Width = new GridLength(300);
            ShowConsole.Content = "Console";
            ShowPlayers.Content = "Players";
            ShowWorldsModsPlugins.Content = "Worlds/Plugins/Mods";
            ShowInfo.Content = "Server Information";
            ShowDownload.Content = "New Server";
            ShowGitHub.Content = "GitHub";
            ShowSettings.Content = "Settings";
        }

        private void OpenCloseMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Assets/Menu Button.png")));
            OpenCloseMenu.Content = "";
            WindowGrid.ColumnDefinitions[0].Width = new GridLength(64);
            ShowConsole.Content = "";
            ShowPlayers.Content = "";
            ShowWorldsModsPlugins.Content = "";
            ShowInfo.Content = "";
            ShowDownload.Content = "";
            ShowGitHub.Content = "";
            ShowSettings.Content = "";
        }

        //------------------------------------------Swap Between Panels------------------------------------------
        private void ShowConsole_Click(object sender, RoutedEventArgs e)
        {
            ClosePanel();
            LastPanel = "Console";
            ConsolePanel.Visibility = Visibility.Visible;
        }

        private void ShowWorldsModsPlugins_Click(object sender, RoutedEventArgs e)
        {
            ClosePanel();
            LastPanel = "WorldsModsPlugins";
            WorldsModsPluginsPanel.Visibility = Visibility.Visible;
        }

        private void ShowInfo_Click(object sender, RoutedEventArgs e)
        {
            ClosePanel();
            LastPanel = "Server Info";
            ServerInfoPanel.Visibility = Visibility.Visible;
            ShowInfoNotifBadge.Visibility = Visibility.Collapsed;
        }

        private void ShowDownload_Click(object sender, RoutedEventArgs e)
        {
            ClosePanel();
            LastPanel = "NewServer";
            NewServerPanel.Visibility = Visibility.Visible;
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            ClosePanel();
            LastPanel = "Settings";
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    ClosePanel();
                    LastPanel = "Console";
                    ConsolePanel.Visibility = Visibility.Visible;
                    break;
                case Key.F2:
                    break;
                case Key.F3:
                    ClosePanel();
                    LastPanel = "WorldsModsPlugins";
                    WorldsModsPluginsPanel.Visibility = Visibility.Visible;
                    break;
                case Key.F4:
                    ClosePanel();
                    LastPanel = "Server Info";
                    ServerInfoPanel.Visibility = Visibility.Visible;
                    ShowInfoNotifBadge.Visibility = Visibility.Collapsed;
                    break;
                case Key.F5:
                    ClosePanel();
                    LastPanel = "NewServer";
                    NewServerPanel.Visibility = Visibility.Visible;
                    break;
                case Key.F12:
                    ClosePanel();
                    LastPanel = "Settings";
                    SettingsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        //------------------------------------------Console Menu------------------------------------------
        private void StartStopServer_Click(object sender, RoutedEventArgs e)
        {
            if (!ServerIsRunning)
            {
                StartServer();
            }
            else
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    ServerProcess.Kill();
                    StopServer();
                }
                else
                {
                    ServerProcess.StandardInput.WriteLine("stop");
                    StopServer();
                }
            }
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e) => ConsoleOutput.Document.Blocks.Clear();

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private async void BackupWorlds_Click(object sender, RoutedEventArgs e)
        {
            ConsoleOutput.AppendText("\n[Server Wrapper] Starting Backup");

            //Check backups directory exists
            if (!Directory.Exists(WorkingDirectory + @"\Backups")) { Directory.CreateDirectory(WorkingDirectory + @"\Backups"); }

            //Create Folder to hold backups of current time
            string DestinationFolder = WorkingDirectory + "\\Backups\\" + DateTime.Now.ToString("dd-mm-yyyy-_-HH-mm-ss");
            Directory.CreateDirectory(DestinationFolder);
            
            //Find all world folders and copy
            await Task.Run(() => {
                foreach (var item in Directory.EnumerateDirectories(WorkingDirectory.ToString()))
                {
                    if (File.Exists(item + "\\level.dat"))
                    {
                        string[] WorldName = item.Split('\\');
                        Directory.CreateDirectory(DestinationFolder + "\\" + WorldName[WorldName.Length - 1]);
                        Dispatcher.Invoke(() => {
                            ConsoleOutput.AppendText("\n[Server Wrapper] Backing up world \"" + WorldName[WorldName.Length - 1] + "\"");

                            try { DirectoryCopy(item, DestinationFolder + "\\" + WorldName[WorldName.Length - 1], true); }
                            catch (Exception f) { ConsoleOutput.AppendText(f.ToString()); }

                            ConsoleOutput.AppendText("\n[Server Wrapper] Finished backing up world \"" + WorldName[WorldName.Length - 1] + "\"");
                        });
                    }
                }
            });

            ConsoleOutput.AppendText("\n[Server Wrapper] Backuping Worlds Complete\n");
        }

        private void OpenServerFolder_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", WorkingDirectory);

        private void AccentColorPicker_MouseDown(object sender, MouseButtonEventArgs e) => AccentColorChangePreview.Background = new SolidColorBrush(AccentColorPicker.Value);

        private void AccentColorSetting_MouseUp(object sender, MouseButtonEventArgs e) => AccentColorChangePreview.Background = new SolidColorBrush(AccentColorPicker.Value);

        private void CommandBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CommandBox.Text == "")
            {
                TypeCommandsHereLabel.Visibility = Visibility.Visible;
                ClearCommandBox.Visibility = Visibility.Hidden;
            }

            if (CommandBox.Text != "")
            {
                TypeCommandsHereLabel.Visibility = Visibility.Hidden;
                ClearCommandBox.Visibility = Visibility.Visible;
            }
        }

        private void CommandBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { ExecuteCommands(CommandBox, e); }
        }

        private void ExecuteCommands(object sender, RoutedEventArgs e)
        {
            if (ServerIsRunning)
            {
                string[] CommandArray = CommandBox.Text.Split(';');
                foreach (var Command in CommandArray) { ServerProcess.StandardInput.WriteLine(Command.Trim()); }
                CommandBox.Text = "";
            }
        }

        private void InfoRamUsageUnit_SelectionChanged(object sender, SelectionChangedEventArgs e) => GetRamUsage();

        private void ClearCommandBox_Click(object sender, RoutedEventArgs e) => CommandBox.Text = "";

        //------------------------------------------Server Info Menu------------------------------------------
        private void InfoServerPathShowInExplorer_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", "/select, \"" + ServerPath);

        private void InfoWorkingDirectoryShowInExplorer_Click(object sender, RoutedEventArgs e) => Process.Start("explorer.exe", WorkingDirectory);

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => GetTotalPhysicalMemory();

        private void CopyPublicIP_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(PublicIP);

        BitmapImage Eye = new BitmapImage(new Uri("Assets\\Eye.png", UriKind.Relative));
        BitmapImage EyeLineThrough = new BitmapImage(new Uri("Assets\\Eye Line Through.png", UriKind.Relative));

        private void ShowHidePublicIP_Click(object sender, RoutedEventArgs e)
        {
            if (InfoPublicIP.Opacity == 0) { InfoPublicIP.Opacity = 1; } else { InfoPublicIP.Opacity = 0; }
            //if (ShowHidePublicIPIcon.Source == Eye) { ShowHidePublicIPIcon.Source = EyeLineThrough; } else { ShowHidePublicIPIcon.Source = Eye; }
        }

        private void CopyLocalIP_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(LocalIP);

        private void ShowHideLocalIP_Click(object sender, RoutedEventArgs e)
        {
            if (InfoLocalIP.Opacity == 0) { InfoLocalIP.Opacity = 1; } else { InfoLocalIP.Opacity = 0; }
            //if (ShowHideLocalIPIcon.Source == Eye) { ShowHideLocalIPIcon.Source = EyeLineThrough; } else { ShowHideLocalIPIcon.Source = Eye; }
        }

        //------------------------------------------Download Server------------------------------------------
        private void ServerVersionSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ServerVersionSearchBox.Text == "")
            {
                SearchServerVersion.Visibility = Visibility.Visible;
                ClearServerVersionSearchBox.Visibility = Visibility.Hidden;
                foreach (ListBoxItem item in ServerVersionList.Items) { item.Visibility = Visibility.Visible; }
            }

            if (ServerVersionSearchBox.Text != "")
            {
                SearchServerVersion.Visibility = Visibility.Hidden;
                ClearServerVersionSearchBox.Visibility = Visibility.Visible;
                foreach (ListBoxItem item in ServerVersionList.Items) { item.Visibility = Visibility.Collapsed; }
                foreach (ListBoxItem item in ServerVersionList.Items)
                {
                    if (item.Content.ToString().Contains(ServerVersionSearchBox.Text))
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }
            }

            CreateServerCheck();
        }

        private void InstallPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InstallPathBox.Text != "") { ClearInstallPathBox.Visibility = Visibility.Visible; }
            else { ClearInstallPathBox.Visibility = Visibility.Hidden; }

            if (Directory.Exists(InstallPathBox.Text)) { InstallPathBox.Foreground = new SolidColorBrush(Colors.White); }
            else { InstallPathBox.Foreground = new SolidColorBrush(Colors.Red); }

            CreateServerCheck();
        }

        private void CreateServerCheck()
        {
            if (Directory.Exists(InstallPathBox.Text) && ServerVersionList.SelectedItem != null)
            {
                CreateServer.IsEnabled = true;
            }
            else
            {
                CreateServer.IsEnabled = false;
            }
        }

        string BuildToolsDirectory = Path.GetTempPath() + "BuildTools";
        string BuildToolsFileLocation = Path.GetTempPath() + "BuildTools\\BuildTools.jar";
        string CreateServerVersion = "";

        private void ServerVersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { CreateServerVersion = ServerVersionList.Items.CurrentItem.ToString(); } catch(Exception) { }
        }

        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            CreateServer.IsEnabled = false;
            BrowseInstallPath.IsEnabled = false;
            ServerTypeSelect.IsEnabled = false;
            ServerVersionList.IsEnabled = false;

            NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Version: " + CreateServerVersion + "\n");
            NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Install Path: " + InstallPathBox.Text + "\n");

            if (!File.Exists(BuildToolsFileLocation))
            {
                Directory.CreateDirectory(BuildToolsDirectory);
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] BuildTools not found, downloading BuildTools..." + "\n");
                NewServerConsoleOutput.AppendText("Progress: 0%\n");

                try
                {
                    webClient.DownloadFileAsync(new Uri("https://hub.spigotmc.org/jenkins/job/BuildTools/lastSuccessfulBuild/artifact/target/BuildTools.jar"), BuildToolsFileLocation);
                }
                catch (Exception f)
                {
                    NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] " + f.ToString());
                    CreateServer.IsEnabled = true;
                    BrowseInstallPath.IsEnabled = true;
                    ServerTypeSelect.IsEnabled = true;
                    ServerVersionList.IsEnabled = true;
                }
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                if (e.ProgressPercentage % 10 == 0) { NewServerConsoleOutput.AppendText("Progress: " + e.ProgressPercentage + "%\n"); }
            });
        }

        private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Dispatcher.Invoke(() => {
                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] BuildTools download Complete\n");
                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Creating server jar file... \n");

                Process BuildToolsProcess = new Process();
                string args = "";

                if (ServerTypeSelect.SelectedIndex == 0)
                {
                    args = BuildToolsFileLocation + " --rev" + CreateServerVersion + " --craftbukkit";
                }

                if (ServerTypeSelect.SelectedIndex == 1)
                {
                    args = BuildToolsFileLocation + " --rev" + CreateServerVersion;
                }

                ProcessStartInfo BuildToolsArgs = new ProcessStartInfo("java", args)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = BuildToolsDirectory
                };

                BuildToolsProcess.StartInfo = BuildToolsArgs;
                BuildToolsProcess.EnableRaisingEvents = true;
                BuildToolsProcess.OutputDataReceived += BuildToolsProcess_OutputDataReceived;
                BuildToolsProcess.Exited += BuildToolsProcess_Exited;
                BuildToolsProcess.Start();
            });
        }

        private void BuildToolsProcess_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => {
                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Finished creating server jar file" + "\n");
                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Copying server jar" + "\n");
                if (ServerTypeSelect.SelectedIndex == 0)
                {
                    File.Copy(BuildToolsDirectory + "craftbukkit-" + CreateServerVersion, InstallPathBox.Text + "craftbukkit-" + CreateServerVersion);
                }

                if (ServerTypeSelect.SelectedIndex == 1)
                {
                    File.Copy(BuildToolsDirectory + "spigot-" + CreateServerVersion, InstallPathBox.Text + "spigot-" + CreateServerVersion);
                }

                NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] Server Created!" + "\n");

                Process.Start("explorer.exe", InstallPathBox.Text);

                CreateServer.IsEnabled = true;
                BrowseInstallPath.IsEnabled = true;
                ServerTypeSelect.IsEnabled = true;
                ServerVersionList.IsEnabled = true;
            });
        }

        private void BuildToolsProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => { NewServerConsoleOutput.AppendText("[" + DateTime.Now + "] " + e.Data + "\n"); });
        }
        
        private void ClearServerVersionSearchBox_Click(object sender, RoutedEventArgs e) => ServerVersionSearchBox.Text = "";

        private void BrowseInstallPath_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog BrowseInstallDialog = new WinForms.FolderBrowserDialog();

            if (BrowseInstallDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                InstallPathBox.Text = BrowseInstallDialog.SelectedPath;
            }
        }

        //------------------------------------------Settings Menu------------------------------------------
        private void ServerPathBox_TextChanged(object sender, TextChangedEventArgs e) => IsServerReady("ServerPath");

        private void LocateServerJar_Click(object sender, RoutedEventArgs e)
        {
            WinForms.OpenFileDialog LocateServerFile = new WinForms.OpenFileDialog();
            LocateServerFile.Filter = ".jar|*.jar";
            if (LocateServerFile.ShowDialog() == WinForms.DialogResult.OK) { ServerPathBox.Text = LocateServerFile.FileName; }
        }
    }
}
