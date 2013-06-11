using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.Recurring
{
    public class SampleRecurringWorkflowTask : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.WriteLine("Running...");
        }
    }
}
