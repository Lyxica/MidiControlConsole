using System.Diagnostics;
using System.Timers;

namespace MidiControl
{
    internal class ShutdownBtn
    {
        public readonly byte address;
        private readonly DisplayShow ds;
        private readonly Timer timer;
        private int state;

        public ShutdownBtn(DisplayShow ds, byte address)
        {
            timer = new Timer {Interval = 3000, AutoReset = false};
            timer.Elapsed += (sender, args) => Reset();
            this.ds = ds;
            this.address = address;


            Reset();
        }

        private void Reset()
        {
            ds.change_color("blue", address);
            state = 0;
            timer.Enabled = false;
        }

        public void Next()
        {
            switch (state)
            {
                case 0:
                    ds.change_color("yellow", address);
                    timer.Enabled = true;
                    break;
                case 1:
                    ds.change_color("red", address);

                    // "Resetting" the timer
                    timer.Enabled = false;
                    timer.Enabled = true;
                    break;
                case 2:
                    Process.Start("shutdown.exe", "-s -t 00");
                    break;
            }

            state += 1;
        }
    }
}