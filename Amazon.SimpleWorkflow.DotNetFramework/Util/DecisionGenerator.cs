using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Util
{
    public static class DecisionGenerator
    {
        const int DEFAULT_TASK_START_TO_CLOSE_TIMEOUT = 1000;
        const int DEFAULT_SCHEDULE_TO_START_TIMEOUT = 1000;
        const int DEFAULT_SCHEDULE_TO_CLOSE_TIMEOUT = 1000;
        const int DEFAULT_HEARTBEAT_TIMEOUT = 1000;

        public static Decision GenerateScheduleTaskActivityDecision(string taskList, string task, string version, string input)
        {
            var ac = SimpleWorkflowFoundationSettings.Settings.FindActivity(task);

           
            var withScheduleActivityTaskDecision = new Decision()
                {

                    DecisionType = "ScheduleActivityTask",
                    ScheduleActivityTaskDecisionAttributes = new ScheduleActivityTaskDecisionAttributes()
                        {
                            StartToCloseTimeout =
                                (ac.TaskStartToCloseTimeout ?? DEFAULT_TASK_START_TO_CLOSE_TIMEOUT).ToString(),
                            ScheduleToStartTimeout =
                                (ac.TaskScheduleToStartTimeout ?? DEFAULT_SCHEDULE_TO_START_TIMEOUT).ToString(),
                            ScheduleToCloseTimeout =
                                (ac.TaskScheduleToCloseTimeout ?? DEFAULT_SCHEDULE_TO_CLOSE_TIMEOUT).ToString(),
                            Input = WorkflowExecutionContext.PrepareInputForWorkflow(input),
                            TaskList = WorkflowManager.FormatTaskListAsNecessary(taskList),
                            ActivityType = new ActivityType {Name = task,Version = version},
                            ActivityId = Guid.NewGuid().ToString()
                        }
                };
            
         
            return withScheduleActivityTaskDecision ;
        }

        public static Decision GenerateMarkerDecision(string markerName, string markerDetails)
        {
            return new Decision()
            {
                DecisionType = DecisionTypes.RecordMarker,
                RecordMarkerDecisionAttributes = new RecordMarkerDecisionAttributes()
                {
                    MarkerName = markerName,
                    Details = markerDetails
                }
            };
        }

        public static Decision GenerateWorkflowCompletedDecision(string result)
        {
            return new Decision()
                {
                    DecisionType = DecisionTypes.CompleteWorkflowExecution,
                    CompleteWorkflowExecutionDecisionAttributes =
                        new CompleteWorkflowExecutionDecisionAttributes() {Result = result}
                };
        }

        public static Decision GenerateWorkflowFailedDecision(string reason, string details)
        {
            return new Decision()
                {
                    DecisionType = DecisionTypes.FailWorkflowExecution,
                    FailWorkflowExecutionDecisionAttributes = new FailWorkflowExecutionDecisionAttributes()
                        {
                            Reason = reason,
                            Details = details
                        }
                };

        }

        public static Decision GenerateTimerDecision(TimeSpan timerInterval, string timerID, string control)
        {
            if (timerID == null) throw new ArgumentNullException("timerID");

            return new Decision()
                {
                    DecisionType = DecisionTypes.StartTimer,
                    StartTimerDecisionAttributes = new StartTimerDecisionAttributes()
                        {
                            TimerId = timerID,
                            StartToFireTimeout = timerInterval.TotalSeconds.ToString(),
                            Control = control
                        }
                };
        }

        public static Decision GenerateWorkflowCancelledDecision(string details)
        {
            return new Decision()
                {
                    DecisionType = DecisionTypes.CancelWorkflowExecution,
                    CancelWorkflowExecutionDecisionAttributes = new CancelWorkflowExecutionDecisionAttributes()
                        {
                            Details = details
                        }
                };
        }

  

        public static Decision GenerateContinueWorkflowAsNewDecision(string result)
        {
            return new Decision()
                {
                    DecisionType = DecisionTypes.ContinueAsNewWorkflowExecution,
                    ContinueAsNewWorkflowExecutionDecisionAttributes =
                        new ContinueAsNewWorkflowExecutionDecisionAttributes()
                };

        }
    }
}
