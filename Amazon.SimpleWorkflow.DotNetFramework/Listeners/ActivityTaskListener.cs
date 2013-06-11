using System;
using System.Diagnostics;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;


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
            
            int maxConcurrentExecutionsPerInstance = SimpleWorkflowFoundationSettings.Settings.MaxActivityThreads ;



            while (numberOfCurrentlyRunningTasks >= maxConcurrentExecutionsPerInstance)
                Thread.Sleep(1000); // just wait until we can start polling


            _workflowClient.BeginPollForActivityTask(new PollForActivityTaskRequest()
                                                     .WithDomain(_workflowDomain)
                                                     .WithTaskList(WorkflowManager.GetTaskList(null, _taskList))
                                                      .WithIdentity(getIdentity())
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
                if (!string.IsNullOrWhiteSpace(resp.PollForActivityTaskResult.ActivityTask.TaskToken))
                // we don't have a timeout - we have an actual task
                {

                    Interlocked.Increment(ref numberOfCurrentlyRunningTasks); // need to use thread safe increments here

                    
                    Thread t = new Thread(new ParameterizedThreadStart(_runActivityProcessAsync));
                    startThread(t, resp.PollForActivityTaskResult.ActivityTask);
                    
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
                if (task == null) return;

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
                IWorkflowActivity workflowActivity = Activator.CreateInstance(_relevantType) as IWorkflowActivity;

                if (workflowActivity == null)
                {
                    //LogWithContext.Debug("Workflow class '{0}' does not implement IWorkflowActivity.", Name);
                    return;
                }

                WorkflowExecutionContext.InitializeThread(task); // setup the executon context
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
                    Interlocked.Decrement(ref numberOfCurrentlyRunningTasks); // need to use thread safe decrements here
                    registerEndOfThread(Thread.CurrentThread);
                }
                catch
                {
                    // this CANNOT fail or it will bring down the entire application
                }
            }
        }
    }
}
