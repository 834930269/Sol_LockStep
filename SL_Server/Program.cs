using SL_Server.Net;
using System;
using System.Threading.Tasks;

namespace SL_Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await TCPServer.Start(8899);
            Console.ReadLine();
        }
    }
}
