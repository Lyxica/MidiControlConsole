using System;
using System.Threading;

namespace MidiControl
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new Thread(SmartPAD.Start);
            x.Start();

            var lpd = new LPD();
            var y = new Thread(lpd.start);
            y.Start();

            while (true)
            {
                Console.WriteLine("Found the following audio session names:");
                foreach(var name in lpd.GetAudioSessionNames())
                {
                    Console.WriteLine(name);
                }
                Console.WriteLine("\nPress any key to print audio session names again.");
                Console.ReadLine();
            }
        }
    }
}
