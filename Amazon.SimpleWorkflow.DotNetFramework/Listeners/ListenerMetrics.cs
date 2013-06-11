using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Listeners
{
    /// <summary>
    /// Represents key metrics that the listeners publish
    /// </summary>
    public class ListenerMetrics
    {
        public ListenerMetrics(BaseSWFListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");
            ListenerName = listener.Name;
            ListenerType = listener.GetType();
        }

        public string ListenerName { get; set; }
        public Type ListenerType { get; set; }
        public double ExceptionsPerMinute { get; set; }
        public double TasksPerMinute { get; set; }
        public long TotalNumberOfTasksExecuted { get; set; }
        public long TotalNumberOfExceptions { get; set; }
        public TimeSpan Uptime { get; set; }

        public bool IsStopped { get; set; }

        public TimeSpan LastSWFResponse { get; set; }

        public int NumberOfCurrentlyRunningTasks { get; set; }
    }
}
