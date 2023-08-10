using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Websocket.Tcp.Server.Listener.Options;

namespace Websocket.Tcp.Server.Listener
{
    public abstract class TcpConnectionListener : IDisposable
    {
        private const int StartedState = 1;
        private const int StoppedState = 2;

        private readonly TcpListener _listener;
        private readonly ListenerOptions _options;
        private CancellationTokenSource? _listenerCts;
        private List<Task>? _listenTasks;
        private int _state = 0;
        private bool _disposed = false;

        protected ListenerOptions Options => _options;
        public bool IsStarted
        {
            get
            {
                ThrowIfDisposed();
                return _state == StartedState;
            }
        }

        public TcpConnectionListener(IPEndPoint ipEndPoint, ListenerOptions? options = null)
            : this(new TcpListener(ipEndPoint), options)
        {
        }
        public TcpConnectionListener(IPAddress ipAddress, int port, ListenerOptions? options = null)
            : this(new IPEndPoint(ipAddress, port), options)
        {
        }
        public TcpConnectionListener(TcpListener tcpListener, ListenerOptions? options = null)
        {
            _listener = tcpListener;
            _options = options ?? new ListenerOptions();
        }

        public void Init()
        {
            _listener.Start();
        }

        public void Start(CancellationToken cancellationToken)
        {
            StartListen(cancellationToken).ConfigureAwait(false);
        }

        public void Start(int backLog, CancellationToken cancellationToken)
        {
            StartListen(backLog, cancellationToken).ConfigureAwait(false);
        }

        public void Stop()
        {
            StopListen();
        }

        private void StopListen()
        {
            if (Interlocked.CompareExchange(ref _state, StoppedState, StartedState) == StoppedState)
                throw new InvalidOperationException("Listener not started.");

            _listener.Stop();
            _listenerCts?.Cancel();
            _listenerCts?.Dispose();
            _listenerCts = null;
        }

        private Task StartListen(CancellationToken cancellationToken)
        {
            return StartListen(int.MaxValue, cancellationToken);
        }

        private Task StartListen(int backLog, CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _state, StartedState, StoppedState) == StartedState)
                throw new InvalidOperationException("Listener is already started.");

            _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener.Start(backLog);
            _listenTasks = new List<Task>(_options.CountListeners);
            for (var i = 0; i < _options.CountListeners; i++)
            {
                _listenTasks.Add(Listen(_listener, cancellationToken));
            }

            return Task.WhenAll(_listenTasks);
        }

        protected ValueTask<TcpClient> AcceptTcpClientAsync(CancellationToken cancellationToken)
        {
            return _listener.AcceptTcpClientAsync(cancellationToken);
        }

        private async Task Listen(TcpListener listener, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    var tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
                    await AcceptTcpClientAsyncCore(tcpClient, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    throw;
                }
            }
        }

        private async Task AcceptTcpClientAsyncCore(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            try
            {
                await OnTcpClientAcceptedAsync(tcpClient, cancellationToken);
            }
            catch (Exception ex)
            {
                OnTcpClientError(tcpClient, ex);
            }
        }

        protected abstract Task OnTcpClientAcceptedAsync(TcpClient tcpClient, CancellationToken cancellationToken);

        protected virtual void OnTcpClientError(TcpClient tcpClient, Exception exception)
        {
            tcpClient.Close();
            throw exception;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;

                if (IsStarted)
                    StopListen();

                _listenTasks?.Clear();
                _listenTasks = null;
            }
        }
    }
}