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
        private Dictionary<string, Action> actions;
        private DisplayShow ds;
        private ScrollControl sc;
        private Dictionary<int, string> side_buttons;

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

            var y = File.OpenText(@"C:\Users\Alexia\OneDrive\Documents\LINQPad Queries\side_buttons.yml");
            side_buttons = deserializer.Deserialize<Dictionary<int, string>>(y);
            y.Close();

            sc = new ScrollControl(md);
            ds = new DisplayShow(md);

            ds.change_color("red", ds.get_address(0, 0));
        }

        private void msg_parser(object sender, MidiInMessageEventArgs e)
        {
            var data = BitConverter.GetBytes(e.RawMessage);

            if (data[0] == 0x90) // Pad buttons
            {
                if (data[1] == 0) { User32API.LockWorkStation(); }
            }
            else if (data[0] == 0x9F) // Side buttons
            {
                if (side_buttons.ContainsKey(data[1]))
                {
                    var proc = Process.Start(side_buttons[data[1]]);
                    User32API.SetForegroundWindow(proc.MainWindowHandle);
                }
            }
            else if (data[0] == 0xB0) // Wheel/knob controls
            {
                var knob = data[1];
                if (knob == 7) { sc.update(data[2]); }
                else if (knob == 6) { sc.reset(); }
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