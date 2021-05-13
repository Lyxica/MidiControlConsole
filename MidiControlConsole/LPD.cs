using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MidiControl
{
    public class LPD
    {
        public class MidiEvent
        {
            public int controller;
            public float value;
            public DateTime time;

            public MidiEvent(int controller, float value)
            {
                this.controller = controller;
                this.value = getNormalizedValue(value);
                time = DateTime.Now.AddMilliseconds(8);
            }

            private float getNormalizedValue(float value)
            {
                // Max = 127
                float range = Math.Clamp(value, 5f, 120f) - 5f; // Add dead zones to both sides of number line. I.e., Without the dead zones, twisting the knob all the way to the left would not deafen a session.
                float norm = range / 115.0f;
                return norm;
            }
        }

        Dictionary<int, string> lut = new Dictionary<int, string>()
        {
            {51, "__System"},
            {52, "__ActiveProcess"},
            {55, "foobar2000"},
            {57, "chrome"},
            {58, "Discord"}
        };

        ConcurrentDictionary<int, MidiEvent> event_list = new ConcurrentDictionary<int, MidiEvent>();

        EventWaitHandle wait_event = new EventWaitHandle(false, EventResetMode.AutoReset);

        MidiIn device;

        public LPD()
        {
            for (var i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                var name = MidiIn.DeviceInfo(i).ProductName;
                if (name.Contains("LPD8"))
                {
                    device = new MidiIn(i);
                }
            }
            device.MessageReceived += midiIn_messageReceive;
            device.Start();
        }

        public void Start()
        {
            while (true)
            {
                if (event_list.Count == 0)
                {
                    wait_event.WaitOne();
                }

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

        void midiIn_messageReceive(object sender, MidiInMessageEventArgs e)
        {
            var concrete_event = ((NAudio.Midi.ControlChangeEvent)e.MidiEvent);
            int controller = (int)concrete_event.Controller;
            if (!lut.ContainsKey(controller) && !lut.ContainsKey(controller - 40))
            {
                return;
            }

            if (controller > 90) // All of the mute button-ID's are higher than 90. 
            {
                MuteMatchingSessionNames(lut[controller - 40], concrete_event.ControllerValue > 0); // Because the knobs and mute buttons share the same 1's place value, we can just subtract 40 to tie together mute buttons and knob buttons.
            }
            else
            {
                var pod_event = new MidiEvent(controller, concrete_event.ControllerValue);
                event_list[controller] = pod_event;
                wait_event.Set();
            }
        }

        void SetVolumeMatchingSessionNames(string proc_name, float volume)
        {
            var matches = GetMatchingAudioSessions(proc_name);
            foreach (var match in matches)
            {
                match.SetVolume(volume);
            }
        }

        void MuteMatchingSessionNames(string proc_name, bool mute)
        {
            var matches = GetMatchingAudioSessions(proc_name);
            foreach (var match in matches)
            {
                match.SetMute(mute);
            }
        }

        class AudioControl
        {
            enum AudioControlType
            {
                DEVICE, SESSION
            }

            AudioControlType type;
            AudioSessionControl session;
            MMDevice device;

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
                if (type == AudioControlType.DEVICE)
                {
                    device.AudioEndpointVolume.Mute = shouldMute;
                }
                else
                {
                    session.SimpleAudioVolume.Mute = shouldMute;
                }
            }

            public void SetVolume(float volume)
            {
                if (type == AudioControlType.DEVICE)
                {
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                }
                else
                {
                    session.SimpleAudioVolume.Volume = volume;
                }
            }
        }

        IEnumerable<AudioControl> GetMatchingAudioSessions(string proc_name)
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
                proc_name = Process.GetProcessById((int)proc_id).ProcessName;
            }

            var sessions = Enumerable.Range(0, device.AudioSessionManager.Sessions.Count).Select(x => device.AudioSessionManager.Sessions[x]);
            var matching_sessions = sessions.Where(x => Process.GetProcessById((int)x.GetProcessID).ProcessName.Contains(proc_name)).Select(x => new AudioControl(x));
            foreach (var session in matching_sessions)
            {
                yield return session;
            }
            yield break;
        }

        public IEnumerable<string> GetAudioSessionNames()
        {
            var x = new MMDeviceEnumerator();
            var device = x.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return Enumerable.Range(0, device.AudioSessionManager.Sessions.Count).Select(x => Process.GetProcessById((int)device.AudioSessionManager.Sessions[x].GetProcessID).ProcessName);
        }
    }
}
