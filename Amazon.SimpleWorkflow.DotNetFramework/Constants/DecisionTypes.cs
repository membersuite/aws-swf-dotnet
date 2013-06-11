using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Constants
{
    public static class DecisionTypes
    {
        public const string CancelTimer = "CancelTimer";
        public const string CancelWorkflowExecution = "CancelWorkflowExecution";
        public const string CompleteWorkflowExecution = "CompleteWorkflowExecution";
        public const string ContinueAsNewWorkflowExecution = "ContinueAsNewWorkflowExecution";
        public const string FailWorkflowExecution = "FailWorkflowExecution";
        public const string RecordMarker = "RecordMarker";
        public const string RequestCancelActivityTask = "RequestCancelActivityTask";
        public const string RequestCancelExternalWorkflowExecution = "RequestCancelExternalWorkflowExecution";
        public const string ScheduleActivityTask = "ScheduleActivityTask";
        public const string SignalExternalWorkflowExecution = "SignalExternalWorkflowExecution";
        public const string StartChildWorkflowExecution = "StartChildWorkflowExecution";
        public const string StartTimer = "StartTimer";
    }
}
