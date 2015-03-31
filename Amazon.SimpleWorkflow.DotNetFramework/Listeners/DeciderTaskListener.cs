using System;
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
    /// This class is responsible for listening for tasks from the workflow decider
    /// </summary>
    public class DeciderTaskListener : BaseSWFListener
    {


        public DeciderTaskListener(AmazonSimpleWorkflowClient workflowClient,
            string workflowDomain, string taskList)
            : base(workflowClient, workflowDomain, taskList)
        {
            Name = taskList;
        }



        protected override void pollForTasks()
        {
            if (isStopped)
            {
                WorkflowLogging.Debug("Decision Task Listener '{0}' stopped. Exiting", this.Name);
                return; // done. Nothing to do
            }

            while (isPaused)
                Thread.Sleep(1000);     // if the thread is paused, hold on

            // do we have a throttle?

            int maxConcurrentExecutionsPerInstance = SimpleWorkflowFoundationSettings.Settings.MaxDeciderThreads ;
            
           
            while (numberOfCurrentlyRunningTasks >= maxConcurrentExecutionsPerInstance)
                Thread.Sleep(1000); // just wait until we can start polling
            

            var taskList = WorkflowManager.FormatTaskListAsNecessary(_taskList);
            _workflowClient.BeginPollForDecisionTask(new PollForDecisionTaskRequest()
            {
                Domain = (_workflowDomain),
                TaskList = (taskList),
                Identity = (getIdentity())
            },pollingCallBack, null);
        }

        private void pollingCallBack(IAsyncResult ar)
        {
            try
            {

                recordMetricsForSWFResponse();

                // let's get the result
                var resp = _workflowClient.EndPollForDecisionTask(ar);

                retryManager.ResetExponentialBackoff(); // we didn't encounter an exception

                // is it a timeout?
                if (!string.IsNullOrWhiteSpace(resp.DecisionTask.TaskToken))
                // we don't have a timeout - we have an actual task
                {

                    Interlocked.Increment(ref numberOfCurrentlyRunningTasks); // need to use thread safe increments here
                    Thread t = new Thread(new ParameterizedThreadStart(_runDecisionProcessAsync));
                    t.IsBackground = true;
                    startThread(t, resp.DecisionTask);
                    //ThreadPool.QueueUserWorkItem(_runDecisionProcessAsync, resp.PollForDecisionTaskResult.DecisionTask );

                    // update internal metrics
                    recordMetricsForNewTask();

                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The underlying connection was closed"))
                    WorkflowLogging.Error(
                        "Connection reset while polling for decider task in {0}. No problem though... restarting polling...",
                        Name);
                else if (ex.Message.Contains("ate exceeded"))
                {
                    long backOffValue = retryManager.CalculateExponentialBackOff();
                    WorkflowLogging.Debug(
                        "While polling for decider {0}, SWF indicated that we've exceeded our usage rate... we'll back waiting for {1:N0} ms and trying again.",
                        Name,
                        backOffValue);
                    Thread.Sleep(TimeSpan.FromMilliseconds(backOffValue));

                }
                else
                    WorkflowLogging.Error(
                        "Fatal exception in decider task listener '{0}'... resuming polling....\r\n\r\n{1}",
                        Name, ex.ToString());

                recordMetricsForError();
            }
            finally
            {
                pollForTasks();
            }



        }



        private void _runDecisionProcessAsync(object state)
        {
            try
            {
                var decisionTask = (DecisionTask)state;
                if (decisionTask == null) return;

                // ok, we don't have a timeout
                string decisionType = decisionTask.WorkflowType.Name;
                var workflowConfiguration = SimpleWorkflowFoundationSettings.Settings.FindWorkflow(decisionType);

                if (workflowConfiguration == null)
                {
                    WorkflowLogging.Error("Decision type '{0}' has no configuration - cannot run decider.", decisionType);
                    return; // shouldn't happen, we vetted this out
                }

                Type _relevantType = Type.GetType(workflowConfiguration.Type);

                if (_relevantType == null)
                {
                    WorkflowLogging.Error("Unable to locate decision type '{0}' - cannot run decider.", decisionType);
                    return; // shouldn't happen, we vetted this out
                }

                IWorkflowDecider decider = Container.GetOrCreateInstance(_relevantType) as IWorkflowDecider;

                if (decider == null)
                {
                    //LogWithContext.Debug("Workflow class '{0}' does not implement IWorkflowDecider.", Name);
                    return;
                }

                WorkflowExecutionContext.InitializeThread(decisionTask);        // setup the executon context
                decider.Decide();

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
