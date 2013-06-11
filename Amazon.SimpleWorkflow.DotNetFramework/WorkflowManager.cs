using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Listeners;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;



namespace Amazon.SimpleWorkflow.DotNetFramework
{
    /// <summary>
    /// Responsible for coordinating access to AWS SWF
    /// </summary>
    public static class WorkflowManager
    {
        #region Dictionaries

        private static Dictionary<string, Type> _workflowDefinitions = new Dictionary<string, Type>();
        private static Dictionary<string, Type> _activityDefinitions = new Dictionary<string, Type>();
        private static AmazonSimpleWorkflowClient _workflowClient;
        private static string _domainName;
        #endregion

        private static DeciderTaskListener _deciderTaskListener;
        private static ActivityTaskListener _activityTaskListener;
        private static ActionRetryManager simpleWorkflowFoundationRetryManager;

        /// <summary>
        /// Initializes workflow access. It does this by scanning through
        /// and locating all workflow definitions and making sure that workflow
        /// types are define
        /// </summary>
        static WorkflowManager()
        {
            //    // logging
            //    ILayout layout = new PatternLayout { ConversionPattern = "%date [%thread] %-5level %logger [%ndc] - %message%newline" };
            //    FileAppender appender = new FileAppender(layout, @"c:\temp\awsWireLogging.txt", true);

            //    BasicConfigurator.Configure(appender);

            //    AmazonSimpleWorkflowConfig config = new AmazonSimpleWorkflowConfig
            //    {

            //        LogResponse = true

            //    };
            // initialize the workflow client
            //    _workflowClient = new AmazonSimpleWorkflowClient( config );

            WorkflowLogging.Debug("Initializing AWS Client...");
            _workflowClient = new AmazonSimpleWorkflowClient();
            _domainName = SimpleWorkflowFoundationSettings.Settings.Domain;

            // try to configure backoff interval
            long configuredBackOffInterval = 0;
            if (!long.TryParse(ConfigurationManager.AppSettings["SWFExponentialBackoffRetryInterval"], out configuredBackOffInterval))
                configuredBackOffInterval = 1000;

            simpleWorkflowFoundationRetryManager = new ActionRetryManager("StartWorkflow", configuredBackOffInterval, MAXIMUM_BACKOFF);

            int workerThreads, completionThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionThreads);
            WorkflowLogging.Debug("Workflow Manager Starting up... maximum number of worker threads is {0}, completion threads {1}....",
                workerThreads, completionThreads );
                
                
            WorkflowLogging.Debug("Workflow Manager: Initializing workflows....");
            initializeWorkflows();
            WorkflowLogging.Debug("Workflow Manager: Initializing activities....");
            initializeActivities();
            WorkflowLogging.Debug("Workflow Manager: Initializing monitoring....");
            initializeMonitoring();


            WorkflowLogging.Debug("Workflow Manager: Initialization complete.");
            //_workflowClient.StartWorkflowExecution(new StartWorkflowExecutionRequest().WithDomain(_domainName)
            //    .WithWorkflowId(Guid.NewGuid().ToString())
            //    .WithExecutionStartToCloseTimeout( "2222" )
            //    .WithTaskStartToCloseTimeout( "2222")
            //    .WithChildPolicy( "TERMINATE")
            //    .WithWorkflowType(new WorkflowType().WithName("Sample").WithVersion("1"))
            //    .WithTaskList(new TaskList().WithName("Sample")));

        }

        const long MAXIMUM_BACKOFF = 20000; // 20 seconds

        #region Scheduling

        private static Timer _metricsTimer;
        
        /// <summary>
        /// Initializes the monitoring timers that check the health of all listeners
        /// </summary>
        private static void initializeMonitoring()
        {
            if (!SimpleWorkflowFoundationSettings.Settings.EnableWorkflowMetrics)
                return;




            WorkflowLogging.Debug("Scheduling metrics job timer... Collection period is {0} milliseconds.", _getMetricsCollectionPeriod());
            monitoringStopWatch = new Stopwatch();
            monitoringStopWatch.Start();

            _metricsTimer = new Timer(collectMetrics, null, _getMetricsCollectionPeriod(), Timeout.Infinite);
        }

        private static int _getMetricsCollectionPeriod()
        {
            return SimpleWorkflowFoundationSettings.Settings.MetricsCollectionPeriod;
        }

