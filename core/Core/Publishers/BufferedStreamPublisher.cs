using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Peach.Core.IO;

namespace Peach.Core.Publishers
{
	/// <summary>
	/// Helper class for creating stream based publishers.
	/// This class is used when the publisher implementation
	/// has a non-seekable stream interface.
	/// 
	/// Most derived classes should only need to override OnOpen()
	/// and in the implementation open _client and call StartClient()
	/// to begin async reads from _client to _buffer.
	/// </summary>
	public abstract class BufferedStreamPublisher : Publisher
	{
		public int SendTimeout
		{
			get { return _sendTimeout; }
			set { _sendTimeout = value; }
		}

		public int Timeout { get; set; }
		public bool NoWriteException { get; set; }
		public int ReadAvailableTimeout { get; set; }

		/// <summary>
		/// Special mode in which Input will wait for a single byte with Timeout timeout,
		/// but keep reading any available data after that till a timeout of 250 ms
		/// </summary>
		public bool ReadAvailableMode { get; set; }

		protected int _sendTimeout = -1;
		protected int _sendLen = 0;
		protected byte[] _sendBuf = new byte[0x14000]; // Buffer size Stream.CopyTo uses
		protected byte[] _recvBuf = new byte[0x14000];
		protected object _bufferLock = new object();
		protected object _clientLock = new object();
		protected string _clientName = null;
		protected ManualResetEvent _event = null;
		protected Stream _client = null;
		protected MemoryStream _buffer = null;
		protected bool _timeout = false;

		protected BufferedStreamPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			NoWriteException = false;
		}

		#region Async Read Operations

		protected void ScheduleRead()
		{
			lock (_clientLock)
			{
				try
				{
					System.Diagnostics.Debug.Assert(_client != null);
					ClientBeginRead(_recvBuf, 0, _recvBuf.Length, OnReadComplete, _client);
				}
				catch (Exception ex)
				{
					Logger.Debug("Unable to start reading data from {0}.  {1}", _clientName, ex.Message);
					CloseClient();
				}
			}
		}

		/// <summary>
		/// Called by OnReadComplete after new data added to buffer.
		/// </summary>
		/// <remarks>
		/// Allows children to perform initial processing of data
		/// if needed.
		/// </remarks>
		protected virtual void HandleReadCompleted()
		{
		}

		protected void OnReadComplete(IAsyncResult ar)
		{
			lock (_clientLock)
			{
				// Already closed!
				if (_client == null)
					return;

				try
				{
					int len = ClientEndRead(ar);

					if (len == 0)
					{
						Logger.Debug("Read 0 bytes from {0}, closing client connection.", _clientName);
						CloseClient();
					}
					else
					{
						Logger.Debug("Read {0} bytes from {1}", len, _clientName);

						lock (_bufferLock)
						{
							long pos = _buffer.Position;
							long prevLen = _buffer.Length;
							_buffer.Seek(0, SeekOrigin.End);
							_buffer.Write(_recvBuf, 0, len);
							_buffer.Position = pos;

							// Reset any timeout value
							_timeout = false;

							if (Logger.IsDebugEnabled)
								Logger.Debug("\n\n" + Utilities.HexDump(_recvBuf, 0, len, startAddress: prevLen));

							HandleReadCompleted();
						}

						ScheduleRead();
					}
				}
				catch (Exception ex)
				{
					var baseEx = ex.GetBaseException() as SocketException;
					if (baseEx != null && baseEx.SocketErrorCode == SocketError.Interrupted)
						Logger.Debug("Read from {0} interrupted, closing client connection.", _clientName);
					else
						Logger.Debug("Unable to complete reading data from {0}.  {1}", _clientName, ex.Message);

					CloseClient();
				}
			}
		}

		#endregion

		#region Base Client/Buffer Sync Implementations

