using System;
using System.IO;
using System.Threading;

namespace MidiControl
{
    internal class Program
    {
        private static readonly LPD lpd = new LPD();
        private static readonly SmartPAD smartpad = new SmartPAD();
        private static FileSystemWatcher watcher;

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

            while (true)
            {
                Console.WriteLine("Found the following audio session names:");
                foreach (var name in lpd.GetAudioSessionNames()) Console.WriteLine(name);

                Console.WriteLine("\nPress any key to print audio session names again.");
                Console.ReadLine();
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