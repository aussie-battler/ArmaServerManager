using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArmaServerManager
{
    public class ServerSchedule
    {
        public bool Enabled = false;

        public List<ScheduledEvent> ServerEvents = new List<ScheduledEvent>();

        private Timer timer = new Timer();

        public ServerSchedule()
        {
            timer.AutoReset = true;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            timer.Interval = 500;
        }
        public ServerSchedule(List<ScheduledEvent> evts)
        {
            ServerEvents = evts;
            timer.AutoReset = true;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            timer.Interval = 500; 
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Enabled)
            {
                HandleEventList();
            }
        }

        private void HandleEventList()
        {
            for (int i = ServerEvents.Count - 1; i >= 0; i--)
            {
                ScheduledEvent item = ServerEvents[i];
                if (item.Scheduletype == ScheduleType.Once)
                {
                    if (item.EvtDate.Ticks < DateTime.Now.Ticks)
                    {
                        ExecuteEvent(item);
                        ServerEvents.RemoveAt(i);
                    }

                }

                else if(item.Scheduletype == ScheduleType.Time)
                {
                    DateTime currentDate = DateTime.Now;
                    if (item.EvtDate.Hour >= currentDate.Hour && item.EvtDate.Minute >= currentDate.Minute && item.EvtDate.Second >= currentDate.Second)
                    {
                        if (item.LastExec.Day != currentDate.Day)
                        {
                            ExecuteEvent(item);
                            item.LastExec = currentDate;
                        }
                    }
                }

                else if (item.Scheduletype == ScheduleType.Interval)
                {
                    if (new TimeSpan(DateTime.Now.Ticks - item.LastExec.Ticks).Seconds > item.Interval)
                    {
                        ExecuteEvent(item);
                        item.LastExec = DateTime.Now;
                    }
                }
            }
        }

        private void ExecuteEvent(ScheduledEvent evt)
        {
            Console.WriteLine("Executed event: {0}", evt.Description);
        }
    }

    [Serializable]
    public class ScheduledEvent
    {
        public string Description;
        public DateTime EvtDate;
        public EventType EvtType;
        public ScheduleType Scheduletype;
        public DateTime LastExec;
        public int Interval;
        public string MessageToServer;

        public ScheduledEvent(string desc, string msg, DateTime date, EventType evttype, ScheduleType scheduletype, int interval = 99999)
        {
            Description = desc;
            EvtDate = date;
            EvtType = evttype;
            Scheduletype = scheduletype;
            LastExec = DateTime.Now;
            Interval = interval;
            MessageToServer = msg;
        }
    }

    public enum ScheduleType
    {
        Interval,
        Once,
        Time
    };
    public enum EventType
    {
        STOP,
        START
    }
}
