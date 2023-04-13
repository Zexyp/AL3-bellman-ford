using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;

namespace VeryFunnyGraphs
{
    class Connector : IDisposable
    {
        private TcpClient client;
        private StreamWriter writer;
        private StreamReader reader;

        public string Use(string host, int port, string message)
        {
            if (!Connect(host, port))
                throw new Exception();
            Send(message);
            var result = Receive();
            Dispose();
            return result;
        }

        public bool Connect(string host, int port)
        {
            try
            {
                client = new TcpClient();
                client.Connect(host, port);
                var stream = client.GetStream();
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);
                return true;
            }
            catch (SocketException e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        public void Dispose()
        {
            writer.Dispose();
            reader.Dispose();
            client.Close();
            client.Dispose();
        }

        public void Send(string message)
        {
            writer.WriteLine(message);
            writer.Flush();
        }

        public string Receive()
        {
            return reader.ReadLine();
        }
    }
}