        private static void collectMetrics(object state)
        {
            try
            {
                WorkflowManager.PublishMetrics();
            }
            finally
            {
                _metricsTimer.Change(_getMetricsCollectionPeriod(), Timeout.Infinite);
            }
        }

        #endregion

        public static string Domain
        {
            get { return _domainName; }

        }

        public static AmazonSimpleWorkflowClient SWFClient
        {
            get { return _workflowClient; }
        }

        public static StartWorkflowExecutionResponse StartWorkflow(string workflowTypeName,
            string workflowExecutionID, string taskList, string input, List<string> tags)
        {
            try
            {
                string workflowVersion = GetCurrentVersionOfWorkflowType(workflowTypeName);


                StartWorkflowExecutionRequest request = new StartWorkflowExecutionRequest()
                    .WithDomain(_domainName)
                    .WithWorkflowId(workflowExecutionID)
                    .WithWorkflowType(
                        new WorkflowType()
                            .WithName(workflowTypeName)
                            .WithVersion(workflowVersion))
                    .WithTaskList(GetTaskList(workflowTypeName, taskList))
                    .WithInput(WorkflowExecutionContext.PrepareInputForWorkflow(input));

                if (tags != null && tags.Count > 0)
                    request.TagList = tags;


                var resp = _workflowClient.StartWorkflowExecution(request);
                simpleWorkflowFoundationRetryManager.ResetExponentialBackoff();    // no exception encountered
                return resp;
            }
            catch (AmazonSimpleWorkflowException ex)
            {
                // did we exceed the rate?
                if (ex.Message.Contains("Rate exceeded"))
                {
                    long backOffValue = simpleWorkflowFoundationRetryManager.CalculateExponentialBackOff();
                    WorkflowLogging.Debug(
                      "While starting workflow {0} with ID {1}, SWF indicated that we've exceeded our usage rate... we'll back waiting for {2:N0} ms and trying again.", workflowTypeName, workflowExecutionID, backOffValue);
                    Thread.Sleep(TimeSpan.FromMilliseconds(backOffValue));
                    return StartWorkflow(workflowTypeName, workflowExecutionID, taskList, input, tags);

                }

                throw;
            }

        }

        public static TaskList GetTaskList(string workflowTypeName, string taskList)
        {
            // ok - we've rewritten the code so that we have a single poller; so this should now be "AllTasks";
            taskList = "AllTasks";

            

            string fullyQualifiedTaskListName;
            string prefix = "";
            if (string.IsNullOrWhiteSpace(taskList))
                fullyQualifiedTaskListName = workflowTypeName;
            else
                fullyQualifiedTaskListName = taskList;

            if (SimpleWorkflowFoundationSettings.Settings.PrefixTaskListWithComputerName &&
                 !fullyQualifiedTaskListName.StartsWith(Environment.MachineName + "-"))
                prefix = Environment.MachineName + "-";

            if ( SimpleWorkflowFoundationSettings.Settings.PrefixTaskListWithProcessName  )
                prefix += Process.GetCurrentProcess().ProcessName+ "-";


            fullyQualifiedTaskListName = prefix + fullyQualifiedTaskListName;

            return new TaskList().WithName(fullyQualifiedTaskListName);
        }

        private static Dictionary<string, string> workflowVersions = new Dictionary<string, string>();
        private static string GetCurrentVersionOfWorkflowType(string workflowName)
        {
            if (workflowName == null) throw new ArgumentNullException("workflowName");

            string version;

            if (workflowVersions.TryGetValue(workflowName, out version))
                return version;


            version = _determineVersionFromConfiguration(workflowName);

            workflowVersions[workflowName] = version;

            return version;
        }

        private static string _determineVersionFromConfiguration(string workflowName)
        {
            var config = SimpleWorkflowFoundationSettings.Settings;

            // first, check clients
            if (config.Clients != null)
                foreach (WorkflowClientConfiguration cc in config.Clients)
                    if (cc.Name == workflowName)
                        if (!string.IsNullOrWhiteSpace(cc.Version))
                            return cc.Version;
                        else
                            // ok, so the version is not specified, we have to discover it
                            return GetLatestVersionOfWorkflow(workflowName);

            // now, check workflow definition
            if (config.Workflows != null)
                foreach (WorkflowConfiguration wc in config.Workflows)
                    if (wc.Name == workflowName)
                        return wc.Version;

            // it's not found, that's ok - ask SWF
            return GetLatestVersionOfWorkflow(workflowName);
        }

