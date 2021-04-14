using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TDSProtocol;

namespace TDSProxy
{
    class TDSConnection : IDisposable
    {
        #region Log4Net

        static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        // TODO: log (local and)? inside EP(s)

        private static readonly bool VerboseLogging = TDSProxyService.VerboseLogging;

        private const uint MaxTdsVersion = 0x74000004;
        private const ushort MinimumPacketLimit = 512;

        internal static int TotalConnections;
        internal static int ActiveConnectionCount;
        internal static int UnclosedCollections;

        readonly TDSProxyService _service;
        readonly TDSListener _listener;
        readonly TcpClient _outsideClient;
        readonly NetworkStream _outsideStream;
        readonly TdsSslHandshakeAdapter _outsideAdapter;
        readonly SslStream _outsideSSL;
        readonly IPEndPoint _outsideEP;
        readonly TcpClient _insideClient;
        readonly NetworkStream _insideStream;
        readonly IPEndPoint _insideEP;

        // ReSharper disable once NotAccessedField.Local -- needed to prevent premature collection
        readonly Task _processingTask;

        TDSPreLoginMessage.EncryptionEnum _encryptionSettingForClient;
        ushort _spid;
        ushort _packetLength = 4096;
        uint _clientTdsVersion;
        uint _serverSoftwareVersion;

        enum StateEnum
        {
            PreLogin,
            SslHandshake,
            Login,
            Connected,
            Closed
        }

        StateEnum _state = StateEnum.PreLogin;

        #region SSL Handshake adapter

        class TdsSslHandshakeAdapter : Stream
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass -- don't want to accidentally use outer class's value
            private static readonly bool VerboseLogging = TDSProxyService.VerboseLoggingInWrapper;

            private class TaskAsyncResult : IAsyncResult
            {
                public TaskAsyncResult(Task task, object state)
                {
                    Task = task;
                    AsyncState = state;
                }

                public object AsyncState { get; }

                public WaitHandle AsyncWaitHandle => ((IAsyncResult)Task).AsyncWaitHandle;

                public bool CompletedSynchronously => ((IAsyncResult)Task).CompletedSynchronously;

                public bool IsCompleted => Task.IsCompleted;

                public Task Task { get; }
            }

            readonly TDSConnection _connection;

            byte[] _wrapperBytes;
            int _wrapperOffset;

            public TdsSslHandshakeAdapter(TDSConnection connection)
            {
                _connection = connection;
            }

            public override IAsyncResult BeginRead(byte[] buffer,
                                                   int offset,
                                                   int count,
                                                   AsyncCallback callback,
                                                   object state)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper BeginRead called");
                var task = ReadAsync(buffer, offset, count);
                var result = new TaskAsyncResult(task, state);
                if (null != callback)
                    task.ContinueWith(t => callback(result));

                return result;
            }

            public override IAsyncResult BeginWrite(byte[] buffer,
                                                    int offset,
                                                    int count,
                                                    AsyncCallback callback,
                                                    object state)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper BeginWrite called");
                var task = WriteAsync(buffer, offset, count);
                var result = new TaskAsyncResult(task, state);
                if (null != callback)
                    task.ContinueWith(t => callback(result));

