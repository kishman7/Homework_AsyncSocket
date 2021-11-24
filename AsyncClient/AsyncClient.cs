using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncClient
{
    class AsyncClient
    {
        private const int Port = 2020;
        private const int Backlog = 100;
        private const int Size = 100;

        static void Main(string[] args)
        {
            //for (int i = 0; i < 10; i++)
            //{
            //    Process.Start("test.exe");
            //}
            Console.Title = "Client";

            StartClient();

            Console.ReadLine();
        }

        private static void StartClient()
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip = entry.AddressList[0];
            // Socket
            Socket client = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Connect
                client.BeginConnect(new IPEndPoint(ip, Port), ConnectCallBack, client);
            }
            catch (SocketException ex)
            {
            }
        }

        private static void ConnectCallBack(IAsyncResult ar)
        {
            // встановлюємо з’єднання
            var client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            Console.WriteLine("Enter data to get a date or time! (Enter date or time)");

            var data = Encoding.UTF8.GetBytes(Console.ReadLine());

            // Send
            client.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState;
            int countBytes = client.EndSend(ar);
            Console.WriteLine("Send to server {0} bytes", countBytes);

            ReceiveFrom(client);
        }

        private static void ReceiveFrom(Socket client)
        {
            var buffer = new byte[Size];
            var data = new
            {
                Socket = client,
                Buffer = buffer,
                Size = Size
            };
            client.BeginReceive(buffer, 0, Size, SocketFlags.None, ReceiveCallback, data);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var data = (dynamic)ar.AsyncState;
            var client = (Socket)data.Socket;
            var buffer = (byte[])data.Buffer;

            var countBytes = client.EndReceive(ar);

            var responce = Encoding.UTF8.GetString(buffer, 0, countBytes);

            Console.WriteLine("Received from server ({0}): {1}", client.RemoteEndPoint, responce);
        }
    }
}
