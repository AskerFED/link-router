using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Manages single-instance application behavior using Mutex for detection
    /// and named pipe for argument forwarding between instances.
    /// </summary>
    public sealed class SingleInstanceManager : IDisposable
    {
        #region Constants

        private const string MutexName = "BrowserSelector_SingleInstance_Mutex";
        private const string PipeName = "BrowserSelector_SingleInstance_Pipe";
        private const int PipeTimeout = 5000; // ms

        #endregion

        #region Singleton

        private static SingleInstanceManager? _instance;
        private static readonly object _lock = new object();

        public static SingleInstanceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SingleInstanceManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private Mutex? _mutex;
        private CancellationTokenSource? _cts;
        private Task? _pipeServerTask;
        private bool _isFirstInstance;
        private bool _disposed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when arguments are received from another instance.
        /// Always fired on the UI thread.
        /// </summary>
        public event EventHandler<string[]>? ArgumentsReceived;

        #endregion

        #region Properties

        public bool IsFirstInstance => _isFirstInstance;

        #endregion

        #region Constructor

        private SingleInstanceManager() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to acquire single-instance lock.
        /// Call this at the very start of Application_Startup.
        /// </summary>
        /// <returns>True if this is the first instance, false if another exists.</returns>
        public bool TryAcquireLock()
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                Logger.Log("SingleInstanceManager: Acquired mutex - this is the first instance");
            }
            else
            {
                Logger.Log("SingleInstanceManager: Another instance is running");
            }

            return _isFirstInstance;
        }

        /// <summary>
        /// Starts the named pipe server to receive arguments from other instances.
        /// Must be called after the Application object exists (in Application_Startup).
        /// </summary>
        public void StartPipeServer()
        {
            if (!_isFirstInstance) return;

            _cts = new CancellationTokenSource();
            _pipeServerTask = Task.Run(() => RunPipeServerAsync(_cts.Token));

            Logger.Log("SingleInstanceManager: Pipe server started");
        }

        /// <summary>
        /// Sends arguments to the existing instance via named pipe.
        /// </summary>
        public static bool SendArgumentsToExistingInstance(string[] args)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.Out);

                pipeClient.Connect(PipeTimeout);

                using var writer = new StreamWriter(pipeClient);
                writer.AutoFlush = true;

                // Join args with null character (safe delimiter)
                var message = string.Join("\0", args);
                writer.WriteLine(message);

                Logger.Log($"SingleInstanceManager: Sent args to existing instance: {message}");
                return true;
            }
            catch (TimeoutException)
            {
                Logger.Log("SingleInstanceManager: Timeout connecting to existing instance");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"SingleInstanceManager: Error sending args: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private async Task RunPipeServerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Create pipe with security settings that allow current user access
                    var pipeSecurity = new PipeSecurity();
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        WindowsIdentity.GetCurrent().User!,
                        PipeAccessRights.FullControl,
                        System.Security.AccessControl.AccessControlType.Allow));

                    using var pipeServer = NamedPipeServerStreamAcl.Create(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        0,  // inBufferSize
                        0,  // outBufferSize
                        pipeSecurity);

                    await pipeServer.WaitForConnectionAsync(ct);

                    using var reader = new StreamReader(pipeServer);
                    var message = await reader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(message))
                    {
                        var args = message.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                        Logger.Log($"SingleInstanceManager: Received args: {string.Join(", ", args)}");

                        // Dispatch to UI thread
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ArgumentsReceived?.Invoke(this, args);
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Log("SingleInstanceManager: Pipe server cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"SingleInstanceManager: Pipe server error: {ex.Message}");
                    // Continue listening unless cancelled
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _cts?.Cancel();
            _cts?.Dispose();

            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch
                {
                    // Mutex may not be owned by this thread
                }
                _mutex.Dispose();
            }

            _disposed = true;
            Logger.Log("SingleInstanceManager: Disposed");
        }

        #endregion
    }
}
