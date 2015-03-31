using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    /// <summary>
    /// Represents a workflow that is time-delayed - i.e., scheduled to happen in the future
    /// </summary>
    public abstract class DelayedTaskWorkflowDecider : WorkflowDeciderBase
    {
        protected abstract WorkflowActivityBase getActivityTask();
        protected abstract TimeSpan determineExecutionDelay();

        public override void respondWithDecision()
        {

            // now, what happened last - did an activity complete, or did a timer fire?
            var lastEvent = WorkflowExecutionContext.FindMostRecentEvent(
                x => x.EventType == WorkflowHistoryEventTypes.TimerFired ||
                    x.EventType == WorkflowHistoryEventTypes.WorkflowExecutionStarted ||
                x.EventType.Value.StartsWith("ActivityTask"));

            // now, if the last event was a timer firing, then we need to schedule the activity
            RespondDecisionTaskCompletedRequest respondDecisionTaskCompletedRequest = null; ;

            // alright... if the last event was the execution starting, we're firing the timer
            if (lastEvent == null)
                throw new ApplicationException("Unable to process state machine");

            switch (lastEvent.EventType)
            {
                case WorkflowHistoryEventTypes.WorkflowExecutionStarted:

                    // ok, we're good, let's just schedule the timer
                    var executionDelay = determineExecutionDelay();
                    if (executionDelay.TotalSeconds < 0)
                        executionDelay = TimeSpan.FromSeconds(0);   // don't allow negative numbers

                    // we need to round into minutes
                    executionDelay = TimeSpan.FromMinutes(Math.Round(executionDelay.TotalMinutes));

                    respondDecisionTaskCompletedRequest = new RespondDecisionTaskCompletedRequest()
                        {
                            TaskToken = WorkflowExecutionContext.TaskToken,
                            Decisions = new List<Decision>
                                {
                                    DecisionGenerator.GenerateTimerDecision(executionDelay,
                                                                            "DelayedTaskTimer", null)
                                }
                        };
                    break;

                case WorkflowHistoryEventTypes.TimerFired:

                    // let's schedule the activity
                    var activityType = getActivityTask();
                    if (activityType == null) throw new ArgumentNullException("activityType");

                    ActivityConfiguration wc = WorkflowManager.FindActivityByType(activityType.GetType());

                    string version = wc.Version;
                    string taskList = wc.DefaultTaskList;


                    respondDecisionTaskCompletedRequest = new RespondDecisionTaskCompletedRequest()
                    {
                        TaskToken = WorkflowExecutionContext.TaskToken,
                        Decisions =
                            new List<Decision>
                                {
                                    DecisionGenerator.GenerateScheduleTaskActivityDecision(taskList, wc.Name, version,
                                                                                           null)
                                }
                    };
                    break;

                case WorkflowHistoryEventTypes.ActivityTaskCompleted:


                    WorkflowExecutionContext.CompleteCurrentWorkflow(null);
                    return;

                case WorkflowHistoryEventTypes.ActivityTaskTimedOut:
                    // fail the workflow
                    WorkflowExecutionContext.FailCurrentWorkflow(
                        "Activity timed out",
                        lastEvent.ActivityTaskTimedOutEventAttributes.Details);
                    return;
                case WorkflowHistoryEventTypes.ActivityTaskCanceled:
                    WorkflowExecutionContext.FailCurrentWorkflow(
                        "Cancelled Task",
                        lastEvent.ActivityTaskCanceledEventAttributes.Details);
                    return;

                case WorkflowHistoryEventTypes.ActivityTaskFailed:
                    WorkflowExecutionContext.FailCurrentWorkflow(
                        lastEvent.ActivityTaskFailedEventAttributes.Reason,
                        lastEvent.ActivityTaskFailedEventAttributes.Details);
                    return;
            }

            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(respondDecisionTaskCompletedRequest);
        }
    }
}
