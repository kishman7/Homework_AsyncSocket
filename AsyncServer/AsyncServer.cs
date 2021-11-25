using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AsyncServer
{
    class AsyncServer
    {
        private const int Port = 2020;
        private const int Backlog = 10;
        private const int Size = 100;
        static AutoResetEvent done = new AutoResetEvent(false); //подія для здійснення синхронізації потоків
        static void Main(string[] args)
        {
            Console.Title = "Server";
            StartServer();
        }

        private static void StartServer()
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip = entry.AddressList[0];
            //1 new Socket
            Socket server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //2 Bind(EP) -> EP = IP:port
                server.Bind(new IPEndPoint(ip, Port)); //байндимо сервер з IP та портом
                //3 Listen
                server.Listen(Backlog); // сервер слухає одночасно кількість клієнтів

                while (true)
                {
                    Console.WriteLine("Wait for connection...");
                    //4 Accept
                    server.BeginAccept(AcceptCallback, server); // через асинхронний делегат BeginAccept виділяємо окремий потік для підключення клієнта
                    done.WaitOne(); // чекаю на сигнал. Якщо подію переведуть в сигнальний стан, то тоді цикл повторить іншу ітерацію, інакше потік буде за блокований
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            done.Set(); //встановлюємо подію в  сигнальний стан. Значить тут все ОК і ми готові слухати нового клієнта
            var server = (Socket)ar.AsyncState; // звертаємось до сервера через параметер ar з делегата BeginAccept
            //5) work with client (Receive)
            var client = server.EndAccept(ar); // отримуємо клієнтський Socket
            var buffer = new byte[Size];
            var data = new //TransferObject -> struct, class, який потрібно реалізувати вище
            {
                Socket = client,
                Buffer = buffer,
                Size = Size
            };
            client.BeginReceive(buffer, 0, Size, SocketFlags.None, ReceiveCallBack, data);
        }

        private static void ReceiveCallBack(IAsyncResult ar)
        {
            var data = (dynamic)ar.AsyncState;
            //var client = data.GetType().GetProperty("Socket").GetValue(data); // один з варіантів використання data (складнійший)
            var client = (Socket)data.Socket; // інший з варіантів використання data (простійший)
            var countBytes = client.EndReceive(ar); // повертаємо кількість байтів отриманих від клієнта
            var buffer = (byte[])data.Buffer;
            string result = Encoding.UTF8.GetString(buffer, 0, countBytes);
            Console.WriteLine("Got: {0} from {1}", result, client.RemoteEndPoint);

            //6) Send responce
            SendTo(client, result); //повертаємо повідомлення для нашого клієнта
        }

        private static void SendTo(Socket client, string result)
        {
            Console.WriteLine("Receive: " + result); //виводимо повідомлення, яке отримаємо
            var responce = Encoding.UTF8.GetBytes(result);
            byte [] temp;

            if (result.Equals("date", StringComparison.CurrentCultureIgnoreCase))
            {

                temp = Encoding.UTF8.GetBytes(DateTime.Now.ToShortDateString());
                client.BeginSend(temp, 0, temp.Length, SocketFlags.None, SendCallBack, client);

            }
            else if (result.Equals("time", StringComparison.CurrentCultureIgnoreCase))
            {
                temp = Encoding.UTF8.GetBytes(DateTime.Now.ToLongTimeString());

                client.BeginSend(temp, 0, temp.Length, SocketFlags.None, SendCallBack, client);

            }
            else
            {
                temp = Encoding.UTF8.GetBytes("Not command!!!");
                client.BeginSend(temp, 0, temp.Length, SocketFlags.None, SendCallBack, client);

            }
        }

        private static void SendCallBack(IAsyncResult ar)
        {
            var client = (Socket)ar.AsyncState; // звертаємось до клієнта через параметер ar з делегата BeginSend
            int countBytes = client.EndSend(ar); // отримуємо кількість байт від клієнта

            Console.WriteLine("OK. Sent {0} bytes to client", countBytes);

            // 7) Close
            client.Close();
        }
    }
}
