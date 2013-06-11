using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.Timer
{
    public class TimerWorkflowActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.Write(DateTime.Now.ToString());
        }
    }
}
