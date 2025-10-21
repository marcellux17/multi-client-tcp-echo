using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Helpers;

namespace Client
{
    internal class Client
    {
        int heartBeatIntervalInSec = 5;
        int heartBeatChecksLimit = 3;
        TaskCompletionSource<string> messageEcho;
        DateTime lastMessage;
        TcpClient socket;
        IPEndPoint remoteEndPoint;
        int connectionAliveFlag;

        SemaphoreSlim writeLock;
        public Client(IPAddress serverAddress, Int32 serverPort)
        {
            remoteEndPoint = new IPEndPoint(serverAddress, serverPort);
            socket = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
            connectionAliveFlag = 0;
            writeLock = new SemaphoreSlim(1, 1);   
        }
        public async Task Connect()
        {
            try
            {
                socket.Connect(remoteEndPoint);
                connectionAliveFlag = 1;
                ReadStream();
                PrintWelcomeMessage();
                CheckHeartBeats();
                while (Interlocked.CompareExchange(ref connectionAliveFlag, 1, 1) == 1)
                {
                    Console.Write("> ");

                    string messageText = Console.ReadLine();
                    await writeLock.WaitAsync();
                    try
                    {
                        await NetworkHelper.SendMessage(0, socket, messageText);
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                    messageEcho = new TaskCompletionSource<string>();
                    string echo = await messageEcho.Task;
                    Console.WriteLine(echo);
                    await Task.Delay(100);

                }
            }
            catch (Exception ex)
            {
                await CloseClient();
            }

        }
        private async Task CheckHeartBeats()
        {
            int heartBeatChecksLeft = heartBeatChecksLimit;
            while (Interlocked.CompareExchange(ref connectionAliveFlag, 1, 1) == 1)
            {
                heartBeatChecksLeft--;
                if ((DateTime.UtcNow - lastMessage).TotalSeconds <= heartBeatIntervalInSec * 2.5 && heartBeatChecksLeft >= 0)
                {
                    heartBeatChecksLeft = heartBeatChecksLimit;
                }
                else if (heartBeatChecksLeft < 0)
                {
                    //we disconnect
                    await CloseClient();
                }
                if (Interlocked.CompareExchange(ref connectionAliveFlag, 1, 1) == 1)
                {
                    await Task.Delay(heartBeatIntervalInSec * 1000);
                }
            }
        }
        private async Task ReadStream()
        {
            while (true)
            {
                int messageType = await NetworkHelper.GetMessageType(socket);
                lastMessage = DateTime.UtcNow;
                int payloadLength = await NetworkHelper.GetMessageLength(socket);
                string message = await NetworkHelper.GetMessage(socket, payloadLength);
                if (messageType == 1)
                {
                    //we send back a pong
                    await writeLock.WaitAsync();
                    try
                    {
                        await NetworkHelper.SendMessage(1, socket, "PONG");
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }
                else
                {
                    messageEcho.SetResult(message);
                }
            }

        }
        private void PrintWelcomeMessage()
        {
            Console.WriteLine("Welcome to the echo server!");
            Console.WriteLine("Send messages to the server and see if it responds!");
        }
        private async Task CloseClient()
        {
            if (Interlocked.Exchange(ref connectionAliveFlag, 0) == 1)
            {
                Console.WriteLine($"Server unavailble :(");
                Console.WriteLine("Client shutting down...");
                socket.Close();
                await Task.Delay(500);
                Environment.Exit(0);
            }
            
        }
    }
}
