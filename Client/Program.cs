using System.Net;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Int32 serverPort = 55000;
            Client client = new Client(IPAddress.Loopback, serverPort);
            await client.Connect();

        }
    }
}
