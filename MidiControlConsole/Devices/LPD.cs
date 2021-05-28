using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using YamlDotNet.Serialization;

namespace MidiControl
{
    public class LPD : MidiDevice
    {
        private readonly ConcurrentDictionary<int, MidiEvent> event_list = new ConcurrentDictionary<int, MidiEvent>();

        private readonly EventWaitHandle wait_event = new EventWaitHandle(false, EventResetMode.AutoReset);
        private MidiIn device;

        private Dictionary<int, string> lut = new Dictionary<int, string>();

        public LPD() : base("LPD8") { UpdateDevice(); }

        public void UpdateDevice()
        {
            for (var i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                var name = MidiIn.DeviceInfo(i).ProductName;
                if (name.Contains("LPD8")) { device = new MidiIn(i); }
            }
        }

        public void Start()
        {
            device.MessageReceived += midiIn_messageReceive;
            device.Start();
            new Thread(ThreadProc).Start();
        }

        public bool IsSupported()
        {
            for (var i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                var name = MidiIn.DeviceInfo(i).ProductName;
                if (name.Contains("LPD8")) { return true; }
            }

            return false;
        }

        private void ThreadProc()
        {
            while (true)
            {
                if (event_list.Count == 0) { wait_event.WaitOne(); }

                var now = DateTime.Now;
                foreach (var key in event_list.Keys)
                {
                    var event_item = event_list[key];
                    if (now >= event_item.time)
                    {
                        SetVolumeMatchingSessionNames(lut[event_item.controller], event_item.value);
                        while (!event_list.TryRemove(key, out _)) { }
                    }
                }
            }
        }

        private void midiIn_messageReceive(object sender, MidiInMessageEventArgs e)
        {
            var concrete_event = (ControlChangeEvent) e.MidiEvent;
            var controller = (int) concrete_event.Controller;
            if (!lut.ContainsKey(controller) && !lut.ContainsKey(controller - 40)) { return; }

            if (controller > 90) // All of the mute button-ID's are higher than 90. 
            {
                // Because the knobs and mute buttons share the same 1's place value, we can just subtract 40 to tie together mute buttons and knob buttons.
                MuteMatchingSessionNames(lut[controller - 40],
                    concrete_event.ControllerValue > 0);
            }
            else
            {
                var pod_event = new MidiEvent(controller, concrete_event.ControllerValue);
                event_list[controller] = pod_event;
                wait_event.Set();
            }
        }

        private void SetVolumeMatchingSessionNames(string proc_name, float volume)
        {
            var matches = GetMatchingAudioSessions(proc_name);
            foreach (var match in matches) { match.SetVolume(volume); }
        }

        private void MuteMatchingSessionNames(string proc_name, bool mute)
        {
            var matches = GetMatchingAudioSessions(proc_name);
            foreach (var match in matches) { match.SetMute(mute); }
        }

        private IEnumerable<AudioControl> GetMatchingAudioSessions(string proc_name)
        {
            var x = new MMDeviceEnumerator();
            var device = x.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (proc_name == "__System")
            {
                yield return new AudioControl(device);
                yield break;
            }

            if (proc_name == "__ActiveProcess")
            {
                uint proc_id;
                User32API.GetWindowThreadProcessId(User32API.GetForegroundWindow(), out proc_id);
                proc_name = Process.GetProcessById((int) proc_id).ProcessName;
            }

            var sessions = Enumerable.Range(0, device.AudioSessionManager.Sessions.Count)
                .Select(x => device.AudioSessionManager.Sessions[x]);
            var matching_sessions = sessions
                .Where(x => Process.GetProcessById((int) x.GetProcessID).ProcessName.Contains(proc_name))
                .Select(x => new AudioControl(x));
            foreach (var session in matching_sessions) { yield return session; }
        }

        public IEnumerable<string> GetAudioSessionNames()
        {
            var x = new MMDeviceEnumerator();
            var device = x.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var processNames = Enumerable.Range(0, device.AudioSessionManager.Sessions.Count).Select(x =>
                Process.GetProcessById((int) device.AudioSessionManager.Sessions[x].GetProcessID).ProcessName);
            return new HashSet<string>(processNames);
        }

        public void OnYmlChange(string name, string data)
        {
            if (name != "audio.yml") { return; }

            var deserializer = new Deserializer();
            lut = deserializer.Deserialize<Dictionary<int, string>>(data);
        }

        public class MidiEvent
        {
            public int controller;
            public DateTime time;
            public float value;

            public MidiEvent(int controller, float value)
            {
                this.controller = controller;
                this.value = getNormalizedValue(value);
                time = DateTime.Now.AddMilliseconds(8);
            }

            private float getNormalizedValue(float value)
            {
                // Max = 127
                // Add dead zones to both sides of number line. I.e., Without the dead zones, twisting the knob all the way to the left would not deafen a session.
                var range = Math.Clamp(value, 5f, 120f) - 5f;
                var norm = range / 115.0f;
                return norm;
            }
        }

        private class AudioControl
        {
            private readonly MMDevice device;
            private readonly AudioSessionControl session;

            private readonly AudioControlType type;

            public AudioControl(AudioSessionControl session)
            {
                type = AudioControlType.SESSION;
                this.session = session;
            }

            public AudioControl(MMDevice device)
            {
                type = AudioControlType.DEVICE;
                this.device = device;
            }

            public void SetMute(bool shouldMute)
            {
                if (type == AudioControlType.DEVICE) { device.AudioEndpointVolume.Mute = shouldMute; }
                else { session.SimpleAudioVolume.Mute = shouldMute; }
            }

            public void SetVolume(float volume)
            {
                if (type == AudioControlType.DEVICE) { device.AudioEndpointVolume.MasterVolumeLevelScalar = volume; }
                else { session.SimpleAudioVolume.Volume = volume; }
            }

            private enum AudioControlType
            {
                DEVICE,
                SESSION
            }
        }
    }
}