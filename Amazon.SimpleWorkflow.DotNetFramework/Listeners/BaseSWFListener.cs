using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Listeners
{
    public abstract class BaseSWFListener
    {
        BaseSWFListener()
        {

            // try to configure backoff interval
            long configuredBackOffInterval = 0;
            if (!long.TryParse(ConfigurationManager.AppSettings["SWFExponentialBackoffRetryInterval"], out configuredBackOffInterval))
                configuredBackOffInterval = 1000;

            retryManager = new ActionRetryManager( string.Format( "{0} for {1}", GetType().Name, Name),
                configuredBackOffInterval, MAXIMUM_BACKOFF );
            
        }


        
        protected AmazonSimpleWorkflowClient _workflowClient;
        protected string _workflowDomain;
        protected string _taskList;
        public string Name { get; set; }
        protected int numberOfCurrentlyRunningTasks = 0;
        protected bool isStopped = false;
        protected bool isPaused = false ;

        // Monitoring-related methods
        protected DateTime lastSWFResponse;
        protected long totalNumberOfTasksRunSinceLastMonitoringCheck = 0;
        protected long totalNumberOfExceptionsSinceLastMonitoringCheck = 0;
        protected long totalNumberOfTasksRun = 0;
        protected long totalNumberOfExceptions = 0;
        protected DateTime startOfFirstPolling;
        const long MAXIMUM_BACKOFF = 20000; // 20 seconds
        protected ActionRetryManager retryManager;

        public BaseSWFListener(AmazonSimpleWorkflowClient workflowClient,   string workflowDomain, string taskList) : this()
        {
            if (workflowClient == null) throw new ArgumentNullException("workflowClient");
            if (workflowDomain == null) throw new ArgumentNullException("workflowDomain");
            if (taskList == null) throw new ArgumentNullException("taskList");

            // let's set our private variables
            _workflowClient = workflowClient;
            _workflowDomain = workflowDomain;
            _taskList = taskList;
        }

        public void Start()
        {
            lastSWFResponse = DateTime.Now; // just so there is a starting point
            startOfFirstPolling = DateTime.Now;
            WorkflowLogging.Debug("Starting {0} {1}....", GetType().Name, Name);
           pollForTasks();
           WorkflowLogging.Debug("{0} {1} has been started.", GetType().Name, Name);
        }

        public void Stop()
        {
            isStopped = true;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Continue()
        {
            isPaused = false;
        }

       
        /// <summary>
        /// Polls for tasks for the given task list
        /// </summary>
        protected abstract void pollForTasks();

        public ListenerMetrics GetLatestMetrics( Stopwatch monitoringStopWatch )
        {
            // let's get the total number of tasks, and then set it to zero, using an 
            long interimTaskCount = Interlocked.Exchange(ref totalNumberOfTasksRunSinceLastMonitoringCheck, 0);
            long interimExceptionCount = Interlocked.Exchange(ref totalNumberOfExceptionsSinceLastMonitoringCheck, 0);
            
            // ok, get the elapsed time and restart
            var elapsedTime = monitoringStopWatch.Elapsed;
         

            ListenerMetrics metrics = new ListenerMetrics( this );
            metrics.TasksPerMinute = ((double)interimTaskCount) / elapsedTime.TotalMinutes;
            metrics.ExceptionsPerMinute = ((double)interimExceptionCount) / elapsedTime.TotalMinutes ;
            
            metrics.TotalNumberOfTasksExecuted = Interlocked.Read( ref totalNumberOfTasksRun );
            metrics.TotalNumberOfExceptions  =  Interlocked.Read( ref totalNumberOfExceptions );
            metrics.LastSWFResponse = (DateTime.Now - lastSWFResponse);
            metrics.Uptime = DateTime.Now - startOfFirstPolling;
            metrics.IsStopped = isStopped;
            metrics.NumberOfCurrentlyRunningTasks = numberOfCurrentlyRunningTasks;

            return metrics;

        }

        private Dictionary<Thread, bool> threadQueue = new Dictionary<Thread, bool>();
        /// <summary>
        /// Starts a new thread, and keeps a reference to it so it does not go out of scope
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="state"></param>
        protected void startThread(Thread thread, object state)
        {
            threadQueue.Add(thread, true);
            thread.Start(state);

        }

        /// <summary>
        /// Indicate that a thread is no longer needed
        /// </summary>
        /// <param name="thread"></param>
        protected void registerEndOfThread(Thread thread )
        {
            threadQueue.Remove(thread);
            

        }

        public int CurrentlyRunningTasks
        {
            get { return numberOfCurrentlyRunningTasks; }

        }

        /// <summary>
        /// Called by subclasses to update metrics when a poll result is found
        /// </summary>
        protected void recordMetricsForNewTask()
        {

            Interlocked.Increment(ref totalNumberOfTasksRunSinceLastMonitoringCheck);
            Interlocked.Increment(ref totalNumberOfTasksRun);

        }

        protected void recordMetricsForError()
        {
            Interlocked.Increment(ref totalNumberOfExceptions);
            Interlocked.Increment(ref totalNumberOfExceptionsSinceLastMonitoringCheck);
        }

        protected void recordMetricsForSWFResponse()
        {
            lastSWFResponse = DateTime.Now;
        }

        protected void recordMetricsForFirstPolling()
        {
            startOfFirstPolling = DateTime.Now;
        }

        protected string getIdentity()
        {
            return Environment.MachineName + "\\" + Process.GetCurrentProcess().ProcessName;
        }
    }
}
