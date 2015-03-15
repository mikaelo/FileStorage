using System;
using Microsoft.Owin.Hosting;

namespace Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:9999"))
            {
                Console.ReadLine();
            }
        }
    }
}