        private static Dictionary<string, string> _cachedWorkflowVersions = new Dictionary<string, string>();
        public static string GetLatestVersionOfWorkflow(string workflowName)
        {
            DateTime latestDate = DateTime.MinValue;
            string currentVersion = null;

            if (_cachedWorkflowVersions.TryGetValue(workflowName, out currentVersion))
                return currentVersion;

            try
            {
                var types = SWFClient.ListWorkflowTypes(new ListWorkflowTypesRequest()
                                                            .WithDomain(Domain)
                                                            .WithName(workflowName).WithRegistrationStatus("REGISTERED"))
                    .ListWorkflowTypesResult.WorkflowTypeInfos;

                do
                {
                    foreach (var typeInfo in types.TypeInfos)
                    {
                        if (typeInfo.CreationDate > latestDate) //update
                        {
                            latestDate = typeInfo.CreationDate;
                            currentVersion = typeInfo.WorkflowType.Version;
                        }
                    }

                    if (types.NextPageToken != null)
                        types =
                            SWFClient.ListWorkflowTypes(
                                new ListWorkflowTypesRequest().WithNextPageToken(types.NextPageToken)).
                                ListWorkflowTypesResult.WorkflowTypeInfos;
                    else
                        break; // keep going until there's no more
                } while (true);
            }
            catch (Exception ex)
            {
                WorkflowLogging.Error("Unable to locate current version of workflow '{0}'. Error was:\r\n{1}",
                                      workflowName, ex);
            }

            _cachedWorkflowVersions[workflowName] = currentVersion;

            return currentVersion;

        }

        public static void Initialize()
        {
            ; // noop
        }

        static bool pausedMonitoringThread = false;
        public static void Pause()
        {
            WorkflowLogging.Debug("Pausing workflows...");

            if ( _deciderTaskListener != null ) _deciderTaskListener.Pause();
            if (_activityTaskListener != null) _activityTaskListener.Pause();
            
            if (_monitorWorkflowThatShouldAlwaysBeRunningTimer != null)
            {
                pausedMonitoringThread = true;
                _monitorWorkflowThatShouldAlwaysBeRunningTimer.Dispose();
            }
            else
                pausedMonitoringThread = false;

           
            WorkflowLogging.Debug("Workflows paused. Waiting...");
        }

        public static void Continue()
        {
            WorkflowLogging.Debug("Continuing workflow listeners...");

            if (_deciderTaskListener != null) _deciderTaskListener.Continue();
            if (_activityTaskListener != null) _activityTaskListener.Continue();

            if (pausedMonitoringThread)
                _monitorWorkflowThatShouldAlwaysBeRunningTimer =
                    new Timer(monitorWorkflowThatShouldAlwaysBeRunning,
                              null, LONG_RUNNING_WORKFLOW_INTERVAL, LONG_RUNNING_WORKFLOW_INTERVAL);

            WorkflowLogging.Debug("Workflows back online...");
        }

        public static void Shutdown()
        {
            if (_deciderTaskListener != null) _deciderTaskListener.Stop();
            if (_activityTaskListener != null) _activityTaskListener.Stop();

           
            if (_monitorWorkflowThatShouldAlwaysBeRunningTimer != null)
                _monitorWorkflowThatShouldAlwaysBeRunningTimer.Dispose();

            // now wait
            while ((_deciderTaskListener != null && _deciderTaskListener.CurrentlyRunningTasks > 0 )
                ||
                 (_activityTaskListener != null && _activityTaskListener.CurrentlyRunningTasks > 0 ))
            {
                WorkflowLogging.Debug("Waiting for tasks to complete...");
                Thread.Sleep(1000);
            }

            if (_metricsTimer != null)
                _metricsTimer.Dispose();
        }


