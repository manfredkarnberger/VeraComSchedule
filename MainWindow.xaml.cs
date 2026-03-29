using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using PcanSqliteSender.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using VeraCom.Models;
using VeraComTool.Properties;

namespace VeraCom
{
    public partial class MainWindow : Window
    {
        private PcanService _pcanService = new();

        public ObservableCollection<CanMessage> Messages { get; set; } = new();
        public ObservableCollection<CanMessage> ReceivedMessages { get; set; } = new();

        private Dictionary<uint, CanMessage> _receivedMap = new();

        private DatabaseService _dbService = new DatabaseService();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _pcanService.MessageSent += OnMessageSent;
            _pcanService.MessageReceived += OnMessageReceived;

            LoadLastDatabase();
        }

        private void LoadLastDatabase()
        {
            string lastPath = Settings.Default.LastDatabasePath;

            if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
            {
                try
                {
                    var loadedMessages = _dbService.LoadMessages(lastPath);

                    Messages.Clear();
                    foreach (var msg in loadedMessages)
                        Messages.Add(msg);

                    TxtDatabasePath.Text = lastPath;

                    BtnStart.IsEnabled = true;
                    BtnStop.IsEnabled = true;
                }
                catch
                {
                    // ignorieren, falls DB ungültig
                }
            }
        }

        private void OnMessageSent(CanMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                msg.TxFrameCounter++;
            });
        }

        private void OnMessageReceived(CanMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (_receivedMap.ContainsKey(msg.CanID))
                {
                    var existing = _receivedMap[msg.CanID];

                    var now = DateTime.Now;

                    if (existing.LastTimestamp != default)
                    {
                        existing.RxCycleTime =
                            (int)Math.Round((now - existing.LastTimestamp).TotalMilliseconds);
                    }

                    existing.LastTimestamp = now;

                    existing.Payload = msg.Payload;
                    existing.DLC = msg.DLC;
                    existing.Timestamp = now;

                    existing.Refresh();
                }
                else
                {
                    msg.Timestamp = DateTime.Now;
                    msg.LastTimestamp = msg.Timestamp;
                    msg.RxCycleTime = 0;

                    _receivedMap[msg.CanID] = msg;
                    ReceivedMessages.Add(msg);

                    var sorted = ReceivedMessages.OrderBy(m => m.CanID).ToList();
                    ReceivedMessages.Clear();
                    foreach (var m in sorted)
                        ReceivedMessages.Add(m);
                }
            });
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;

            await _pcanService.StartAsync(Messages);
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;

            await _pcanService.StopAsync();
        }

        private void Beenden_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DateiOeffnen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "SQLite-Datenbank öffnen",
                Filter = "SQLite Dateien (*.db;*.sqlite)|*.db;*.sqlite|Alle Dateien (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string selectedPath = ofd.FileName;

                if (!File.Exists(selectedPath))
                {
                    MessageBox.Show("Datei existiert nicht.");
                    return;
                }

                try
                {
                    var loadedMessages = _dbService.LoadMessages(selectedPath);

                    Messages.Clear();
                    foreach (var msg in loadedMessages)
                        Messages.Add(msg);

                    TxtDatabasePath.Text = selectedPath;

                    BtnStart.IsEnabled = true;
                    BtnStop.IsEnabled = true;

                    // 🔥 HIER speichern
                    Settings.Default.LastDatabasePath = selectedPath;
                    Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Laden: " + ex.Message);
                }
            }
        }
    }
}
