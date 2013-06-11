using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.TestStateMachineWorkflow
{
    public class SecondActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            TestStateMachineWorkflow.StaticString += "2";

            complete(null);
        }
    }
}
