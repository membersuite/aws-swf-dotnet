using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Util;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.LargeInput
{
    public class SaveLargeInputActivity : WorkflowActivityBase
    {
        public static string FileName;
        protected override void processWorkflowActivity()
        {
            string input = WorkflowExecutionContext.GetActivityInput();

            File.WriteAllText(FileName, input);

            
        }
    }
}
