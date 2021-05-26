using System;
using NAudio.Midi;
using OneOf;

namespace MidiControl
{
    public class MidiDevice
    {
        public enum Actions
        {
            NOTE_ON = 0x90,
            NOTE_OFF = 0x80,
            CONTROL_CHANGE = 0xB0
        }

        private OneOf<MidiOut> _device;

        public MidiDevice(int deviceId)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    _device = new MidiOut(deviceId);
                    break;

                default:
                    throw new NotImplementedException("API for OS not implemented");
            }
        }

        public void Send(Actions action, int address, int value) { Send(action, address, value, 1); }

        public void Send(Actions action, int address, int value, int channel)
        {
            _device.Switch(
                midi =>
                {
                    var note = new NoteEvent(0, channel, (MidiCommandCode) action, address, value);
                    midi.Send(note.GetAsShortMessage());
                });
        }
    }
}