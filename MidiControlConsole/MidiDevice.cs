using NAudio.Midi;

namespace MidiControl
{
    public class MidiDevice
    {
        private int device_id;

        public MidiDevice(int device_id)
        {
            this.device_id = device_id;
        }

        public void Send(byte[] data)
        {
            var dev = new MidiOut(device_id);
            dev.SendBuffer(data);
            dev.Close();
        }
    }
}
