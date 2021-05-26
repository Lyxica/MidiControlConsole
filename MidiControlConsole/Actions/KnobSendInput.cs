using System.Collections.Generic;

namespace MidiControl
{
    public class KnobSendInput
    {
        private const byte ROUTE_ID = 0xB0;

        // We'll use the middle value as a neutral pivot point. We'll ignore the value 0 as well in order to prevent an unbalanced distributuion of values.
        private const byte NEUTRAL_VALUE = 64;
        private const int SCROLL_MULTIPLIER = 5;
        private readonly byte KNOB_CONTROLLER_ID;
        private readonly User32API.ScanCodeShort left;
        private readonly MidiDevice md;
        private readonly User32API.ScanCodeShort right;


        private int lastValue;
        private int scroll_amount;
        private int wait_time = 0;

        public KnobSendInput(MidiDevice md, byte knobId, User32API.ScanCodeShort left, User32API.ScanCodeShort right)
        {
            // Setup the knobs state
            this.md = md;
            this.left = left;
            this.right = right;
            KNOB_CONTROLLER_ID = knobId;
        }

        public void update(int value)
        {
            var rel_value = value - NEUTRAL_VALUE;


            var direction = value switch
            {
                0 => left,
                127 => right,
                _ => rel_value < lastValue ? left : right
            };

            lastValue = rel_value;
            User32API.Type(new List<User32API.ScanCodeShort> {direction});
        }
    }
}