        // variables are used to make sure workflows that should always be running stay running
        private static Timer _monitorWorkflowThatShouldAlwaysBeRunningTimer = null;
        const int LONG_RUNNING_WORKFLOW_INTERVAL = 1000 * 60;
        /// <summary>
        /// Goes through and finds all workflows, and makes sure they are defined in the domain
        /// </summary>
        private static void initializeWorkflows()
        {
            WorkflowLogging.Debug("Initializing workflows. Maximum activity thread count is: " +
                                  SimpleWorkflowFoundationSettings.Settings.MaxDeciderThreads);

            foreach (WorkflowConfiguration w in SimpleWorkflowFoundationSettings.Settings.Workflows)
            {

                // first, can we load the type?
                Type workflowType = Type.GetType(w.Type);
                string workflowName = w.Name;

                // we know this is unique since the configuration settings use "name" as a key
                _workflowDefinitions.Add(workflowName, workflowType);

                WorkflowLogging.Debug("Examining workflow type '{0}'...", workflowName);

                // now, for each workflow, see if it exists in AWS. If not, create it
                try
                {
                    var resp = _workflowClient.DescribeWorkflowType(
                        new DescribeWorkflowTypeRequest()
                            .WithDomain(_domainName)
                            .WithWorkflowType(new WorkflowType().WithName(workflowName).WithVersion(w.Version)));


                    if (resp.DescribeWorkflowTypeResult.WorkflowTypeDetail.TypeInfo.Status == "DEPRECATED")
                        throw new ApplicationException(
                            string.Format("Workflow type '{0}' version {1} is deprecated, and cannot be used.",
                                          w.Name, w.Version));

                    WorkflowLogging.Debug("Successfully located workflow '{0}'...", workflowName);

                }
                catch (AmazonSimpleWorkflowException ex)
                {
                    if (ex.ErrorCode == "UnknownResourceFault")
                    {
                        WorkflowLogging.Debug("Workflow '{0}' not found, creating...",
                                              workflowName);

                        createWorkflowFromConfiguration(w);

                        WorkflowLogging.Debug("Workflow '{0}' created successfully.",
                                              workflowName);
                    }
                    else
                    {
                        Console.WriteLine("Amazon exception: " + ex.ErrorCode + Environment.NewLine + ex);
                        throw;
                    }
                }


          


                // now, let's try to start it. If it fails, that's fine, that just means it's already running
                if (w.AlwaysKeepThisWorkflowRunning)
                {
                    WorkflowLogging.Debug("Workflow '{0}' is marked for automatic startup... attempting to start.", w.Name);

                    if (_monitorWorkflowThatShouldAlwaysBeRunningTimer == null)
                        _monitorWorkflowThatShouldAlwaysBeRunningTimer = new Timer(monitorWorkflowThatShouldAlwaysBeRunning,
                                                                                   null, LONG_RUNNING_WORKFLOW_INTERVAL, LONG_RUNNING_WORKFLOW_INTERVAL);

                    try
                    {
                        WorkflowManager.StartWorkflow(w.Name, _getWorkflowExecutionIDForAlwaysRunningWorkflow(w), null, null, null);
                        WorkflowLogging.Debug("Workflow '{0}' started successfully.", w.Name);
                    }
                    catch (AmazonSimpleWorkflowException ex)
                    {
                        if (ex.ErrorCode == "WorkflowExecutionAlreadyStartedFault")
                            WorkflowLogging.Debug("Workflow '{0}' is already running, no need to restart.", w.Name );
                        else
                            WorkflowLogging.Error("ERROR starting workflow: {0}\r\n\r\n{1}",
                                w.Name, ex);
                    }

                }
            }

            if (SimpleWorkflowFoundationSettings.Settings.Workflows.Count > 0)
            {

                // we need to create a listener, then add it as a member so it doesn't break
                _deciderTaskListener = new DeciderTaskListener(_workflowClient, _domainName,
                                                               GetTaskList(null, null).Name);
                ;
                _deciderTaskListener.Start();
            }

        }



        private static string _getWorkflowExecutionIDForAlwaysRunningWorkflow(WorkflowConfiguration w)
        {

            string id = w.Name;
            string prefix = "";
            if (SimpleWorkflowFoundationSettings.Settings.PrefixTaskListWithComputerName )
                prefix = Environment.MachineName + "-";

            if (SimpleWorkflowFoundationSettings.Settings.PrefixTaskListWithProcessName)
                prefix += Process.GetCurrentProcess().ProcessName + "-";


            id = prefix + id;

            return id;
        }

