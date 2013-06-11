using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.StateMachine
{
    public class IntroductionActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            Console.WriteLine("Welcome to the state machine sample. This state machine");
            Console.WriteLine(" represents an order process. It will receive the order");
            Console.WriteLine(", process payment, then ask for you want expedited shipping.");
            Console.WriteLine("If you say yes, it will ask for an expedite code; if no,");
            Console.WriteLine("it will complete the order.");
            Console.WriteLine();
            Console.WriteLine("Ready to continue?");
            Console.ReadLine();

          
        }
    }
}
