using System.Collections.Generic;
using System.Threading;

namespace MidiControl
{
    public class DisplayShow
    {
        private static readonly Dictionary<string, byte> colors = new Dictionary<string, byte>
        {
            {"none", 0x00},
            {"white", 0x10},
            {"yellow", 0x20},
            {"cyan", 0x30},
            {"purple", 0x40},
            {"blue", 0x50},
            {"green", 0x60},
            {"red", 0x70}
        };

        private readonly MidiDevice md;

        public DisplayShow(MidiDevice md) { this.md = md; }

        public byte get_address(int row, int column) { return (byte) (row * 16 + column); }

        public void change_color(string color, byte address)
        {
            md.Send(new byte[] {0x80, address, 0});
            Thread.Sleep(25);
            md.Send(new byte[] {0x90, address, colors[color]});
        }
    }
}