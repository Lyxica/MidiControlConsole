using System;

namespace MidiControl
{
    class Program
    {
        static readonly LPD lpd = new LPD();
        static readonly SmartPAD smartpad = new SmartPAD();
        static void Main(string[] args)
        {
            if (smartpad.IsSupported())
            {
                smartpad.Start();
            }
            if (lpd.IsSupported())
            {
                lpd.Start();
            }

            while (true)
            {
                Console.WriteLine("Found the following audio session names:");
                foreach (var name in lpd.GetAudioSessionNames())
                {
                    Console.WriteLine(name);
                }
                Console.WriteLine("\nPress any key to print audio session names again.");
                Console.ReadLine();
            }
        }
    }
}