        private static void monitorWorkflowThatShouldAlwaysBeRunning(object state)
        {
            // find all workflows that should always be running
            foreach (WorkflowConfiguration wc in SimpleWorkflowFoundationSettings.Settings.Workflows)
            {
                if (!wc.AlwaysKeepThisWorkflowRunning) continue;

                // check - is this running?
                try
                {
                    StartWorkflow(wc.Name, _getWorkflowExecutionIDForAlwaysRunningWorkflow(wc), null, null, null);

                    // if no exception is thrown, then the workflow wasn't running
                    WorkflowLogging.Debug("Workflow '{0}' was found to not be running and has been successfully restarted.", wc.Name);
                }
                catch
                {
                    // ok, it was running - no problem
                }
            }
        }

        private static void createWorkflowFromConfiguration(WorkflowConfiguration w)
        {
            var request = new RegisterWorkflowTypeRequest().WithDomain(_domainName).WithName(w.Name)
                .WithVersion(w.Version)
                .WithDefaultChildPolicy(w.DefaultChildPolicy)
                 .WithDefaultTaskList(GetTaskList(w.Name, w.DefaultTaskList))
                 .WithDescription(w.Description);

            if (w.DefaultExecutionStartToCloseTimeout != null)
                request.DefaultExecutionStartToCloseTimeout = w.DefaultExecutionStartToCloseTimeout.ToString();

            if (w.DefaultTaskStartToCloseTimeout != null)
                request.DefaultTaskStartToCloseTimeout = w.DefaultTaskStartToCloseTimeout.ToString();

            _workflowClient.RegisterWorkflowType(request);


        }

        private static void createActivityFromConfiguration(ActivityConfiguration w)
        {
            var request = new RegisterActivityTypeRequest()
                .WithDomain(_domainName)
                .WithName(w.Name)
                .WithVersion(w.Version)
                .WithDefaultTaskList(GetTaskList(w.Name, w.DefaultTaskList))
                .WithDescription(w.Description);

            if (w.HeartbeatTimeout != null)
                request.DefaultTaskHeartbeatTimeout = w.HeartbeatTimeout.ToString();

            if (w.TaskScheduleToCloseTimeout != null)
                request.DefaultTaskScheduleToCloseTimeout = w.TaskScheduleToCloseTimeout.ToString();

            if (w.TaskScheduleToStartTimeout != null)
                request.DefaultTaskScheduleToStartTimeout = w.TaskScheduleToStartTimeout.ToString();

            if (w.TaskStartToCloseTimeout != null)
                request.DefaultTaskStartToCloseTimeout = w.TaskStartToCloseTimeout.ToString();

            _workflowClient.RegisterActivityType(request);
        }


        /// <summary>
        /// Goes through and finds all activities, and makes sure they are defined in the domain
        /// </summary>
        private static void initializeActivities()
        {
            WorkflowLogging.Debug("Initializing activities. Maximum activity thread count is: " +
                                  SimpleWorkflowFoundationSettings.Settings.MaxActivityThreads);

            foreach (ActivityConfiguration a in SimpleWorkflowFoundationSettings.Settings.Activities)
            {

                // first, can we load the type?
                Type activityType = Type.GetType(a.Type);
                string activityName = a.Name;

                // we know this is unique since the configuration settings use "name" as a key
                _activityDefinitions.Add(activityName, activityType);


                WorkflowLogging.Debug("Examining activity type '{0}'...", activityName);

                //now, for each activity, see if it exists in AWS. If not, create it
                try
                {
                    var resp = _workflowClient.DescribeActivityType(
                       new DescribeActivityTypeRequest()
                           .WithDomain(_domainName)
                           .WithActivityType(new ActivityType().WithName(activityName).WithVersion(a.Version)));


                    if (resp.DescribeActivityTypeResult.ActivityTypeDetail.TypeInfo.Status == "DEPRECATED")
                        throw new ApplicationException(string.Format("Activity type '{0}' version {1} is deprecated, and cannot be used.",
                            a.Name, a.Version));

                    WorkflowLogging.Debug("Successfully located activity '{0}'...", activityName);

                }
                catch (AmazonSimpleWorkflowException ex)
                {
                    if (ex.ErrorCode == "UnknownResourceFault")
                    {
                        WorkflowLogging.Debug("Activity '{0}' not found, creating...",
                                             activityName);

                        createActivityFromConfiguration(a);

                        WorkflowLogging.Debug("Activity '{0}' created successfully.",
                                              activityName);
                    }
                    else
                        throw;
                }

                


            }

            if (SimpleWorkflowFoundationSettings.Settings.Activities.Count > 0)
            {
                // we need to create a listener, then add it as a member so it doesn't break
                _activityTaskListener = new ActivityTaskListener(_workflowClient, _domainName,
                                                                 GetTaskList(null, null).Name);
                ;
                _activityTaskListener.Start();
            }

        }


