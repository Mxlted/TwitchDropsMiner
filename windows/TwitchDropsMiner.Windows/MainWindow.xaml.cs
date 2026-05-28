using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TwitchDropsMiner.Windows.Core;

namespace TwitchDropsMiner.Windows;

public partial class MainWindow : Window
{
    private const int MaxLogCharacters = 120_000;

    private readonly MinerProcessRunner _runner;
    private readonly MinerStatusClient _statusClient = new();
    private readonly DispatcherTimer _statusTimer;
    private readonly WindowsServerOptions _serverOptions = new();
    private bool _isClosing;
    private bool _isRefreshing;
    private bool _isStarting;
    private bool _isStopping;

    public MainWindow(string repositoryRoot)
    {
        InitializeComponent();

        _runner = new MinerProcessRunner(repositoryRoot);
        _runner.OutputReceived += Runner_OutputReceived;
        _runner.Exited += Runner_Exited;

        RepositoryPathTextBox.Text = repositoryRoot;
        ServerUrlTextBox.Text = _serverOptions.ServerUri.ToString();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();

        AppendLog("Ready. Start the miner, then open the local web UI.");
        UpdateControls();
        _ = RefreshStatusAsync();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        await StartMinerAsync();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWebUi();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        await StopMinerAsync();
    }

    private async void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        await StopMinerAsync();
        await StartMinerAsync();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        if (_runner.IsRunning)
        {
            e.Cancel = true;
            IsEnabled = false;
            AppendLog("Window is closing; stopping the miner first.");
            await StopMinerAsync();
            _statusTimer.Stop();
            _statusClient.Dispose();
            _runner.Dispose();
            Close();
            return;
        }

        _statusTimer.Stop();
        _statusClient.Dispose();
        _runner.Dispose();
    }

    private async Task StartMinerAsync()
    {
        if (_isStarting || _runner.IsRunning)
        {
            return;
        }

        _isStarting = true;
        try
        {
            UpdateControls();
            LaunchResult result = await _runner.StartAsync();
            PythonCommandTextBlock.Text = result.CommandLine;
            AppendLog(result.Started
                ? $"Started miner process {result.ProcessId}."
                : $"Miner process {result.ProcessId} is already running.");
        }
        catch (Exception ex)
        {
            AppendLog($"Start failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Twitch Drops Miner", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isStarting = false;
            await RefreshStatusAsync();
            UpdateControls();
        }
    }

    private async Task StopMinerAsync()
    {
        if (!_runner.IsRunning)
        {
            return;
        }

        _isStopping = true;
        UpdateControls();
        AppendLog("Stopping miner...");

        try
        {
            await _runner.StopAsync(
                token => _statusClient.RequestShutdownAsync(_serverOptions, token),
                TimeSpan.FromSeconds(10)
            );
            AppendLog("Miner stopped.");
        }
        catch (Exception ex)
        {
            AppendLog($"Stop failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Twitch Drops Miner", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isStopping = false;
            await RefreshStatusAsync();
            UpdateControls();
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            MinerServerStatus status = await _statusClient.GetStatusAsync(_serverOptions);

            ServerStatusTextBlock.Text = status.Status;
            LoginStatusTextBlock.Text = status.LoginStatus;
            ProcessStatusTextBlock.Text = _runner.IsRunning ? "Running" : "Stopped";

            if (_runner.IsRunning && status.IsOnline)
            {
                SetStatusPill("Running", "AccentBrush");
            }
            else if (_runner.IsRunning || _isStarting)
            {
                SetStatusPill("Starting", "WarningBrush");
            }
            else if (status.IsOnline)
            {
                SetStatusPill("External server", "WarningBrush");
            }
            else
            {
                SetStatusPill("Stopped", "MutedTextBrush");
            }

            UpdateControls();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void OpenWebUi()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _serverOptions.ServerUri.ToString(),
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Could not open browser: {ex.Message}");
            MessageBox.Show(ex.Message, "Twitch Drops Miner", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Runner_OutputReceived(object? sender, ProcessOutputEventArgs e)
    {
        Dispatcher.Invoke(() => AppendLog(e.Line));
    }

    private void Runner_Exited(object? sender, ProcessExitedEventArgs e)
    {
        Dispatcher.Invoke(async () =>
        {
            AppendLog(e.ExitCode is null
                ? "Miner process exited."
                : $"Miner process exited with code {e.ExitCode}.");
            await RefreshStatusAsync();
            UpdateControls();
        });
    }

    private void UpdateControls()
    {
        bool running = _runner.IsRunning;
        StartButton.IsEnabled = !running && !_isStarting && !_isStopping;
        StopButton.IsEnabled = running && !_isStopping;
        RestartButton.IsEnabled = running && !_isStopping;
        OpenButton.IsEnabled = true;
    }

    private void SetStatusPill(string text, string brushKey)
    {
        StatusPillTextBlock.Text = text;
        if (TryFindResource(brushKey) is Brush brush)
        {
            StatusDotEllipse.Fill = brush;
            StatusPillBorder.BorderBrush = brush;
        }
    }

    private void AppendLog(string line)
    {
        if (LogTextBox.Text.Length > MaxLogCharacters)
        {
            int keepFrom = Math.Max(0, LogTextBox.Text.Length - (MaxLogCharacters / 2));
            LogTextBox.Text = LogTextBox.Text[keepFrom..];
        }

        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }
}