                return result;
            }

            public override bool CanRead => _connection._outsideStream.CanRead;

            public override bool CanSeek => false;

            public override bool CanTimeout => _connection._outsideStream.CanTimeout;

            public override bool CanWrite => _connection._outsideStream.CanWrite;

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                        _connection._outsideStream.Close();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper EndRead called");
                if (null == asyncResult)
                    throw new ArgumentNullException(nameof(asyncResult));
                var t = asyncResult as TaskAsyncResult;
                return t?.Task is Task<int> taskInt
                           ? taskInt.Result
                           : throw new ArgumentException("Not an IAsyncResult for a read operation",
                                                         nameof(asyncResult));
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper EndWrite called");
                if (null == asyncResult)
                    throw new ArgumentNullException(nameof(asyncResult));
                var t = asyncResult as TaskAsyncResult ??
                        throw new ArgumentException("Not an IAsyncResult for a write operation", nameof(asyncResult));
                t.Task.Wait();
            }

            public override void Flush()
            {
                _connection._outsideStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _connection._outsideStream.FlushAsync(cancellationToken);
            }

            public override long Length => throw new NotSupportedException("Seek is not supported");

            public override long Position
            {
                get => throw new NotSupportedException("Seek is not supported");
                set => throw new NotSupportedException("Seek is not supported");
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper Read called");
                return ReadAsync(buffer, offset, count).Result;
            }

            public override async Task<int> ReadAsync(byte[] buffer,
                                                      int offset,
                                                      int count,
                                                      CancellationToken cancellationToken)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper ReadAsync called");
                if (_connection._state == StateEnum.SslHandshake)
                {
                    // Did we have payload from a previous wrapper left over?
                    if (null != _wrapperBytes)
                    {
                        // Sure did, return saved data
                        var savedBytes = _wrapperBytes;
                        var savedOffset = _wrapperOffset;
                        int returnCount;
                        if (count + savedOffset < savedBytes.Length)
                        {
                            // Will not exhaust our stash of bytes, bump the offset for the next read
                            _wrapperOffset += count;
                            returnCount = count;
                        }
                        else
                        {
                            // Exhausted stashed bytes - update state to reflect
                            _wrapperBytes = null;
                            _wrapperOffset = 0;
                            returnCount = savedBytes.Length - savedOffset;
                        }

                        Buffer.BlockCopy(savedBytes, savedOffset, buffer, offset, returnCount);

                        if (VerboseLogging)
                            log.DebugFormat("Returning {0} bytes from buffer (caller requested {1}), outsideEP = {2}",
                                            returnCount,
                                            count,
                                            _connection._outsideEP);
                        return returnCount;
                    }
                    else
                    {
                        // No saved bytes, read a new wrapper (although peek at it to make sure it's a wrapper, if not just pass through)
                        byte[] peekBuf = new byte[1];

                        if (VerboseLogging)
                            log.DebugFormat("Peeking for a TDS-wrapped SSL packet from {0}", _connection._outsideEP);
                        var byteCount = await _connection
                                              ._outsideStream.ReadAsync(peekBuf, 0, 1, cancellationToken)
                                              .ConfigureAwait(false);
                        if (VerboseLogging)
                            log.DebugFormat("Peek got {0} bytes from {1}", byteCount, _connection._outsideEP);
                        if (byteCount == 0)
                        {
                            return 0;
                        }

                        if (TDSMessageType.PreLogin == (TDSMessageType)peekBuf[0] ||
                            TDSMessageType.TabularResult == (TDSMessageType)peekBuf[0])
                        {
                            if (VerboseLogging)
                                log.DebugFormat("Reading TDS-wrapped SSL packet from {0}", _connection._outsideEP);
                            var packets = await TDSPacket
                                                .ReadAsync(TDSMessageType.PreLogin,
                                                           _connection._outsideStream,
                                                           cancellationToken)
                                                .ConfigureAwait(false);
                            var wrapper = (TDSPreLoginMessage)TDSMessage.FromPackets(packets);
                            var payload = wrapper.SslPayload;
                            if (VerboseLogging)
                                log.DebugFormat("Got {0} bytes of SSL payload from {1}",
                                                payload.Length,
                                                _connection._outsideEP);
                            int unwrappedCount;
                            if (payload.Length > count)
                            {
                                _wrapperBytes = payload;
                                _wrapperOffset = count;
                                unwrappedCount = count;
                            }
                            else
                            {
                                unwrappedCount = payload.Length;
                            }

                            Buffer.BlockCopy(payload, 0, buffer, offset, count);
                            if (VerboseLogging)
                                log.DebugFormat(
                                    "Returning {0} bytes from unwrapped data of {1} bytes (caller requested {2}), outsideEP = {3}",
                                    unwrappedCount,
                                    payload.Length,
                                    count,
                                    _connection._outsideEP);
                            return unwrappedCount;
                        }
                        else
                        {
                            buffer[offset] = peekBuf[0];
                            if (count == 1)
                            {
                                if (VerboseLogging)
                                    log.DebugFormat(
                                        "Returning 1 byte after peek showed non-TDS packet (caller requested 1), outsideEP = {0}",
                                        _connection._outsideEP);
                                return 1;
                            }

                            byteCount = await _connection
                                              ._outsideStream
                                              .ReadAsync(buffer, offset + 1, count - 1, cancellationToken)
                                              .ConfigureAwait(false);
                            if (VerboseLogging)
                                log.DebugFormat(
                                    "Returning {0} bytes from network after peek showed non-TDS packet (caller requested {1}), outsideEP = {2}",
                                    byteCount + 1,
                                    count,
                                    _connection._outsideEP);
                            return byteCount + 1;
                        }
                    }
                }

                // We're not in a state where wrapping is appropriate
                if (null != _wrapperBytes)
                {
                    log.WarnFormat("Discarding {0} of {1} bytes from unconsumed wrapper from {2}",
                                   _wrapperBytes.Length,
                                   _wrapperBytes.Length - _wrapperOffset,
                                   _connection._outsideEP);
                    _wrapperBytes = null;
                    _wrapperOffset = 0;
                }

                var underlyingBytes =
                    await _connection._outsideStream.ReadAsync(buffer, offset, count, cancellationToken)
                                     .ConfigureAwait(false);
                if (VerboseLogging)
                    log.DebugFormat("Returning {0} bytes (requested {1}) from {2} in non-wrapped mode",
                                    underlyingBytes,
                                    count,
                                    _connection._outsideEP);
                return underlyingBytes;
            }

            public override int ReadTimeout
            {
                get => _connection._outsideStream.ReadTimeout;
                set => _connection._outsideStream.ReadTimeout = value;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException("Seek is not supported");
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("Seek is not supported");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper Write called");
                WriteAsync(buffer, offset, count).Wait();
            }

            public override async Task WriteAsync(byte[] buffer,
                                                  int offset,
                                                  int count,
                                                  CancellationToken cancellationToken)
            {
                if (VerboseLogging)
                    log.Debug("Wrapper WriteAsync called");
                if (_connection._state == StateEnum.SslHandshake)
                {
                    byte[] sslBuffer = new byte[count];
                    Buffer.BlockCopy(buffer, offset, sslBuffer, 0, count);
                    var msg = new TDSPreLoginMessage { SslPayload = sslBuffer };
                    await msg
                          .WriteAsPacketsAsync(
                              _connection._outsideStream,
                              _connection._packetLength,
                              _connection._spid,
                              overrideMessageType: TDSMessageType.TabularResult,
                              cancellationToken: cancellationToken)
                          .ConfigureAwait(false);
                }
                else
                    await _connection._outsideStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override int WriteTimeout
            {
                get => _connection._outsideStream.WriteTimeout;
                set => _connection._outsideStream.WriteTimeout = value;
            }
        }

        #endregion

        public TDSConnection(TDSProxyService service,
                             TDSListener listener,
                             TcpClient outsideClient,
                             IPEndPoint insideEndPoint)
        {
            Interlocked.Increment(ref TotalConnections);
            Interlocked.Increment(ref ActiveConnectionCount);

            _service = service;
            service.Stopping +=
                service_Stopping; // Beneficially, this keeps this instance alive too. We unbind when we close the connection.

            _listener = listener;

            // Ensure Nagle algorithm is used. We're SSL-offloading which changes packet size, so the remote ends' assumptions
            // about optimum packet size won't apply to us. We will, however, flush the SSL stream at the ends of message units
            // forward from inside to outside, because it seems ODBC and OLEDB clients assume their SSL layer will deliver them
            // complete packets. Gross!
            outsideClient.NoDelay = false;

            _outsideEP = (IPEndPoint)outsideClient.Client.RemoteEndPoint;
            _outsideClient = outsideClient;
            _outsideStream = outsideClient.GetStream();
            _outsideAdapter = new TdsSslHandshakeAdapter(this);
            _outsideSSL = new SslStream(_outsideAdapter);

            _insideEP = insideEndPoint;
            _insideClient = new TcpClient(_insideEP.AddressFamily) { NoDelay = false };
            _insideClient.Connect(insideEndPoint);
            _insideStream = _insideClient.GetStream();

            _processingTask = ProcessConnection();
        }

        ~TDSConnection()
        {
            if (_state != StateEnum.Closed)
            {
                Interlocked.Increment(ref UnclosedCollections);
                Interlocked.Decrement(ref ActiveConnectionCount);
            }
        }

        private void service_Stopping(object sender, EventArgs e)
        {
            if (VerboseLogging)
                log.Debug("Closing connection to {0} due to service shutdown");
            Close();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        void IDisposable.Dispose()
        {
            if (_state != StateEnum.Closed)
            {
                //Log this at Info level so we can see disconnects
                log.InfoFormat("Closing connection from {0} that was forwarding to {1}", _outsideEP, _insideEP);
                Interlocked.Decrement(ref ActiveConnectionCount);
                _state = StateEnum.Closed;
                _service.Stopping -=
                    service_Stopping; // NOTE: VERY IMPORTANT, DO NOT REMOVE - this prevents memory leaks
                try
                {
                    _insideStream.Close();
                }
                catch (Exception e)
                {
                    log.Error($"Error closing inside stream for connection from {_outsideEP}", e);
                }

                try
                {
                    _outsideSSL.Close();
                }
                catch (Exception e)
                {
                    log.Error($"Error closing outbound SSL stream for connection from {_outsideEP}", e);
                }

                try
                {
                    _outsideAdapter.Close();
                }
                catch (Exception e)
                {
                    log.Error($"Error closing SSL adapter for connection from {_outsideAdapter}", e);
                }
            }
        }

        private async Task ProcessConnection()
        {
            try
            {
                var preLoginFromClient = await ReadPreLoginFromClient().ConfigureAwait(false);
                if (null == preLoginFromClient)
                    return;
                log.DebugFormat("Received PreLogin message from {0}, will forward to {1}", _outsideEP, _insideEP);

                await ProcessAndForwardPreLogin(preLoginFromClient).ConfigureAwait(false);

                var preLoginResponse = await ReadPreLoginResponseFromServer().ConfigureAwait(false);
                if (null == preLoginResponse)
                {
                    log.WarnFormat("Bad response from prelogin for {0} from {1}.\r\n{2}\r\n{3}",
                                   _outsideEP,
                                   _insideEP,
                                   preLoginFromClient.DumpReceivedPayload(),
                                   preLoginFromClient.DumpPayload());
                    return;
                }

                log.DebugFormat("Received PreLogin response for {0} from {1}", _outsideEP, _insideEP);

                await ProcessAndForwardPreLoginResponse(preLoginResponse).ConfigureAwait(false);

                log.DebugFormat("Starting SSL handshake with {0}", _outsideEP);
                _state = StateEnum.SslHandshake;

                try
                {
                    await _outsideSSL.AuthenticateAsServerAsync(_listener.Certificate).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    log.Error("Failed to complete SSL handshake with " + _outsideEP, e);
                    return;
                }

                if (!TDSProxyService.SkipLoginProcessing)
                {
                    _state = StateEnum.Login;

                    log.DebugFormat(
                        "Established {0} session using {1}({2})-{3}({4}) with {5}",
                        _outsideSSL.SslProtocol,
                        _outsideSSL.HashAlgorithm,
                        _outsideSSL.HashStrength,
                        _outsideSSL.CipherAlgorithm,
                        _outsideSSL.CipherStrength,
                        _outsideEP);

                    var login7 = await ReadLogin7FromClient().ConfigureAwait(false);
                    if (null == login7)
                        return;
                    log.DebugFormat("Received Login7 message from {0} with user '{1}' and database '{2}'",
                                    _outsideEP,
                                    login7.UserName,
                                    login7.Database);

                    if (string.Equals(login7.UserName, "sa", StringComparison.OrdinalIgnoreCase))
                    {
                        log.InfoFormat("Quick exit on 'sa' attempt. Login denied for remote client {0}.", _outsideEP);
                        return;
                    }

                    var authResult =
                        _listener.Authenticator.Authenticate(_outsideEP.Address,
                                                             login7.UserName,
                                                             login7.Password,
                                                             login7.Database);
                    bool authOk = null != authResult && authResult.AllowConnection;
                    if (!authOk)
                    {
                        log.InfoFormat("Authentication failed for user '{0}', database '{1}' for remote client {2}",
                                       login7.UserName,
                                       login7.Database,
                                       _outsideEP);
                        await SendLogin7DeniedResponse("Username or password incorrect.").ConfigureAwait(false);
                        return;
                    }

                    log.InfoFormat(
                        "Authentication successful for user '{0}', database '{1}' for remote client {2}; connecting to {3}, db '{4}' as user '{5}'",
                        login7.UserName,
                        login7.Database,
                        _outsideEP,
                        _insideEP,
                        authResult.ConnectToDatabase,
                        authResult.ConnectAsUser);

                    await ProcessAndForwardLogin7(login7, authResult).ConfigureAwait(false);
                    if (!await ReadAndProcessLogin7Response().ConfigureAwait(false))
                        return;
                }
                else if (_encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off)
                {
                    // Encryption "Off" still means client will send Login7 message SSL-encrypted,
                    // so we have to decrypt that before dumb-proxying the non-SSL outside stream
                    var login7 = await ReadLogin7FromClient().ConfigureAwait(false);
                    if (null == login7)
                        return;

                    log.DebugFormat("Received SSL-encrypted Login7 message from {0}, forwarding unencrypted to {1}",
                                    _outsideEP,
                                    _insideEP);

                    await login7.WriteAsPacketsAsync(_insideStream, _packetLength, _spid);
                }

                log.DebugFormat("Connection with {0} forwarding to {1} without further interpretation",
                                _outsideEP,
                                _insideEP);

                await Connected().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error($"Error processing connection for client {_outsideEP}, server {_insideEP}", e);
            }
            finally
            {
                Close();
            }
        }

        private async Task<TDSPreLoginMessage> ReadPreLoginFromClient()
        {
            //Let's give them 10 seconds to send their info.
            var cts = new CancellationTokenSource(10_000);

            try
            {

                //The underlying read on stream doesn't support cancellation so don't bother
                //	with sending the cancellation token. This is ugly as hell, but it will
                //	cause the read to fail.
                using (cts.Token.Register(() => _outsideStream.Close()))
                {
                    var packetsFromClient = await TDSPacket.ReadAsync(_outsideStream);
                    var packetList = packetsFromClient as List<TDSPacket> ?? packetsFromClient.ToList();
                    _spid = packetList[0].SPID;
                    var message = TDSMessage.FromPackets(packetList, null, TDSMessageType.PreLogin);
                    var preLogin = message as TDSPreLoginMessage;
                    if (null == preLogin)
                    {
                        log.DebugFormat("Client {0} sent a {1} message when expecting a PreLogin message",
                            _outsideEP,
                            message.MessageType);
                        return null;
                    }

                    if (!preLogin.Encryption.HasValue)
                    {
                        log.DebugFormat("Client {0} sent a PreLogin message without the Encryption setting",
                            _outsideEP);
                        return null;
                    }

                    if (preLogin.Encryption == TDSPreLoginMessage.EncryptionEnum.NotSupported)
                    {
                        log.DebugFormat(
                            "Client {0} does not support encryption; responding that encryption is required prior to dropping connection",
                            _outsideEP);
                        preLogin.Encryption = TDSPreLoginMessage.EncryptionEnum.Required;
                        await preLogin.WriteAsPacketsAsync(_outsideStream, _packetLength, _spid).ConfigureAwait(false);
                        return null;
                    }

                    return preLogin;
                }
            }
            catch (EndOfStreamException)
            {
                log.DebugFormat("Client {0} closed the connection before sending any data", _outsideEP);
            }
            catch (IOException)
            {
                log.DebugFormat(
                    "IOException reading initial packets from {0}, this is typical for scanners, ignore it.",
                    _outsideEP);
            }
            catch (TDSInvalidPacketException ipe)
            {
                log.Debug($"Client {_outsideEP} sent invalid TDS data", ipe);
            }
            catch (TDSInvalidMessageException ime)
            {
                log.Debug($"Client {_outsideEP} sent invalid TDS message within valid TDS packets", ime);
            }
            catch (ObjectDisposedException) when (_state == StateEnum.Closed || cts.IsCancellationRequested)
            {
                if (cts.IsCancellationRequested)
                    log.DebugFormat("Client {0} never told us anything. Is he mute?", _outsideEP);

                // Normal, ignore it.
            }
            catch (ProtocolViolationException ex)
            {
                log.DebugFormat("Received a protocol violation processing {0}. Message {1}", _outsideEP, ex.Message);
            }
            catch (Exception e)
            {
                //Most "errors" are logged as Debug since we don't care about them from a 
                //	long term view. But we should have most caught above, so log what's left
                //	as Error so we can see it easier.
                log.Error($"Error reading PreLogin from client {_outsideEP}", e);
            }

            return null;
        }

        private async Task ProcessAndForwardPreLogin(TDSPreLoginMessage preLoginMessage)
        {
            // We always want to talk SSL to the client (outside)
            if (TDSProxyService.AllowUnencryptedConnections ||
                preLoginMessage.Encryption == TDSPreLoginMessage.EncryptionEnum.On)
                _encryptionSettingForClient = preLoginMessage.Encryption ?? TDSPreLoginMessage.EncryptionEnum.On;
            else
                _encryptionSettingForClient = TDSPreLoginMessage.EncryptionEnum.Required;
            // We never want to talk SSL to the server (inside)
            preLoginMessage.Encryption = TDSPreLoginMessage.EncryptionEnum.NotSupported;
            log.DebugFormat("Forwarding PreLogin request from {0} to {1}", _outsideEP, _insideEP);
            await preLoginMessage.WriteAsPacketsAsync(_insideStream, _packetLength, _spid).ConfigureAwait(false);
        }

        private async Task<TDSPreLoginMessage> ReadPreLoginResponseFromServer()
        {
            try
            {
                var packetsFromServer = await TDSPacket.ReadAsync(_insideStream).ConfigureAwait(false);
                var packetList = packetsFromServer as List<TDSPacket> ?? packetsFromServer.ToList();
                var firstPacket = packetList[0];
                _spid = firstPacket.SPID;
                var firstPacketType = firstPacket.PacketType;
                if (firstPacketType != TDSMessageType.TabularResult)
                {
                    log.ErrorFormat("Server {0} responded with a {1} message when expecting a PreLogin response",
                                    _insideEP,
                                    firstPacketType);
                    return null;
                }

                var preLoginResponse =
                    (TDSPreLoginMessage)TDSMessage.FromPackets(packetList, TDSMessageType.PreLogin);
                if (!preLoginResponse.Version.HasValue)
                {
                    log.ErrorFormat(
                        "Server {0}'s PreLogin response lacked required VersionInfo element; dropping connection to {1}",
                        _insideEP,
                        _outsideEP);
                    return null;
                }

                _serverSoftwareVersion = preLoginResponse.Version.GetValueOrDefault().Version;
                if (preLoginResponse.Encryption == TDSPreLoginMessage.EncryptionEnum.Required)
                {
                    log.ErrorFormat("Server {0} requires encryption; dropping connection to {1}",
                                    _insideEP,
                                    _outsideEP);
                    return null;
                }

                return preLoginResponse;
            }
            catch (EndOfStreamException)
            {
                log.InfoFormat("Server {0} closed the connection before sending any data back for {1}",
                               _insideEP,
                               _outsideEP);
            }
            catch (TDSInvalidPacketException ipe)
            {
                log.Error($"Server {_insideEP} sent invalid TDS data for {_outsideEP}", ipe);
            }
            catch (TDSInvalidMessageException ime)
            {
                log.Error($"Server {_insideEP} sent invalid TDS message within valid TDS packets for {_outsideEP}",
                          ime);
            }
            catch (Exception e)
            {
                log.Error($"Error reading PreLogin response from server {_insideEP} for client {_outsideEP}", e);
            }

            return null;
        }

        private async Task ProcessAndForwardPreLoginResponse(TDSPreLoginMessage preLoginResponse)
        {
            // Set encryption flag to the appropriate value to turn encryption on given the flag in the initial PreLogin request
            preLoginResponse.Encryption = _encryptionSettingForClient;
            log.DebugFormat("Forwarding PreLogin response from {0} to {1} with Encryption = {2}",
                            _insideEP,
                            _outsideEP,
                            preLoginResponse.Encryption);
            await preLoginResponse.WriteAsPacketsAsync(_outsideStream,
                                                       _packetLength,
                                                       _spid,
                                                       overrideMessageType: TDSMessageType.TabularResult);
        }

        private async Task<TDSLogin7Message> ReadLogin7FromClient()
        {
            try
            {
                var cts = new CancellationTokenSource(30_000);
                var packetsFromClient = await TDSPacket.ReadAsync(_outsideSSL, cts.Token);
                var packetList = packetsFromClient as List<TDSPacket> ?? packetsFromClient.ToList();
                _spid = packetList[0].SPID;
                var message = TDSMessage.FromPackets(packetList);
                if (!(message is TDSLogin7Message login7))
                {
                    log.ErrorFormat("Client {0} sent a {1} message when expecting a Login7 message",
                                    _outsideEP,
                                    message.MessageType);
                    return null;
                }

                _packetLength = (ushort)Math.Min(ushort.MaxValue, Math.Max(MinimumPacketLimit, login7.PacketSize));
                _clientTdsVersion = login7.TdsVersion;
                if (!string.IsNullOrEmpty(login7.AttachDBFile) ||
                    (_clientTdsVersion >= 0x72000000 &&
                     (login7.OptionFlags3 &
                      TDSLogin7Message.OptionFlags3Enum
                                      .UserInstance) !=
                     0))
                {
                    log.InfoFormat("Client {0} requested a user instance; denying login & dropping connection",
                                   _outsideEP);
                    await SendLogin7DeniedResponse("User instances not permitted.").ConfigureAwait(false);
                    return null;
                }

                if ((login7.OptionFlags2 & TDSLogin7Message.OptionFlags2Enum.IntegratedSecurity) != 0)
                {
                    if (TDSProxyService.BypassIntegratedSecurity)
                    {
                        login7.OptionFlags2 = login7.OptionFlags2 ^ TDSLogin7Message.OptionFlags2Enum.IntegratedSecurity;
                        return login7;
                    }
                    else
                    {
                        log.InfoFormat("Client {0} requested integrated security; denying login & dropping connection", _outsideEP);
                        await SendLogin7DeniedResponse("Integrated Security not supported.").ConfigureAwait(false);
                        return null;
                    }
                }

                if (null != login7.SSPI && 0 != login7.SSPI.Length)
                {
                    if (TDSProxyService.BypassSSPI)
                    {
                        login7.SSPI = null;
                        return login7;
                    }
                    else
                    {
                        log.InfoFormat("Client {0} requested SSPI; denying login & dropping connection", _outsideEP);
                        await SendLogin7DeniedResponse("SSPI is not supported.").ConfigureAwait(false);
                        return null;
                    }
                }

                if (null != login7.FeatureExt &&
                    login7.FeatureExt.Any(fe => fe.FeatureId == TDSLogin7Message.FeatureId.FedAuth))
                {
                    log.InfoFormat("Client {0} requested federated authentication; denying login & dropping connection",
                                   _outsideEP);
                    await SendLogin7DeniedResponse("Federated authentication is not supported.").ConfigureAwait(false);
                    return null;
                }

                return login7;
            }
            catch (EndOfStreamException)
            {
                log.InfoFormat("Client {0} closed the connection before sending a Login7 message", _outsideEP);
            }
            catch (TDSInvalidPacketException ipe)
            {
                log.Error($"Client {_outsideEP} sent invalid TDS data", ipe);
            }
            catch (TDSInvalidMessageException ime)
            {
                log.Error($"Client {_outsideEP} sent invalid TDS message within valid TDS packets", ime);
            }
            catch (TaskCanceledException tce)
            {
                log.Error($"Timed out reading Login7 from client {_outsideEP}", tce);
            }
            catch (Exception e)
            {
                log.Error($"Error reading Login7 from client {_outsideEP}", e);
            }

            return null;
        }

        private async Task SendLogin7DeniedResponse(string errorMessage)
        {
            uint presumedServerTdsVersion;
            if (_serverSoftwareVersion >= 0x0B000000) // 2012 (11.0.yyyy)
                presumedServerTdsVersion = 0x74000004;
            else if (_serverSoftwareVersion >= 0x0A320000) // 2008 R2 (10.50.yyyy)
                presumedServerTdsVersion = 0x730B0003;
            else if (_serverSoftwareVersion >= 0x0A000000) // 2008 (10.x.yyyy)
                presumedServerTdsVersion = 0x730A0003;
            else if (_serverSoftwareVersion >= 0x09000000) // 2005 (9.x.yyyy)
                presumedServerTdsVersion = 0x72090002;
            else if (_serverSoftwareVersion >= 0x08000180) // 2000 SP1 (8.0.384)
                presumedServerTdsVersion = 0x71000001;
            else if (_serverSoftwareVersion >= 0x08000000) // 2000 (8.0.yyy)
                presumedServerTdsVersion = 0x07010000;
            else // 7.0
                presumedServerTdsVersion = 0x07000000;

            // NOTE: Cannot use await from here down to after the call to msg.BuildMessage() because TDSToken is stored in a ThreadStatic and await may cause thread hopping
            TDSToken.TdsVersion = presumedServerTdsVersion;
            var msg = new TDSTabularDataMessage();
            msg.AddTokens(new TDSErrorToken(msg)
            {
                Number = 50000,
                State = 1,
                Class = 14,
                MsgText = "Login failed. " + errorMessage
            },
                          new TDSDoneToken(msg)
                          {
                              Status = TDSDoneToken.StatusEnum.Final | TDSDoneToken.StatusEnum.Error
                          });
            msg.BuildMessage();

            await msg.WriteAsPacketsAsync(_encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off
                                              ? (Stream)_outsideStream
                                              : _outsideSSL,
                                          _packetLength,
                                          _spid)
                     .ConfigureAwait(false);
        }

        private async Task ProcessAndForwardLogin7(TDSLogin7Message login7,
                                                   Authentication.AuthenticationResult authResult)
        {
            if (login7.TdsVersion >= MaxTdsVersion)
                login7.TdsVersion = MaxTdsVersion;
            string displayUserAt = (authResult.DisplayUsername ?? login7.UserName) + "@";
            string ipAddress = _outsideEP.Address.ToString();
            string host = login7.HostName ?? "";
            if (displayUserAt.Length + host.Length + ipAddress.Length + ("" == host ? 0 : 3) <= 128)
                // Can fit "username@hostname (ip address)" or if no hostname, "username@ip address"
                login7.HostName = displayUserAt + ("" == host ? ipAddress : host + " (" + ipAddress + ")");
            else if (displayUserAt.Length + ipAddress.Length > 128)
                // ReSharper disable once CommentTypo
                // Can't even fit "username@ip address", show "usern...@ip address"
                login7.HostName = displayUserAt.Substring(124 - ipAddress.Length) + "...@" + ipAddress;
            else if (displayUserAt.Length + ipAddress.Length + Math.Min(3, host.Length) > 124)
                // Can't fit the shorter of either "username@hostname (ip address)" or "username@... (ip address)", show "username@ip address"
                login7.HostName = displayUserAt + ipAddress;
            else
                // Show username@hos... (ip address)"
                login7.HostName = displayUserAt +
                                  host.Substring(122 - (displayUserAt.Length + host.Length + ipAddress.Length));
            login7.UserName = authResult.ConnectAsUser;
            login7.Password = authResult.ConnectUsingPassword;
            login7.Database = authResult.ConnectToDatabase;
            await login7.WriteAsPacketsAsync(_insideStream, _packetLength, _spid).ConfigureAwait(false);
        }

        private async Task<bool> ReadAndProcessLogin7Response()
        {
            // do/while(false) is a hack to allow break statements to jump out of a block
            // Lets us avoid nesting ifs
            do
            {
                TDSTabularDataMessage loginResponse = null;

                try
                {
                    var packetsFromServer = await TDSPacket.ReadAsync(_insideStream).ConfigureAwait(false);
                    var packetList = packetsFromServer as List<TDSPacket> ?? packetsFromServer.ToList();

                    var firstPacket = packetList[0];
                    var firstPacketType = firstPacket.PacketType;
                    if (firstPacketType != TDSMessageType.TabularResult)
                    {
                        log.ErrorFormat("Server {0} responded with a {1} message when expecting a Login response",
                                        _insideEP,
                                        firstPacketType);
                        break;
                    }

                    loginResponse = (TDSTabularDataMessage)TDSMessage.FromPackets(packetList);
                    var tokenList = loginResponse.Tokens as List<TDSToken> ?? loginResponse.Tokens.ToList();
                    if (tokenList.All(t => t.TokenId != TDSTokenType.LoginAck))
                    {
                        // Server denied login. Log the error/info message(s) but forward only a generic login denied message to the client.
                        var messageTokens = tokenList.OfType<TDSMessageToken>().ToList();
                        log.ErrorFormat("SQL Server denied login with the following error messages:\r\n\t{0}",
                                        messageTokens.Count == 0 ? "(none)" : string.Join("\r\n\t", messageTokens));
                        break;
                    }

                    // Login was accepted, forward response to client
                    await loginResponse
                          .WriteAsPacketsAsync(_encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off
                                                   ? (Stream)_outsideStream
                                                   : _outsideSSL,
                                               _packetLength,
                                               _spid)
                          .ConfigureAwait(false);
                    return true;
                }
                catch (EndOfStreamException)
                {
                    log.InfoFormat("Server {0} closed the connection before responding to LOGIN7 for {1}{2}",
                                   _insideEP,
                                   _outsideEP);
                }
                catch (TDSInvalidPacketException ipe)
                {
                    log.Error($"Server {_insideEP} sent invalid TDS data for {_outsideEP}", ipe);
                }
                catch (TDSInvalidMessageException ime)
                {
                    var payload = loginResponse?.DumpReceivedPayload("    ");
                    payload = string.IsNullOrEmpty(payload) ? "<no payload>" : "\r\nMessage received:\r\n" + payload;
                    log.Error($"Server {_insideEP} sent invalid TDS message within valid TDS packets for {_outsideEP}{payload}",
                              ime);
                }
                catch (Exception e)
                {
                    log.Error($"Error reading Login response from server {_insideEP} for client {_outsideEP}", e);
                }
            } while (false);

            await SendLogin7DeniedResponse("Login denied");
            return false;
        }

        private async Task Connected()
        {
            _state = StateEnum.Connected;

            await Task.WhenAll(ForwardOutsideToInside(), ForwardInsideToOutside());
        }

        private async Task ForwardOutsideToInside()
        {
            try
            {
                var outsideStream = (_encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off
                                         ? (Stream)_outsideStream
                                         : _outsideSSL);
                var outsideStreamName = _encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off
                                            ? "_outsideStream"
                                            : "_outsideSSL";
                var packetTypeBuffer = new byte[1];
                while (true)
                {
                    var bytesRead = await outsideStream.ReadAsync(packetTypeBuffer, 0, 1).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        if (TDSPacket.IsTDSPacketType(packetTypeBuffer[0]))
                        {
                            var tdsType = (TDSMessageType)packetTypeBuffer[0];
                            var packet = await TDSPacket.ReadSinglePacketAsync(tdsType, outsideStream, false);
                            if (VerboseLogging)
                                log.DebugFormat("Read TDS {0} packet of {1} bytes from {2} on {3}, forwarding to {4}",
                                                tdsType,
                                                packet.Length,
                                                _outsideEP,
                                                outsideStreamName,
                                                _insideEP);
                            await packet.WriteToStreamAsync(_insideStream);
                        }
                        else if (SMPPacket.IsSMPPacketType(packetTypeBuffer[0]))
                        {
                            var smpType = (SmpPacketType)packetTypeBuffer[0];
                            var packet = await SMPPacket.ReadFromStreamAsync(outsideStream, false, smpType);
                            if (VerboseLogging)
                                log.DebugFormat(
                                    "Read SMP (MARS) {0} packet of {1} bytes from {2} on {3}, forwarding to {4}",
                                    packet.Flags,
                                    packet.Length,
                                    _outsideEP,
                                    outsideStreamName,
                                    _insideEP);
                            await packet.WriteToStreamAsync(_insideStream);
                        }
                        else
                        {
                            log.ErrorFormat(
                                "Unexpected message type {0:X2} received from {1} on {2} - killing connection.",
                                packetTypeBuffer[0],
                                _outsideEP,
                                outsideStreamName);
                            Close();
                            return;
                        }
                    }
                    else
                        break;
                }

                log.DebugFormat("Closing _insideClient for Send to {0} and _outsideClient for Receive from {1}",
                                _insideEP,
                                _outsideEP);
                _insideClient.Client.Shutdown(SocketShutdown.Send);
                _outsideClient.Client.Shutdown(SocketShutdown.Receive);
            }
            catch (ObjectDisposedException)
            {
                // Swallow ObjectDisposedExceptions if (and only if) we've shut down already
                if (_state != StateEnum.Closed)
                    throw;
            }
        }

        private async Task ForwardInsideToOutside()
        {
            try
            {
                var outsideStream = (_encryptionSettingForClient == TDSPreLoginMessage.EncryptionEnum.Off
                                         ? (Stream)_outsideStream
                                         : _outsideSSL);
                bool flushAfterWrite = _encryptionSettingForClient != TDSPreLoginMessage.EncryptionEnum.Off;
                var packetTypeBuffer = new byte[1];
                Task flushTask = null;
                while (true)
                {
                    var bytesRead = await _insideStream.ReadAsync(packetTypeBuffer, 0, 1).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        if (TDSPacket.IsTDSPacketType(packetTypeBuffer[0]))
                        {
                            var tdsType = (TDSMessageType)packetTypeBuffer[0];
                            var packet = await TDSPacket.ReadSinglePacketAsync(tdsType, _insideStream, false);
                            if (VerboseLogging)
                                log.DebugFormat("Read TDS {0} packet of {1} bytes from {2} for {3}",
                                                tdsType,
                                                packet.Length,
                                                _insideEP,
                                                _outsideEP);
                            if (null != flushTask)
                            {
                                await flushTask.ConfigureAwait(false);
                                flushTask = null;
                            }

                            await packet.WriteToStreamAsync(outsideStream);
                        }
                        else if (SMPPacket.IsSMPPacketType(packetTypeBuffer[0]))
                        {
                            var smpType = (SmpPacketType)packetTypeBuffer[0];
                            var packet = await SMPPacket.ReadFromStreamAsync(_insideStream, false, smpType);
                            if (VerboseLogging)
                                log.DebugFormat("Read SMP (MARS) {0} packet of {1} bytes from {2} for {3}",
                                                packet.Flags,
                                                packet.Length,
                                                _insideEP,
                                                _outsideEP);
                            if (null != flushTask)
                            {
                                await flushTask.ConfigureAwait(false);
                                flushTask = null;
                            }

                            await packet.WriteToStreamAsync(outsideStream);
                        }
                        else
                        {
                            log.ErrorFormat(
                                "Unexpected message type {0:X2} received from {1} for {2} - killing connection.",
                                packetTypeBuffer[0],
                                _insideEP,
                                _outsideEP);
                            Close();
                            return;
                        }
                    }
                    else
                        break;

                    // Flush the write if we're writing to SSL, since it seems the ODBC and OLEDB drivers need the ends of TDS messages to be the ends of SSL packets.
                    if (flushAfterWrite)
                        // NOTE: don't await flush here, await it immediately before writing; that way we can read before the flush is complete
                        flushTask = outsideStream.FlushAsync();
                }

                log.DebugFormat("{0} _outsideClient for Send to {1} and _insideClient for Receive for {2}",
                                flushAfterWrite ? "Flushing _outsideSSL and closing" : "Closing",
                                _insideEP,
                                _outsideEP);
                if (flushAfterWrite)
                    _outsideSSL.Flush();
                _outsideClient.Client.Shutdown(SocketShutdown.Send);
                _insideClient.Client.Shutdown(SocketShutdown.Receive);
            }
            catch (ObjectDisposedException)
            {
                // Swallow ObjectDisposedExceptions if (and only if) we've shut down already
                if (_state != StateEnum.Closed)
                    throw;
            }
        }
    }
}
