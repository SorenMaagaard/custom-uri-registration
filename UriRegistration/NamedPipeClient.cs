using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;

namespace UriRegistration
{
    public sealed class NamedPipeClient : IDisposable
    {
        public string PipeName { get; }
        private readonly NamedPipeClientStream _client;

        public NamedPipeClient(string pipeName)
        {
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _client = new NamedPipeClientStream(Environment.MachineName, PipeName,
                PipeDirection.Out,
                PipeOptions.WriteThrough, TokenImpersonationLevel.Anonymous, HandleInheritability.None);
        }

        /// <summary> Send the arguments to the app server </summary>
        public void SendMessage(string args)
        {
            if (string.IsNullOrEmpty(args))
                throw new ArgumentException("Value cannot be null or empty.", nameof(args));

            const int timeout = 2000;

            try
            {
                if (!_client.IsConnected)
                    _client.Connect(timeout);
            }
            catch (TimeoutException ex)
            {
                Trace.Write(ex);
            }

            using var writer = new StreamWriter(_client, leaveOpen: true);
            writer.WriteLine(args.ToCharArray());
            writer.Flush();
        }

        public void Dispose()
        {
            try { _client?.Dispose(); }
            catch
            {
                // ignored
            }
        }
    }
}
