using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
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
            var withScheduleActivityTaskDecision = new Decision()
                .WithDecisionType("ScheduleActivityTask")
                .WithScheduleActivityTaskDecisionAttributes(
                new ScheduleActivityTaskDecisionAttributes()
                .WithStartToCloseTimeout( DEFAULT_TASK_START_TO_CLOSE_TIMEOUT.ToString())
                .WithScheduleToStartTimeout(DEFAULT_SCHEDULE_TO_START_TIMEOUT.ToString() )
                .WithScheduleToCloseTimeout(DEFAULT_SCHEDULE_TO_CLOSE_TIMEOUT.ToString() )
                .WithInput( WorkflowExecutionContext.PrepareInputForWorkflow( input ) )
                
                .WithTaskList( WorkflowManager.GetTaskList( task, taskList ) )
                    .WithActivityType(new ActivityType().WithName(task)
                    
                .WithVersion(version)).WithActivityId(Guid.NewGuid().ToString()));
            
             
         
            return withScheduleActivityTaskDecision ;
        }

        public static Decision GenerateMarkerDecision(string markerName, string markerDetails)
        {
            return new Decision().WithDecisionType( DecisionTypes.RecordMarker )
                .WithRecordMarkerDecisionAttributes(new RecordMarkerDecisionAttributes()
                .WithMarkerName(markerName).WithDetails(markerDetails));
        }

        public static Decision GenerateWorkflowCompletedDecision( string result )
        {
            return new Decision().WithDecisionType( DecisionTypes.CompleteWorkflowExecution).WithCompleteWorkflowExecutionDecisionAttributes(new CompleteWorkflowExecutionDecisionAttributes()
            .WithResult(result ));
        }

        public static Decision GenerateWorkflowFailedDecision(string reason, string details)
        {
            return new Decision().WithDecisionType(DecisionTypes.FailWorkflowExecution).WithFailWorkflowExecutionDecisionAttributes(new FailWorkflowExecutionDecisionAttributes()
            .WithReason(reason).WithDetails(details));
           
        }

        public static Decision GenerateTimerDecision(TimeSpan timerInterval, string timerID, string control)
        {
            if (timerID == null) throw new ArgumentNullException("timerID");

            return new Decision().WithDecisionType(DecisionTypes.StartTimer)
                .WithStartTimerDecisionAttributes(new StartTimerDecisionAttributes()
                .WithTimerId(timerID)
                .WithStartToFireTimeout(timerInterval.TotalSeconds.ToString())
                .WithControl(control));
        }

        public static Decision GenerateWorkflowCancelledDecision(string details )
        {
            return new Decision().WithDecisionType(DecisionTypes.CancelWorkflowExecution).WithCancelWorkflowExecutionDecisionAttributes(new CancelWorkflowExecutionDecisionAttributes()
            .WithDetails(details));
             
        }

        public static Decision GenerateContinueWorkflowAsNewDecision(string result)
        {
            return new Decision().WithDecisionType(DecisionTypes.ContinueAsNewWorkflowExecution)
                .WithContinueAsNewWorkflowExecutionDecisionAttributes(new ContinueAsNewWorkflowExecutionDecisionAttributes());
                 
          
        }
    }
}
