using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.TestStateMachineWorkflow
{
    public class FailedActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            TestStateMachineWorkflow.ExceptionString = "exception";
            complete(null);
        }
    }
}