        public static ActivityConfiguration FindActivityByType(Type type)
        {
            foreach (var entry in _activityDefinitions)
                if (entry.Value == type)
                    return SimpleWorkflowFoundationSettings.Settings.FindActivity(entry.Key);

            throw new ApplicationException("Unable to find activity name for " + type);
        }





        public static WorkflowExecutionInfo WaitUntilWorkflowCompletes(string workflowId, string runId)
        {
            // we put this in a big try catch because if the workflow doesn't exist, an exception will be thrown
            try
            {
                do
                {
                    var theExecution = new WorkflowExecution().WithRunId(runId).WithWorkflowId(workflowId);
                    var response =
                        _workflowClient.DescribeWorkflowExecution(new DescribeWorkflowExecutionRequest().WithDomain(
                            _domainName)
                                                                      .WithExecution(
                                                                          theExecution));

                    var describeWorkflowExecutionResult = response.DescribeWorkflowExecutionResult;
                    var workflowExecutionInfo = describeWorkflowExecutionResult.WorkflowExecutionDetail.ExecutionInfo;

                    if (workflowExecutionInfo.ExecutionStatus == WorkflowExecutionStatus.Closed)
                    {
                        // ok, we're done. Question is, did it fail?
                        if (workflowExecutionInfo.CloseStatus == WorkflowExecutionCloseStatus.Failed)
                        {
                            // let's find out why
                            var ev = WorkflowExecutionContext.FindMostRecentEvent(theExecution,
                                                                                  x =>
                                                                                  x.
                                                                                      WorkflowExecutionFailedEventAttributes !=
                                                                                  null);
                            if (ev != null) // can we find out what the problem is??
                            {
                            

                                // throw the general exception
                                throw new SimpleWorkflowDotNetFrameworkException( ev.WorkflowExecutionFailedEventAttributes.Reason, ev.WorkflowExecutionFailedEventAttributes.Details);
                            }
                        }

                        //if (workflowExecutionInfo.CloseStatus == WorkflowExecutionCloseStatus.Failed ||
                        //    workflowExecutionInfo.CloseStatus == WorkflowExecutionCloseStatus.Cancelled ||
                        //    workflowExecutionInfo.CloseStatus == WorkflowExecutionCloseStatus.TimedOut ||
                        //    workflowExecutionInfo.CloseStatus == WorkflowExecutionCloseStatus.Terminated)
                        //    throw new ApplicationException("Workflow has failed with status " +
                        //                                   workflowExecutionInfo.CloseStatus);

                        return workflowExecutionInfo; // ok, the workflow is done, it's over
                    }


                    new System.Threading.ManualResetEvent(false).WaitOne(1000);
                } while (true);
            }
            catch (AmazonSimpleWorkflowException ex )
            {
                WorkflowLogging.Error("Error occurred while waiting for workflow '{0}' to complete. \r\n\r\n{1}",
                    workflowId, ex);
            }

            return null;
        }


        #region Metrics & Monitoring

        /// <summary>
        /// Used to collect metrics
        /// </summary>
        private static Stopwatch monitoringStopWatch;

        public delegate void MetricsPublishedEventHandler(List<ListenerMetrics> metrics);

        public static event MetricsPublishedEventHandler MetricsPublished;

        /// <summary>
        /// Goes through all of the tasks and collects and publishes metrics
        /// </summary>
        public static void PublishMetrics()
        {
            List<ListenerMetrics> metrics = new List<ListenerMetrics>();

            metrics.Add( _deciderTaskListener.GetLatestMetrics(monitoringStopWatch));
            metrics.Add( _activityTaskListener.GetLatestMetrics(monitoringStopWatch));

            monitoringStopWatch.Restart();  // restart the clock

            // if there are event subscribers, publish
            try
            {
                if (MetricsPublished != null)
                    MetricsPublished(metrics);
            }
            catch (Exception ex)
            {
                WorkflowLogging.Error("Error publishing metrics: " + ex);
            }

        }
        #endregion
    }
}
