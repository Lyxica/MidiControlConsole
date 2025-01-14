﻿using System.Threading;

namespace MidiControl
{
    public class KnobScroll
    {
        private const byte ROUTE_ID = 0xB0;

        // We'll use the middle value as a neutral pivot point. We'll ignore the value 0 as well in order to prevent an unbalanced distributuion of values.
        private const byte NEUTRAL_VALUE = 64;
        private const int PULSE_TIME = 10;
        private const int SCROLL_MULTIPLIER = 5;
        private readonly byte KNOB_CONTROLLER_ID;

        private readonly MidiDevice md;

        private readonly EventWaitHandle wait_event = new EventWaitHandle(false, EventResetMode.AutoReset);
        private int current_value;
        private int scroll_amount;
        private int wait_time = 0;

        public KnobScroll(MidiDevice md, byte knobId)
        {
            // Setup the knobs state
            this.md = md;
            KNOB_CONTROLLER_ID = knobId;
            var thread = new Thread(start);
            thread.Start();
        }

        private void start()
        {
            while (true)
            {
                if (current_value == 0) { wait_event.WaitOne(); }

                Thread.Sleep(PULSE_TIME);
                User32API.Scroll(scroll_amount);
            }
        }

        public void reset()
        {
            md.Send(MidiDevice.Actions.CONTROL_CHANGE, KNOB_CONTROLLER_ID, NEUTRAL_VALUE);
            current_value = 0;
        }

        public void update(int value)
        {
            if (value == 0)
            {
                md.Send(MidiDevice.Actions.CONTROL_CHANGE, KNOB_CONTROLLER_ID, 1);
                return;
            }

            var rel_value = value - NEUTRAL_VALUE;

            if (current_value == rel_value) { return; }

            if (rel_value == 0)
            {
                current_value = 0;
                scroll_amount = 0;
                return;
            }

            scroll_amount = -rel_value * SCROLL_MULTIPLIER;
            current_value = rel_value;
            wait_event.Set();
        }
    }
}