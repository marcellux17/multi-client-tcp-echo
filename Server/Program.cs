namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Int32 serverPort = 55000;
            Server server = new Server(serverPort);
            await server.Start();
        }
    }
}
