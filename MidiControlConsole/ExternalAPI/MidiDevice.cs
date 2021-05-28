using System;
using NAudio.Midi;
using OneOf;
using OneOf.Types;

namespace MidiControl
{
    public class MidiDevice
    {
        public delegate void MidiControlEvent(Actions action, int address, int value);

        public enum Actions
        {
            NOTE_ON = 0x90,
            NOTE_OFF = 0x80,
            CONTROL_CHANGE = 0xB0
        }

        private readonly string deviceName;

        private OneOf<MidiIn, None> _inDevice;
        private OneOf<MidiOut, None> _outDevice;

        public MidiDevice(string deviceName) { LoadMidiPlatform(); }

        public event MidiControlEvent MidiEvent;

        private void LoadMidiPlatform()
        {
            _inDevice.Switch(
                midiIn =>
                {
                    midiIn.Stop();
                    midiIn.Dispose();
                },
                none => { }
            );
            _outDevice.Switch(
                midiOut => midiOut.Dispose(),
                none => { }
            );


            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:


                    for (var i = 0; i < MidiIn.NumberOfDevices; i++)
                    {
                        var name = MidiIn.DeviceInfo(i).ProductName;
                        if (!name.Contains(deviceName)) continue;

                        var midiIn = new MidiIn(i);
                        _inDevice = midiIn;
                        midiIn.MessageReceived += (sender, args) =>
                        {
                            var bytes = BitConverter.GetBytes(args.RawMessage);
                            MidiEvent?.Invoke((Actions) bytes[0], bytes[1], bytes[2]);
                        };
                    }

                    for (var i = 0; i < MidiOut.NumberOfDevices; i++)
                    {
                        var name = MidiOut.DeviceInfo(i).ProductName;
                        if (name.Contains(deviceName)) { _outDevice = new MidiOut(i); }
                    }

                    break;

                default:
                    throw new NotImplementedException("API for OS not implemented");
            }
        }

        public void Send(Actions action, int address, int value) { Send(action, address, value, 1); }

        public void Send(Actions action, int address, int value, int channel)
        {
            _outDevice.Switch(
                midi =>
                {
                    var note = new NoteEvent(0, channel, (MidiCommandCode) action, address, value);
                    midi.Send(note.GetAsShortMessage());
                },
                none => throw new Exception("Out device not attached")
            );
        }

        public bool IsSupported()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    for (var i = 0; i < MidiIn.NumberOfDevices; i++)
                    {
                        var name = MidiIn.DeviceInfo(i).ProductName;
                        if (name.Contains(deviceName)) { return true; }
                    }

                    for (var i = 0; i < MidiOut.NumberOfDevices; i++)
                    {
                        var name = MidiOut.DeviceInfo(i).ProductName;
                        if (name.Contains(deviceName)) { return true; }
                    }

                    break;
            }

            return true;
        }
    }
}