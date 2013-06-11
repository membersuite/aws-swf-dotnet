using System;
using System.Collections.Generic;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base.RecurringTask
{
    /// <summary>
    /// A recurring task worfklow is a workflow that happens on a regular schedule.
    /// After the task completes, the system initiates a timer to schedule another initiation of the
    /// task
    /// </summary>
    public abstract class RecurringTaskWorkflowDecider : WorkflowDeciderBase
    {
        protected abstract Type getActivityTask();
        //const int MAX_NUMBER_OF_EVENTS = 250;
        //protected virtual int getMaximumNumberOfExecutions()
        //{
        //    return 100;
        //}

        ///// <summary>
        ///// When set to false, after max executions the workflow continues as a new workflow
        ///// </summary>
        //protected virtual bool TerminateWorkflowAfterMaxExecutions
        //{
        //    get { return false; }
        //}
        protected TimeSpan  getMaximumAmountOfTimeBeforeRespawning()
        {
            return TimeSpan.FromHours(1);
        }

        public override void respondWithDecision()
        {

            // now, what happened last - did an activity complete, or did a timer fire?
            var lastEvent = WorkflowExecutionContext.FindMostRecentEvent(
                x => x.EventType == WorkflowHistoryEventTypes.TimerFired ||
                    x.EventType == WorkflowHistoryEventTypes.WorkflowExecutionStarted ||
                x.EventType.StartsWith("ActivityTask"));

            // now, if the last event was a timer firing, then we need to schedule the activity
            RespondDecisionTaskCompletedRequest respondDecisionTaskCompletedRequest;

            // ok - so here's the deal. If the last event was that the timer fired or the workflow started, then we're going to run the activity. 
            // Otherwise, we'll fire the timer
            if (lastEvent != null && lastEvent.EventType != WorkflowHistoryEventTypes.TimerFired && lastEvent.EventType != WorkflowHistoryEventTypes.WorkflowExecutionStarted)
            {
                // let's put the timer in place

                // but, have we reached our max?
                //var respExecutionCount = WorkflowExecutionContext.GetWorkflowVariable("ExecutionCount");
                //int executionCount = 0;

                //if (respExecutionCount != null)
                //    int.TryParse(respExecutionCount.Details, out executionCount);

                //var maxExecutions = getMaximumNumberOfExecutions();
             

                //// we use MAX_NUMBER_OF_EVENTS because after the event log gets to big, AWS starts spitting out 
                //// wierd errors. So we constrain
                //    if (executionCount >= maxExecutions
                //        || WorkflowExecutionContext.CurrentDecisionTask.Events.Count > MAX_NUMBER_OF_EVENTS 
                //        ) // that's it
                //    {
                //        string result = "An execution count of " + executionCount +
                //                        " has been reached.";
                //        if (TerminateWorkflowAfterMaxExecutions)  // we're done
                //            WorkflowExecutionContext.CompleteCurrentWorkflow(result);
                //        else
                //            WorkflowExecutionContext.ContinueCurrentWorkflowAsNew(result);
                        

                   
                //        return;
                //    }
                var detail = WorkflowExecutionContext.DescribeCurrentlyExecutingWorkflow();
                TimeSpan tsRunning = DateTime.Now - detail.ExecutionInfo.StartTimestamp;

                TimeSpan maximumAmountOfTimeBeforeRespawning = getMaximumAmountOfTimeBeforeRespawning();
                if (tsRunning > maximumAmountOfTimeBeforeRespawning) // respawn
                {
                    string result =
                        string.Format("Workflow has been running for {0} minutes. Respawning set for {1} minutes.",
                                      tsRunning.TotalMinutes, maximumAmountOfTimeBeforeRespawning.TotalMinutes);
                    WorkflowExecutionContext.ContinueCurrentWorkflowAsNew(result);
                    return;
                }

                // ok, we're good, let's just schedule the timer
                respondDecisionTaskCompletedRequest = new RespondDecisionTaskCompletedRequest().WithTaskToken(
                    WorkflowExecutionContext.TaskToken).WithDecisions(
                        new List<Decision>
                            {
                                DecisionGenerator.GenerateTimerDecision(getTimerInterval(), "RecurringTaskTimer", null),

                                // and let's update the execution count
                                //DecisionGenerator.GenerateMarkerDecision("ExecutionCount", (executionCount + 1 ).ToString())
                            });
            }
            else
            {
                // let's schedule the activity
                Type activityType = getActivityTask();
                if (activityType == null) throw new ArgumentNullException("activityType");

                ActivityConfiguration wc = WorkflowManager.FindActivityByType(activityType);

                string version = wc.Version;
                string taskList = wc.DefaultTaskList;


                respondDecisionTaskCompletedRequest = new RespondDecisionTaskCompletedRequest().WithTaskToken(
                    WorkflowExecutionContext.TaskToken).WithDecisions(
                        new List<Decision>
                            {
                                DecisionGenerator.GenerateScheduleTaskActivityDecision(taskList, wc.Name, version, null)
                            });
            }

            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(respondDecisionTaskCompletedRequest);
        }

        protected abstract TimeSpan getTimerInterval();

    }
}
