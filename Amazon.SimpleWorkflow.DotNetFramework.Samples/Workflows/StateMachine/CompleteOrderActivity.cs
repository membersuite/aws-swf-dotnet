using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.StateMachine
{
    public class CompleteOrderActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.WriteLine("Order is complete.");
             
        }
    }
}
