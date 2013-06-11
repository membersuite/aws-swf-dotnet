namespace Amazon.SimpleWorkflow.DotNetFramework.Constants
{
    public static class WorkflowHistoryEventTypes
    {
        public const string WorkflowExecutionStarted = "WorkflowExecutionStarted";
        public const string WorkflowExecutionCompleted = "WorkflowExecutionCompleted";
        public const string WorkflowExecutionFailed = "WorkflowExecutionFailed";
        public const string WorkflowExecutionTimedOut = "WorkflowExecutionTimedOut";
        public const string WorkflowExecutionCanceled = "WorkflowExecutionCanceled";
        public const string WorkflowExecutionTerminated = "WorkflowExecutionTerminated";
        public const string WorkflowExecutionContinuedAsNew = "WorkflowExecutionContinuedAsNew";
        public const string WorkflowExecutionCancelRequested = "WorkflowExecutionCancelRequested";
        public const string DecisionTaskScheduled = "DecisionTaskScheduled";
        public const string DecisionTaskStarted = "DecisionTaskStarted";
        public const string DecisionTaskCompleted = "DecisionTaskCompleted";
        public const string DecisionTaskTimedOut = "DecisionTaskTimedOut";
        public const string ActivityTaskScheduled = "ActivityTaskScheduled";
        public const string ScheduleActivityTaskFailed = "ScheduleActivityTaskFailed";
        public const string ActivityTaskStarted = "ActivityTaskStarted";
        public const string ActivityTaskCompleted = "ActivityTaskCompleted";
        public const string ActivityTaskFailed = "ActivityTaskFailed";
        public const string ActivityTaskTimedOut = "ActivityTaskTimedOut";
        public const string ActivityTaskCanceled = "ActivityTaskCanceled";
        public const string ActivityTaskCancelRequested = "ActivityTaskCancelRequested";
        public const string RequestCancelActivityTaskFailed = "RequestCancelActivityTaskFailed";
        public const string WorkflowExecutionSignaled = "WorkflowExecutionSignaled";
        public const string MarkerRecorded = "MarkerRecorded";
        public const string TimerStarted = "TimerStarted";
        public const string StartTimerFailed = "StartTimerFailed";
        public const string TimerFired = "TimerFired";
        public const string TimerCanceled = "TimerCanceled";
        public const string CancelTimerFailed = "CancelTimerFailed";
        public const string StartChildWorkflowExecutionInitiated = "StartChildWorkflowExecutionInitiated";
        public const string StartChildWorkflowExecutionFailed = "StartChildWorkflowExecutionFailed";
        public const string ChildWorkflowExecutionStarted = "ChildWorkflowExecutionStarted";
        public const string ChildWorkflowExecutionCompleted = "ChildWorkflowExecutionCompleted";
        public const string ChildWorkflowExecutionFailed = "ChildWorkflowExecutionFailed";
        public const string ChildWorkflowExecutionTimedOut = "ChildWorkflowExecutionTimedOut";
        public const string ChildWorkflowExecutionCanceled = "ChildWorkflowExecutionCanceled";
        public const string ChildWorkflowExecutionTerminated = "ChildWorkflowExecutionTerminated";
        public const string SignalExternalWorkflowExecutionInitiated = "SignalExternalWorkflowExecutionInitiated";
        public const string ExternalWorkflowExecutionSignaled = "ExternalWorkflowExecutionSignaled";
        public const string SignalExternalWorkflowExecutionFailed = "SignalExternalWorkflowExecutionFailed";
        public const string RequestCancelExternalWorkflowExecutionInitiated = "RequestCancelExternalWorkflowExecutionInitiated";
        public const string ExternalWorkflowExecutionCancelRequested = "ExternalWorkflowExecutionCancelRequested";
        public const string RequestCancelExternalWorkflowExecutionFailed = "RequestCancelExternalWorkflowExecutionFailed";
    }
}
