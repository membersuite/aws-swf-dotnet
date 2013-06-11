using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Constants
{
    public static class WorkflowExecutionCloseStatus
    {
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELED";
        public const string Failed = "FAILED";
        public const string Terminated= "TERMINATED";
        public const string TimedOut = "TIMED_OUT";
        public const string ContinuedAsNew= "CONTINUED_AS_NEW";
    }
}
