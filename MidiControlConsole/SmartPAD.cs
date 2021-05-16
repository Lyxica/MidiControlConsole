using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NAudio.Midi;
using YamlDotNet.Serialization;

namespace MidiControl
{
    public class SmartPAD
    {
        private readonly MidiDevice md;
        private readonly MidiIn mi;
        private Dictionary<int, string> _sideButtons = new Dictionary<int, string>();
        private Dictionary<string, Action> actions;
        private DisplayShow ds;
        private KnobScroll ks;
        private KnobSendInput ksi;
        private ShutdownBtn shutdownBtn;

        public SmartPAD()
        {
            for (var i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                var name = MidiOut.DeviceInfo(i).ProductName;
                if (name.Contains("SmartPAD")) { md = new MidiDevice(i); }
            }

            for (var i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                var name = MidiIn.DeviceInfo(i).ProductName;
                if (name.Contains("SmartPAD")) { mi = new MidiIn(i); }
            }
        }

        public bool IsSupported() { return mi != null && md != null; }

        public void Start()
        {
            mi.MessageReceived += msg_parser;
            mi.Start();

            var f = File.OpenText(@"C:\Users\Alexia\OneDrive\Documents\LINQPad Queries\settings.yml");
            var deserializer = new Deserializer();
            var actions = deserializer.Deserialize<Dictionary<string, Action>>(f);
            f.Close();

            ks = new KnobScroll(md, 0x7);
            ds = new DisplayShow(md);
            shutdownBtn = new ShutdownBtn(ds, 0x1);
            ksi = new KnobSendInput(md, 0x0, User32API.ScanCodeShort.OEM_4, User32API.ScanCodeShort.OEM_6);

            ds.change_color("red", ds.get_address(0, 0));
        }

        private void msg_parser(object sender, MidiInMessageEventArgs e)
        {
            var data = BitConverter.GetBytes(e.RawMessage);

            if (data[0] == 0x90) // Pad buttons
            {
                if (data[1] == 0) { User32API.LockWorkStation(); }

                if (data[1] == shutdownBtn.address) { shutdownBtn.Next(); }
            }
            else if (data[0] == 0x9F) // Side buttons
            {
                if (_sideButtons.ContainsKey(data[1]))
                {
                    var proc = Process.Start(_sideButtons[data[1]]);
                    User32API.SetForegroundWindow(proc.MainWindowHandle);
                }
            }
            else if (data[0] == 0xB0) // Wheel/knob controls
            {
                var knob = data[1];
                switch (knob)
                {
                    case 7:
                        ks.update(data[2]);
                        break;

                    case 6:
                        ks.reset();
                        break;

                    case 0:
                        ksi.update(data[2]);
                        break;
                }
            }
        }

        public void OnYmlChange(string name, string data)
        {
            switch (name)
            {
                case "side_buttons.yml":
                    var deserializer = new Deserializer();
                    _sideButtons = deserializer.Deserialize<Dictionary<int, string>>(data);
                    break;
            }
        }

        private struct Action
        {
            public struct ActionExecutionLight
            {
                public ActionExecution action;
                public Light light;
            }

            public struct ActionExecution
            {
                public string type;
                public string exec;
            }

            public struct Light
            {
                public string color;
                public string active;
            }

            public List<string> triggers;
            public Dictionary<string, ActionExecutionLight> actions;
        }
    }
}