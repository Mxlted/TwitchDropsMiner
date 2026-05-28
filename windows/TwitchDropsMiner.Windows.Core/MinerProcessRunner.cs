using System.ComponentModel;
using System.Diagnostics;

namespace TwitchDropsMiner.Windows.Core;

public sealed class MinerProcessRunner : IDisposable
{
    private static readonly string[] MinerArguments = ["-u", "main.py"];

    private readonly object _gate = new();
    private readonly PythonResolver _pythonResolver;
    private readonly string _repositoryRoot;
    private Process? _process;

    public MinerProcessRunner(string repositoryRoot, PythonResolver? pythonResolver = null)
    {
        _repositoryRoot = repositoryRoot;
        _pythonResolver = pythonResolver ?? new PythonResolver();
    }

    public event EventHandler<ProcessOutputEventArgs>? OutputReceived;

    public event EventHandler<ProcessExitedEventArgs>? Exited;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _process is { HasExited: false };
            }
        }
    }

    public async Task<LaunchResult> StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_process is { HasExited: false } runningProcess)
            {
                return LaunchResult.AlreadyRunning(runningProcess.Id);
            }
        }

        PythonCommand command = _pythonResolver.Resolve(_repositoryRoot);
        ProcessStartInfo startInfo = CreateStartInfo(command);
        Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, args) => PublishOutput(args.Data);
        process.ErrorDataReceived += (_, args) => PublishOutput(args.Data);
        process.Exited += (_, _) => HandleExit(process);

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Windows could not start the miner process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        lock (_gate)
        {
            _process = process;
        }

        await Task.CompletedTask;
        return LaunchResult.StartedProcess(process.Id, command.Format(MinerArguments));
    }

    public async Task StopAsync(
        Func<CancellationToken, Task>? requestGracefulShutdown,
        TimeSpan gracefulTimeout,
        CancellationToken cancellationToken = default
    )
    {
        Process? process;
        lock (_gate)
        {
            process = _process;
        }

        if (process is null || process.HasExited)
        {
            return;
        }

        if (requestGracefulShutdown is not null)
        {
            try
            {
                using CancellationTokenSource shutdownCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                shutdownCts.CancelAfter(TimeSpan.FromSeconds(3));
                await requestGracefulShutdown(shutdownCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                PublishOutput("Graceful shutdown request timed out.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                PublishOutput($"Graceful shutdown request failed: {ex.Message}");
            }
        }

        try
        {
            using CancellationTokenSource waitCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            waitCts.CancelAfter(gracefulTimeout);
            await process.WaitForExitAsync(waitCts.Token);
            return;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            PublishOutput("Miner did not exit in time; stopping the local process.");
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        Process? process;
        lock (_gate)
        {
            process = _process;
            _process = null;
        }

        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // The process can exit or become unavailable between the HasExited check and Kill.
        }
        finally
        {
            process.Dispose();
        }
    }

    private ProcessStartInfo CreateStartInfo(PythonCommand command)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = command.FileName,
            WorkingDirectory = _repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.Environment["PYTHONUNBUFFERED"] = "1";
        command.AddArguments(startInfo, MinerArguments);
        return startInfo;
    }

    private void HandleExit(Process process)
    {
        int? exitCode = null;
        try
        {
            exitCode = process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            // Process state can race with the Exited event on shutdown.
        }

        lock (_gate)
        {
            if (ReferenceEquals(_process, process))
            {
                _process = null;
            }
        }

        Exited?.Invoke(this, new ProcessExitedEventArgs(exitCode));
        process.Dispose();
    }

    private void PublishOutput(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            OutputReceived?.Invoke(this, new ProcessOutputEventArgs(line));
        }
    }
}
