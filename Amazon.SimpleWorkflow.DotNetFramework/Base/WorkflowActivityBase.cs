using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;



namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    public abstract class WorkflowActivityBase : IWorkflowActivity 
    {

        const int DEFAULT_HEARTBEAT_INTERVAL = 10000;

        protected virtual int getHeartbeatInterval()
        {
            return DEFAULT_HEARTBEAT_INTERVAL;
        }

        private Timer _heartbeatTimer;
      
        public void Process()
        {
            var taskToProcess = WorkflowExecutionContext.CurrentActivityTask;
            if (taskToProcess == null) throw new ArgumentNullException("taskToProcess");
            try
            {



                scheduleHeartbeatTimer( WorkflowExecutionContext.TaskToken  );

                processWorkflowActivity();

                if (!hasCompletionBeenSignalled)
                    complete(null ); // complete the activity automatically; without this, the workflow will hang
            }
            catch (SimpleWorkflowDotNetFrameworkException cex)
            {
                try
                {
                    WorkflowManager.SWFClient.RespondActivityTaskFailed(new RespondActivityTaskFailedRequest()
                                                                            .WithTaskToken(taskToProcess.TaskToken)
                                                                            .WithReason(cex.ErrorCode )
                                                                            .WithDetails(cex.Message));

                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Unable to report failed activity: " + ex2 + "\n\nThe underlying exception\n\n" + cex);
                }
            }
            catch (Exception ex)
            {
                // this CANNOT cause an exception or it will fail threads and unit tests
                try
                {
                    WorkflowManager.SWFClient.RespondActivityTaskFailed(new RespondActivityTaskFailedRequest()
                                                                            .WithTaskToken(taskToProcess.TaskToken)
                                                                            .WithReason("Exception").WithDetails(
                                                                                ex.ToString()));
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Unable to report failed activity: " + ex2 + "\n\nThe underlying exception\n\n" + ex);
                }


            }
            finally
            {
                activityIsCompleted = true;
                if (_heartbeatTimer != null)
                    _heartbeatTimer.Dispose(); // stop the timer
            }

        }

        private bool activityIsCompleted = false;

        private void scheduleHeartbeatTimer( string taskToken)
        {
            // we have to pass the task token in - we can't get it from the thread
            if (activityIsCompleted) return;    // no more late heartbeats!

            if (_heartbeatTimer == null) // create a new one
                _heartbeatTimer = new Timer(sendHeartBeat, taskToken, getHeartbeatInterval(),
                                            Timeout.Infinite);
            else
                _heartbeatTimer.Change(getHeartbeatInterval(), Timeout.Infinite);   // just change it
        }

        private void sendHeartBeat(object state)
        {
            if (activityIsCompleted) return;    // no more late heartbeats!
            string taskToken = (string)state;
            
            try
            {


                WorkflowManager.SWFClient.RecordActivityTaskHeartbeat(new RecordActivityTaskHeartbeatRequest()
                .WithTaskToken(taskToken));

                // only schedule the heartbeat if it was successful
                scheduleHeartbeatTimer(taskToken);
            }
            catch (AmazonSimpleWorkflowException ex)
            {
                WorkflowLogging.Debug(
                    "Activity '{0}' experienced an Amazon SWF exception sending heartbeat\r\nTask Token: {3}\r\nCode: {1}\r\nMessage: {2}",
                    this.GetType().Name, ex.ErrorCode, ex.Message, taskToken );
                
            }
            catch (Exception ex)
            {
                WorkflowLogging.Debug("Activity '{0}' experienced an error sending heartbeat\r\n\r\n{1}",
                    this.GetType().Name, ex);
            }
            
        }

        protected abstract void processWorkflowActivity();

        protected bool hasCompletionBeenSignalled = false;
        protected RespondActivityTaskCompletedResponse complete( string result)
        {
            hasCompletionBeenSignalled = true;
            if (WorkflowExecutionContext.CurrentActivityTask == null) return null;
            return WorkflowManager.SWFClient.RespondActivityTaskCompleted(new RespondActivityTaskCompletedRequest().WithTaskToken(WorkflowExecutionContext.CurrentActivityTask.TaskToken)
                .WithResult(result));
        }

        /// <summary>
        /// Reports to the workflow that this task has failed
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <param name="details">The details.</param>
        /// <returns>RespondActivityTaskCompletedResponse.</returns>
        protected RespondActivityTaskFailedResponse fail(string reason, string details)
        {
            hasCompletionBeenSignalled = true;
            if (WorkflowExecutionContext.CurrentActivityTask == null) return null;
            return WorkflowManager.SWFClient.RespondActivityTaskFailed(new RespondActivityTaskFailedRequest().WithTaskToken(WorkflowExecutionContext.CurrentActivityTask.TaskToken)
                .WithReason(reason).WithDetails(details));
        }

       
    }
}
