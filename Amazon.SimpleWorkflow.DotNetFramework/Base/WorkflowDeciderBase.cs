using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;


namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    public abstract class WorkflowDeciderBase : IWorkflowDecider
    {
        public void Decide()
        {
            var taskToDecide = WorkflowExecutionContext.CurrentDecisionTask;
            if (taskToDecide == null) throw new ArgumentNullException("taskToDecide");

            try
            {
                if ( ! determineIfAnActivityShouldBeRescheduled())
                    respondWithDecision();
            }
            catch( Exception ex )
            {
                 // this CANNOT cause an exception or it will fail threads and unit tests
                AmazonSimpleWorkflowException sex = ex as AmazonSimpleWorkflowException;

                if (sex != null)
                {
                    WorkflowLogging.Debug(
                        "Decider '{0}' for workflow ID '{4}' Run '{5}' Started Event ID {6} experienced an Amazon exception. Error Code: {1}\r\nMessage {2}\r\nStack Trace:\r\n-----------------------\r\n{3}",
                        GetType().Name, sex.ErrorCode, sex.Message,sex.StackTrace, WorkflowExecutionContext.Current.WorkflowId,
                        WorkflowExecutionContext.Current.RunId, taskToDecide.StartedEventId  );
                }
                try
                {
                WorkflowManager.SWFClient.RespondDecisionTaskCompleted( new RespondDecisionTaskCompletedRequest()
                .WithTaskToken( taskToDecide.TaskToken )
                .WithDecisions(
                new Decision().WithDecisionType(DecisionTypes.FailWorkflowExecution ).WithFailWorkflowExecutionDecisionAttributes( new FailWorkflowExecutionDecisionAttributes()
                .WithReason( "Exception" ).WithDetails( ex.ToString()))));
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Unable to report failed decider: " + ex2 + "\n\nThe underlying exception\n\n" + ex);
                }
            }





        }

        /// <summary>
        /// Checks to see if the last activity timed out, and if so, whether it should be rescheduled
        /// </summary>
        /// <returns></returns>
        private bool determineIfAnActivityShouldBeRescheduled()
        {

            var lastEvent = WorkflowExecutionContext.FindMostRecentActivityRelatedEvent();
            if (lastEvent == null || lastEvent.EventType != WorkflowHistoryEventTypes.ActivityTaskTimedOut)
                return false;    // no joy

            // now, did the activity have a heart beat timeout?
            if (lastEvent.ActivityTaskTimedOutEventAttributes == null ||
                lastEvent.ActivityTaskTimedOutEventAttributes.TimeoutType != "HEARTBEAT")
                return false;   // not our timeout

            // now, how many times has this timed out?
            var scheduledEvent = WorkflowExecutionContext.FindMostRecentEvent( x=>x.EventId == lastEvent.ActivityTaskTimedOutEventAttributes.ScheduledEventId );

            if ( scheduledEvent == null || scheduledEvent.ActivityTaskScheduledEventAttributes == null ) return false;

            var scheduledActivityAttr = scheduledEvent.ActivityTaskScheduledEventAttributes;
            

            string variable = string.Format("{0}({1})_{2}_RetryCount", scheduledActivityAttr.ActivityType.Name ,scheduledActivityAttr.ActivityType.Version ,
                scheduledActivityAttr.ActivityId);
            var marker = WorkflowExecutionContext.GetWorkflowVariable(variable );

            int retryCount = 0;
            if (marker != null)
                retryCount = int.Parse(marker.Details);

            if (retryCount >= GetMaximumRetryCount(scheduledEvent))
            {
                // the workflow has failed
                WorkflowExecutionContext.FailCurrentWorkflow("An activity repeatedly timed out.",
                                                             string.Format(
                                                                 "Activity ID '{0}' of type '{1}{2}' experienced {3} heartbeat timeouts, which is above the set threshold. This workflow has been terminated.",
                                                                 scheduledActivityAttr.ActivityId,
                                                                 scheduledActivityAttr.ActivityType.Name, scheduledActivityAttr.ActivityType.Version  , retryCount));
                return true;    // the calling method should do nothing
            }

            // ok, retry
            retryCount++;

            // rescedhule
            var decision = DecisionGenerator.GenerateScheduleTaskActivityDecision(scheduledActivityAttr.TaskList.Name, scheduledActivityAttr.ActivityType.Name, scheduledActivityAttr.ActivityType.Version, scheduledActivityAttr.Input);
            // we need to reset this, so that it doesn't double-add the computer name (the logic in the decision method will do this)
            decision.ScheduleActivityTaskDecisionAttributes.TaskList.Name  = scheduledActivityAttr.TaskList.Name;
            decision.ScheduleActivityTaskDecisionAttributes.ActivityId = scheduledActivityAttr.ActivityId;  // IMPORTANT! We must keep the ID the same

            var req = new RespondDecisionTaskCompletedRequest().WithTaskToken(WorkflowExecutionContext.CurrentDecisionTask.TaskToken).WithDecisions(
               new List<Decision> { 
                    decision,

                    // and, let's record the current state
                    DecisionGenerator.GenerateMarkerDecision( variable, retryCount.ToString() )
                });

            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(req);

            return true;
        }

        private int GetMaximumRetryCount(HistoryEvent scheduledEvent)
        {
            return 3;   // for now
        }


        public abstract void respondWithDecision();
        
    }
}