using System;
using System.Linq;
using System.Threading.Tasks;

namespace Horus.Service
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            var server = new HorusServer();
            server.Start();

            Console.ReadKey();

            return Task.FromResult(0);
        }
    }

}