		protected virtual void StartClient()
		{
			System.Diagnostics.Debug.Assert(_clientName != null);
			System.Diagnostics.Debug.Assert(_client != null);
			System.Diagnostics.Debug.Assert(_buffer == null);

			_buffer = new MemoryStream();
			_event.Reset();
			ScheduleRead();
		}

		protected virtual void CloseClient()
		{
			lock (_clientLock)
			{
				if (_client == null || _clientName == null)
					return;

				Logger.Debug("Closing connection to {0}", _clientName);
				ClientClose();
				_client = null;
				_clientName = null;
				_event.Set();
			}
		}

		protected virtual IAsyncResult ClientBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientBeginRead>");
			return _client.BeginRead(buffer, offset, count, callback, state);
		}

		protected virtual int ClientEndRead(IAsyncResult asyncResult)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientEndRead>");
			return _client.EndRead(asyncResult);
		}

		protected virtual IAsyncResult ClientBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientBeginWrite> offset: {0} count: {1}", offset, count);
			_sendLen = count;
			return _client.BeginWrite(buffer, offset, count, callback, state);
		}

		protected virtual int ClientEndWrite(IAsyncResult asyncResult)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientEndWrite>");
			_client.EndWrite(asyncResult);
			return _sendLen;
		}

		protected virtual void ClientWrite(BitwiseStream data)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("Client> {0} bytes", data.Length);
			data.CopyTo(_client);
		}

		protected virtual void ClientShutdown()
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientShutdown>");
			_client.Close();
		}

		protected virtual void ClientClose()
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientClose>");
			_client.Close();
		}

		#endregion

		#region Publisher Overrides

		protected override void OnStart()
		{
			System.Diagnostics.Debug.Assert(_event == null);
			_event = new ManualResetEvent(true);
		}

		protected override void OnStop()
		{
			if (_event != null)
			{
				_event.Close();
				_event = null;
			}
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(_client == null);
			System.Diagnostics.Debug.Assert(_buffer == null);
		}

		protected override void OnClose()
		{
			lock (_clientLock)
			{
				if (_client != null)
				{
					Logger.Debug("Shutting down connection to {0}", _clientName);

					try
					{
						ClientShutdown();
					}
					catch (Exception e)
					{
						Logger.Debug("Failed to gracefully shutdown connection to {0}.  {1}", _clientName, e.Message);
					}
				}
			}

			if (!_event.WaitOne(TimeSpan.FromMilliseconds(Timeout)))
			{
				lock (_clientLock)
				{
					if (_client != null)
					{
						Logger.Debug("Graceful shutdown of connection timed out.  Force closing...");
						CloseClient();
					}
				}
			}

			if (_buffer != null)
			{
				_buffer.Close();
				_buffer = null;
			}
		}

		protected override void OnInput()
		{
			//Check to make sure buffer has been initilized before continuing. 
			lock (_bufferLock)
			{
				if (_buffer == null)
					throw new SoftException("Error on data input, the buffer is not initalized.");
			}

			// Try to make sure 1 byte is available for reading.  Without doing this,
			// state models with an initial state of input can miss the message.
			// Also, ensure the read timeout is reset on every input action.
			_timeout = false;
			WantBytes(1);

			if (ReadAvailableMode || !_timeout)
				ReadAllAvailable();
		}

		protected override void OnOutput(BitwiseStream data)
		{
			IAsyncResult ar = null;
			int offset = 0;
			int length = 0;

			try
			{
				while (true)
				{
					lock (_clientLock)
					{
						//Check to make sure buffer has been initilized before continuing. 
						if (_client == null)
						{
							// First time through, propagate error
							if (ar == null)
								throw new PeachException("Error on data output, the client is not initalized.");

							// Client has been closed!
							return;
						}

						if (Logger.IsDebugEnabled && ar == null)
							Logger.Debug("\n\n" + Utilities.HexDump(data));

						if (ar != null)
							offset += ClientEndWrite(ar);

						if (offset == length)
						{
							offset = 0;
							length = data.Read(_sendBuf, 0, _sendBuf.Length);

							if (length == 0)
								return;
						}

						ar = ClientBeginWrite(_sendBuf, offset, length - offset, null, null);
					}

					if (Logger.IsTraceEnabled) Logger.Trace("OnOutput> WaitOne() timeout: {0}", SendTimeout);

					if (SendTimeout < 0)
						ar.AsyncWaitHandle.WaitOne();
					else if (!ar.AsyncWaitHandle.WaitOne(SendTimeout))
						throw new TimeoutException();

					if (Logger.IsTraceEnabled) Logger.Trace("OnOutput> WaitOne() done");
				}
			}
			catch (Exception ex)
			{
				Logger.Error("output: Error during send.  " + ex.Message);

				// Shutdown socket on error to support Lifetime parameter
				CloseClient();

				// Allow pit to eat write exception
				if (!NoWriteException)
				{
					if(ex is NotSupportedException && ex.Message == "Stream does not support writing.")
						throw new SoftException("Error, remote side closed connection early.", ex);

					throw new SoftException(ex);
				}
			}
		}

		public override void WantBytes(long count)
		{
			if (count == 0)
				return;

			//lock (_bufferLock)
			//{
			//	Logger.Debug("WantBytes({0}): Available: {1}", count, _buffer.Length - _buffer.Position);
			//}

			var sw = Stopwatch.StartNew();

			// Wait up to Timeout milliseconds to see if count bytes become available
			while (true)
			{
				lock (_clientLock)
				{
					// If the connection has been closed, we are not going to get anymore bytes.
					if (_client == null)
						return;

					// If we have already timed out, 
				}

				lock (_bufferLock)
				{
					if ((_buffer.Length - _buffer.Position) >= count || _timeout)
						return;

					if (sw.ElapsedMilliseconds > Timeout)
					{
						Logger.Debug("WantBytes({0}): Timeout waiting for data.  Timeout is {1} ms", count, Timeout);
						_timeout = true;
						return;
					}
				}

				Thread.Sleep(10);
			}
		}

		/// <summary>
		/// Continue reading data until no data is received for 150 ms
		/// </summary>
		public void ReadAllAvailable()
		{
			var timeout = ReadAvailableTimeout;
			var currentLength = _buffer.Length;
			var sw = Stopwatch.StartNew();

			// Wait up to Timeout milliseconds to see if count bytes become available
			while (true)
			{
				lock (_clientLock)
				{
					// If the connection has been closed, we are not going to get anymore bytes.
					if (_client == null)
						return;
				}

				lock (_bufferLock)
				{
					if (_buffer.Length > currentLength)
					{
						currentLength = _buffer.Length;
						sw.Restart();
					}
				}

				if (sw.ElapsedMilliseconds >= timeout)
					return;

				Thread.Sleep(10);
			}
		}

		#endregion

		#region Stream

		public override bool CanRead
		{
			get
			{
				lock (_bufferLock)
				{
					return _buffer.CanRead;
				}
			}
		}

		public override bool CanSeek
		{
			get
			{
				lock (_bufferLock)
				{
					return _buffer.CanSeek;
				}
			}
		}

		public override void Flush()
		{
			lock (_clientLock)
			{
				if (_client == null)
					throw new NotSupportedException();

				_client.Flush();
			}
		}

		public override long Length
		{
			get
			{
				lock (_bufferLock)
				{
					return _buffer.Length;
				}
			}
		}

		public override long Position
		{
			get
			{
				lock (_bufferLock)
				{
					return _buffer.Position;
				}
			}
			set
			{
				lock (_bufferLock)
				{
					_buffer.Position = value;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			lock (_bufferLock)
			{
				return _buffer.Read(buffer, offset, count);
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			lock (_bufferLock)
			{
				return _buffer.Seek(offset, origin);
			}
		}

		public override void SetLength(long value)
		{
			lock (_bufferLock)
			{
				_buffer.SetLength(value);
			}
		}

		#endregion
	}
}
