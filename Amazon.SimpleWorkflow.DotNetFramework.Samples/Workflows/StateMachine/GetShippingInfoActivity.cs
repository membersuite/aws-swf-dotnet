using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.StateMachine
{
    public class GetShippingInfoActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.WriteLine("Retrieving shipping information from shopper...");
            
 
        }
    }
}
