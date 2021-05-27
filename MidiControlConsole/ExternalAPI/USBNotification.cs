using System;
using System.Management;
using System.Timers;

namespace MidiControl.ExternalAPI
{
    internal class USBNotification
    {
        private readonly Timer _timer;

        public USBNotification()
        {
            var query = new WqlEventQuery();
            query.EventClassName = "__InstanceCreationEvent";
            query.WithinInterval = new TimeSpan(0, 0, 2);
            query.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
            var manager = new ManagementEventWatcher(query);
            manager.EventArrived += ManagerOnEventArrived;
            manager.Start();

            _timer = new Timer {AutoReset = false, Enabled = false, Interval = 1};
            _timer.Elapsed += TimerOnElapsed;
        }

        public event Action OnNewUSBDevice;

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            OnNewUSBDevice?.Invoke();
        }

        private void ManagerOnEventArrived(object sender, EventArrivedEventArgs e) { _timer.CleanStart(); }
    }

    internal static class TimerExtension
    {
        public static void CleanStart(this Timer timer)
        {
            timer.Enabled = false;
            timer.Enabled = true;
        }
    }
}