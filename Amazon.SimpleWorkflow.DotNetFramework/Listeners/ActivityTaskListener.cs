using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;
using MemberSuite;


namespace Amazon.SimpleWorkflow.DotNetFramework.Listeners
{
    /// <summary>
    /// This class is responsible for listening for tasks from the workflow Activity
    /// </summary>
    public class ActivityTaskListener : BaseSWFListener
    {

        public ActivityTaskListener(AmazonSimpleWorkflowClient workflowClient, string workflowDomain, string taskList)
            : base(workflowClient, workflowDomain, taskList)
        {

        }

        protected override void pollForTasks()
        {
            if (isStopped)
            {
                WorkflowLogging.Error("Activity Task Listener '{0}' stopped. Exiting", this.Name);
                return; // done. Nothing to do
            }

            while (isPaused)
                Thread.Sleep(1000);  // if the thread is paused, hold on

            // do we have a throttle?

            int maxConcurrentExecutionsPerInstance = SimpleWorkflowFoundationSettings.Settings.MaxActivityThreads;


            int counter = 0;
            while (numberOfCurrentlyRunningTasks >= maxConcurrentExecutionsPerInstance)
            {
                counter++;
                Thread.Sleep(1000); // just wait until we can start polling
                if (counter % 10 == 0)
                    WorkflowLogging.Debug(
                        "Task listener {0} is waiting... currently {1} tasks are running and a maximum of {2} threads can be active. Thread count: {3}",
                        Name,
                        numberOfCurrentlyRunningTasks, maxConcurrentExecutionsPerInstance, this.threadQueue.Count);
            }


            _workflowClient.BeginPollForActivityTask(new PollForActivityTaskRequest()
            {
                Domain = (_workflowDomain),
                TaskList = (WorkflowManager.FormatTaskListAsNecessary(_taskList)),
                Identity = (getIdentity())
            }
                                                     ,
                                                 pollingCallBack, null);
        }

        private void pollingCallBack(IAsyncResult ar)
        {

            try
            {
                recordMetricsForSWFResponse();

                // let's get the result
                var resp = _workflowClient.EndPollForActivityTask(ar);

                retryManager.ResetExponentialBackoff(); // we didn't encounter an error

                // is it a timeout?
                var taskToken = resp.ActivityTask.TaskToken;
                if (!string.IsNullOrWhiteSpace(taskToken))
                // we don't have a timeout - we have an actual task
                {


                    var newRunningTasks = Interlocked.Increment(ref numberOfCurrentlyRunningTasks); // need to use thread safe increments here

                    if (ConfigurationManager.AppSettings["VerboseSWFLogging"] == "true")
                        WorkflowLogging.Debug("WFM: Incrementing running tasks to {0} for task token ending {1}. Thread count: {2}",
                                              newRunningTasks, taskToken.Substring(taskToken.Length - 8, 6), threadQueue.Count);

                    Thread t = new Thread(new ParameterizedThreadStart(_runActivityProcessAsync));
                    t.IsBackground = true;
                    startThread(t, resp.ActivityTask);
                    //ThreadPool.QueueUserWorkItem(_runActivityProcessAsync, resp.PollForActivityTaskResult.ActivityTask);


                    // update the internal metrics
                    recordMetricsForNewTask();
                }


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The underlying connection was closed"))
                    WorkflowLogging.Error("Connection reset while polling for activity task in {0}. No problem though... restarting polling...", Name);
                else if (ex.Message.Contains("ate exceeded"))
                {
                    long backOffValue = retryManager.CalculateExponentialBackOff();
                    WorkflowLogging.Debug(
                        "While polling for activity {0}, SWF indicated that we've exceeded our usage rate... we'll back waiting for {1:N0} ms and trying again.",
                        Name,
                        backOffValue);
                    Thread.Sleep(TimeSpan.FromMilliseconds(backOffValue));

                }
                else
                    WorkflowLogging.Error("Fatal exception in activity task listener '{0}'... restarting polling...\r\n\r\n{1}",
                                          Name, ex.ToString());

                recordMetricsForError();

            }
            finally
            {
                pollForTasks();
            }

        }

        private void _runActivityProcessAsync(object state)
        {
            try
            {
                ActivityTask task = (ActivityTask)state;
                if (task == null)
                {
                    WorkflowLogging.Debug("ERROR: No task passed to activity... exiting.");
                    return;
                }

                string activityType = task.ActivityType.Name;
                var activityConfiguration = SimpleWorkflowFoundationSettings.Settings.FindActivity(activityType);

                if (activityConfiguration == null)
                {
                    WorkflowLogging.Error("Activity type '{0}' has no configuration - cannot run decider.", activityType);
                    return; // shouldn't happen, we vetted this out
                }

                Type _relevantType = Type.GetType(activityConfiguration.Type);

                if (_relevantType == null)
                {
                    WorkflowLogging.Error("Unable to locate activity type '{0}' - cannot run decider.", activityType);
                    return; // shouldn't happen, we vetted this out
                }

                // ok, we don't have a timeout
                IWorkflowActivity workflowActivity = Container.GetOrCreateInstance(_relevantType) as IWorkflowActivity;

                if (workflowActivity == null)
                {
                    WorkflowLogging.Debug("Workflow class '{0}' does not implement IWorkflowActivity. Exiting...", Name);
                    return;
                }

                WorkflowExecutionContext.InitializeThread(task); // setup the executon context

                if (ConfigurationManager.AppSettings["VerboseSWFLogging"] == "true")
                    WorkflowLogging.Debug("WRM: Activity thread initialized, beginning task workflow execution #{0}, run {1}, token {2}.",
                        task.WorkflowExecution.WorkflowId, task.WorkflowExecution.RunId,
                                          workflowActivity.GetType().Name);

                workflowActivity.Process();


            }
            catch (Exception ex)
            {
                WorkflowLogging.Debug("Async activity exception" + ex);
                recordMetricsForError();
            }
            finally
            {
                try
                {
                    var newRunningTasks = Interlocked.Decrement(ref numberOfCurrentlyRunningTasks); // need to use thread safe decrements here

                    ActivityTask task = (ActivityTask)state;

                    var taskToken = task.TaskToken;

                    registerEndOfThread(Thread.CurrentThread);

                    if (ConfigurationManager.AppSettings["VerboseSWFLogging"] == "true")
                        WorkflowLogging.Debug("WFM: Decrementing running tasks to {0} for task token ending {1}. Thread count: {2}",
                                              newRunningTasks, taskToken.Substring(taskToken.Length - 8, 6), threadQueue.Count);


                }
                catch (Exception ex)
                {
                    // this CANNOT fail or it will bring down the entire application
                    Console.WriteLine("CRITICAL ERROR:" + ex);
                }
            }
        }
    }
}
