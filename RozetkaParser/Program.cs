using ParseLib;
using System;

namespace RozetkaParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new ParseLib.Service.RozetkaParser();
            parser.ParseSite();

            Console.WriteLine("Hello World!");
        }
    }
}
