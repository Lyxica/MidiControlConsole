using System;
using System.IO;
using System.Threading;
using MidiControl.ExternalAPI;

namespace MidiControl
{
    internal class Program
    {
        private static readonly LPD lpd = new LPD();
        private static readonly SmartPAD smartpad = new SmartPAD();
        private static FileSystemWatcher watcher;
        private static readonly USBNotification usbNotification = new USBNotification();
        public static bool showDebug = false;

        private static readonly string UserFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "MidiController");

        private static void Main(string[] args)
        {
            CreateUserFolder();
            watcher = new FileSystemWatcher(UserFolder);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnChanged;
            watcher.Filter = "*.yml";
            watcher.EnableRaisingEvents = true;

            if (smartpad.IsSupported()) { smartpad.Start(); }

            if (lpd.IsSupported()) { lpd.Start(); }

            LoadYml();

            usbNotification.OnNewUSBDevice += () =>
            {
                if (smartpad.IsSupported()) { smartpad.UpdateDevice(); }

                if (lpd.IsSupported()) { lpd.UpdateDevice(); }
            };

            while (true)
            {
            StartOfLoop:
                Console.Clear();
                Console.WriteLine(String.Format("Debugging: {0}", showDebug));
                Console.WriteLine("Found the following audio session names:");
                foreach (var name in lpd.GetAudioSessionNames()) Console.WriteLine(String.Format("\t{0}", name));

                Console.WriteLine("\nKey controls:\n\tF9 = Toggle debug messages\n\tEnter = Print audio session names");


                while (true) {
                    var key = Console.ReadKey();
                    switch (key.Key)
                    {
                        case ConsoleKey.F9:
                            showDebug = !showDebug;
                            goto StartOfLoop;

                        case ConsoleKey.Enter:
                            goto StartOfLoop;

                        default:
                            continue;
                    }
                }
            }
        }

        public static void Log(string msg)
        {
            if (showDebug)
            {
                Console.WriteLine(msg);
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            string data;
            var attempt = 1;
            while (true)
            {
                try
                {
                    data = File.ReadAllText(e.FullPath);
                    break;
                }
                catch (IOException ex) when (ex.Message.EndsWith("used by another process."))
                {
                    if (attempt > 5) { throw new IOException($"Couldn't read YML file '{e.FullPath}'"); }

                    attempt += 1;
                    Thread.Sleep(100);
                }
            }

            BroadcastYml(e.Name, data);
        }

        private static void LoadYml()
        {
            var files = Directory.EnumerateFiles(UserFolder, "*.yml");
            foreach (var file in files)
            {
                var data = File.ReadAllText(file);
                BroadcastYml(Path.GetFileName(file), data);
            }
        }

        private static void BroadcastYml(string name, string data)
        {
            if (smartpad.IsSupported()) { smartpad.OnYmlChange(name, data); }

            if (lpd.IsSupported()) { lpd.OnYmlChange(name, data); }
        }

        private static void CreateUserFolder()
        {
            if (Directory.Exists(UserFolder)) { return; }

            Directory.CreateDirectory(UserFolder);
        }
    }
}