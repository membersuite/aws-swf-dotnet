using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.StateMachine
{
    public class CheckShippingActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.WriteLine("Would you like expedited shipping? Hit Y if so...");
            var key = Console.ReadKey();

            if (key.KeyChar.ToString().ToUpper() == "Y")
                complete("EXPEDITE");

            
        }
    }
}
