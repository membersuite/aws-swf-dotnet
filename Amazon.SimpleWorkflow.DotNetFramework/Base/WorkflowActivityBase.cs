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
            var interval =  DEFAULT_HEARTBEAT_INTERVAL;

            // let's randomize it a bit
            var baseInterval = interval / 2;
            var randomVariance = new Random().Next(interval);   // get a random number between 0 and 10000

            var inter =  baseInterval + randomVariance;       // in the case of a 10 sec interval, this yields and interval betwen 5 and 15

            //WorkflowLogging.Debug("{0}: calculated heartbeat interval: {1}", GetType().Name, inter );

            return inter;
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
                    Complete(null); // complete the activity automatically; without this, the workflow will hang
            }
            catch (SimpleWorkflowDotNetFrameworkException cex)
            {
                try
                {
                    WorkflowManager.SWFClient.RespondActivityTaskFailed(new RespondActivityTaskFailedRequest() {
                                                                            TaskToken = taskToProcess.TaskToken,
                                                                            Reason = cex.ErrorCode,
                                                                            Details = cex.Message });

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
                        {
                            TaskToken = taskToProcess.TaskToken,
                            Reason = "Exception",
                            Details = ex.ToString()
                        });
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
            //WorkflowLogging.Debug("{0}: Scheduling heartbeat", GetType().Name);
            // we have to pass the task token in - we can't get it from the thread
            if (activityIsCompleted) return;    // no more late heartbeats!

            if (_heartbeatTimer == null) // create a new one
                _heartbeatTimer = new Timer(sendHeartBeat, taskToken, getHeartbeatInterval(),
                                            Timeout.Infinite);
            else
                _heartbeatTimer.Change(getHeartbeatInterval(), Timeout.Infinite);   // just change it

            //WorkflowLogging.Debug("{0}: Scheduling heartbeat successfully", GetType().Name);
        }

        private void sendHeartBeat(object state)
        {
            //WorkflowLogging.Debug("{0}: Sending heartbeat #1", GetType().Name);
            if (activityIsCompleted) return;    // no more late heartbeats!
            string taskToken = (string)state;

            //WorkflowLogging.Debug("{0}: send heartbeat #2", GetType().Name);
            try
            {


                WorkflowManager.SWFClient.RecordActivityTaskHeartbeat(new RecordActivityTaskHeartbeatRequest()
                {
                    TaskToken = (taskToken)
                });

                //WorkflowLogging.Debug("{0}: heartbeat sent", GetType().Name);

                // always send the heartbeat though
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
        protected RespondActivityTaskCompletedResponse Complete( string result)
        {
            hasCompletionBeenSignalled = true;
            if (WorkflowExecutionContext.CurrentActivityTask == null) return null;
            return WorkflowManager.SWFClient.RespondActivityTaskCompleted(new RespondActivityTaskCompletedRequest()
                {
                    TaskToken = WorkflowExecutionContext.CurrentActivityTask.TaskToken,
                    Result = result
                });
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
            if ( details != null &&  details.Length > 32768)
                details = details.Substring(0, 32768);

            if ( reason != null && reason.Length > 256)
                reason = reason.Substring(0, 256);

            if (WorkflowExecutionContext.CurrentActivityTask == null) return null;
            return WorkflowManager.SWFClient.RespondActivityTaskFailed(new RespondActivityTaskFailedRequest() {
                TaskToken = WorkflowExecutionContext.CurrentActivityTask.TaskToken,
                Reason = reason,
                Details = (details) });
        }

       
    }
}
