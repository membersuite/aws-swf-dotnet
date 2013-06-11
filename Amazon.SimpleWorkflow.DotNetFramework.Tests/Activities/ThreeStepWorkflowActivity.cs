using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.Activities
{
    public class ThreeStepWorkflowActivity : WorkflowActivityBase
    {
        protected override void processWorkflowActivity()
        {
            string[] parts = WorkflowExecutionContext.CurrentActivityTask.Input.Split('|');
            string fileName = parts[0];
            int number = int.Parse(parts[1]);

            
           // var getExecutionDetails = swfClient.DescribeWorkflowExecution( new DescribeWorkflowExecutionRequest().WithDomain( 
           //     SimpleWorkflowFoundationSettings.Settings.Domain ).WithExecution(taskToProcess.WorkflowExecution )).DescribeWorkflowExecutionResult.WorkflowExecutionDetail.;


            if (number > 3)
            {
                File.AppendAllText(fileName, Environment.NewLine + "Workflow is completed");
            }
            else
                File.AppendAllText(fileName,Environment.NewLine + string.Format("Line {0}", number));


            complete(null);
                

        }
    }
}
