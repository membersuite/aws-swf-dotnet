using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.TestStateMachineWorkflow
{
    public class FirstActivity : WorkflowActivityBase
    {

          
        protected override void processWorkflowActivity()
        {
            
            

  
            var tags = WorkflowExecutionContext.DescribeCurrentlyExecutingWorkflow().ExecutionInfo.TagList;
            if (tags != null && tags.Count > 0)
                foreach (var tag in tags)
                    Console.WriteLine("Tag: " + tag);
            else
                Console.WriteLine("No tags found.");

        
            var workflowInput = WorkflowExecutionContext.GetWorkflowInput();
            Console.WriteLine("Workflow input: " + workflowInput);

            if ( workflowInput != "exception")  // otherwise, concurrently running tests fail
                TestStateMachineWorkflow.StaticString += "1";

            complete( workflowInput);
        }
    }
}
