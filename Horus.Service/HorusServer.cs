using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ASCOM.Astrometry.Transform;
using Horus.Shared;
using WatsonTcp;

namespace Horus.Service
{

    public class TelescopeState
    {
        private Transform _transform;

        public void SetEnvironment()
        {
            //https://github.com/WCMoses/ES-PMC8-LocalServer/blob/dccd0e17d481359ce41dcabdea9acb4c516f1462/ES_PMC8-Server/Driver/Driver_old.vb
        }
    }

    public class HubClient : IDisposable
    {
        public const byte ESCAPE_BYTE = 0xFE;
        public const byte FRAME_END_BYTE = 0xFF;

        private TcpClient _client;
        private NetworkStream _stream;

        public HubClient()
        {
            _client = new TcpClient();
            _client.Connect(new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 120 }), 9001));
            _stream = _client.GetStream();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        public void Send()
        {
            var data = Encoding.ASCII.GetBytes("the big brown fox jumps over the lazy dog");

            var frame = new byte[data.Length + 1];

            using (var stream = new MemoryStream(frame))
            {
                stream.WriteByte(42);
                stream.Write(data, 0, data.Length);
            }

            frame = EscapePayload(frame);

            _stream.Write(frame, 0, frame.Length);
            _stream.WriteByte(FRAME_END_BYTE);

        }

        public byte[] EscapePayload(byte[] data)
        {
            int final = data.Length;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == ESCAPE_BYTE || data[i] == FRAME_END_BYTE)
                    final++;
            }

            if (final == data.Length)
                return data;

            var escaped = new byte[final];

            for (int sourceIndex = 0, targetIndex = 0;  sourceIndex < data.Length; sourceIndex++, targetIndex++)
            {
                if (data[sourceIndex] == ESCAPE_BYTE || data[sourceIndex] == FRAME_END_BYTE)
                    escaped[targetIndex++] = ESCAPE_BYTE;

                escaped[targetIndex] = data[sourceIndex];
            }

            return escaped;
        }
    }

    public class HorusServer
    {
        private WatsonTcpServer _server;

        private HubClient _hub;

        private bool _runForever = true;

        public void Start()
        {
            if (_hub != null) _hub.Dispose();
            _hub = new HubClient();
            
            _server = new WatsonTcpServer("127.0.0.1", 9000);
            _server.Events.ClientConnected += (_, args) => { Console.WriteLine("Client connected: " + args.IpPort); };
            _server.Events.ClientDisconnected += (_, args) => { Console.WriteLine("Client disconnected: " + args.IpPort + ": " + args.Reason); };
            _server.Events.ServerStarted += (_, __) => Console.WriteLine("Server started");
            _server.Events.ServerStopped += (_, __) => Console.WriteLine("Server stopped");
            _server.Events.MessageReceived += MessageReceived;
            _server.Callbacks.SyncRequestReceived += SyncRequestReceived;

            _server.Start();
        }

        private void HubMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.Write("Message from " + args.IpPort + ": ");
            if (args.Data != null) 
                Console.WriteLine(Encoding.UTF8.GetString(args.Data));
            else
                Console.WriteLine("[null]");
        }

        private SyncResponse HubSyncRequestReceived(SyncRequest req)
        {
            Console.Write("Message from " + req.IpPort + ": ");
            if (req.Data != null) 
                Console.WriteLine(Encoding.UTF8.GetString(req.Data));
            else 
                Console.WriteLine("[null]");

            return new SyncResponse(req, "Unrecognised command");
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.Write("Message from " + args.IpPort + ": ");
            if (args.Data != null) Console.WriteLine(Encoding.UTF8.GetString(args.Data));
            else Console.WriteLine("[null]");

            if (args.Metadata != null && args.Metadata.Count > 0)
            {
                Console.WriteLine("Metadata:");
                foreach (KeyValuePair<object, object> curr in args.Metadata)
                {
                    Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                }
            }
        }

        private SyncResponse SyncRequestReceived(SyncRequest req)
        {
            if (req.Data != null)
            {
                var commandValue = BitConverter.ToUInt16(req.Data, 0);
                if (Enum.IsDefined(typeof(Commands), commandValue))
                {
                    var command = (Commands)commandValue;
                    Console.Write("Cmd: " + command);
                    switch (command)
                    {
                        case Commands.FocuserMoveSet:
                            var moveValue = BitConverter.ToInt32(req.Data, sizeof(ushort));
                            Console.Write(" Value: " + moveValue);
                            _hub.Send();
                            break;
                    }

                    return new SyncResponse(req, new byte[0]);
                }
            }

            if (req.Metadata != null && req.Metadata.Count > 0)
            {
                Console.WriteLine("Metadata:");
                foreach (KeyValuePair<object, object> curr in req.Metadata)
                {
                    Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                }
            }

            return new SyncResponse(req, "Unrecognised command");
        }
    }

}