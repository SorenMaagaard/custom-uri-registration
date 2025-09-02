using System.IO;
using System.IO.Pipes;

namespace UriRegistration
{
    public sealed class NamedPipeServer 
    {
        public string PipeName { get; }
        private readonly List<NamedPipeServerStream> _streams;
        private CancellationTokenSource? _cancellationSource;

        /// <summary> Event handler </summary>
        public event EventHandler<string>? ReceivedMessage;

        /// <summary> Constructor </summary>
        public NamedPipeServer(string pipeName)
        {
            PipeName = pipeName;

            _streams = new List<NamedPipeServerStream>();
            InitServerPipe();
        }

        /// <summary> Dispose </summary>
        public void Dispose()
        {
            _cancellationSource?.Cancel();
            _cancellationSource = null;

            lock (_streams)
            {
                foreach (var namedPipeServerStream in _streams)
                {
                    try
                    {
                        namedPipeServerStream.Close();
                    }
                    catch (Exception ex) { Console.WriteLine(ex); }
                }
            }
        }

        private void InitServerPipe()
        {
            _cancellationSource = new CancellationTokenSource();
            var token = _cancellationSource.Token;
            Task.Run(() => ListenLoopAsync(token), token);
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    lock (_streams)
                    {
                        _streams.Add(server);
                    }

                    try
                    {
                        await server.WaitForConnectionAsync(token);
                    }
                    catch (OperationCanceledException)
                    {
                        try { await server.DisposeAsync(); } catch { }
                        break;
                    }
                    catch
                    {
                        try { await server.DisposeAsync(); } catch { }
                        lock (_streams) { _streams.Remove(server); }
                        if (token.IsCancellationRequested) break;
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var reader = new StreamReader(server, leaveOpen: true);
                            var line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                            if (line != null)
                            {
                                OnReceivedMessage(line);
                            }
                        }
                        catch
                        {
                            // ignore per-connection errors
                        }
                        finally
                        {
                            try { if (server.IsConnected) server.Disconnect(); } catch { }
                            try { server.Dispose(); } catch { }
                            lock (_streams) { _streams.Remove(server); }
                        }
                    }, token);
                }
            }
            finally
            {
                // Cleanup any remaining streams on exit
                lock (_streams)
                {
                    foreach (var s in _streams.ToArray())
                    {
                        try { s.Dispose(); } catch { }
                    }
                    _streams.Clear();
                }
            }
        }

        /// <summary> Event invocator </summary>
        private void OnReceivedMessage(string e)
        {
            ReceivedMessage?.Invoke(this, e);
        }
    }
}
