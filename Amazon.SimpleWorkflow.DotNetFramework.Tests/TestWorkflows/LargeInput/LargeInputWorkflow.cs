using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine;
using Amazon.SimpleWorkflow.DotNetFramework.Util;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.LargeInput
{
    public class LargeInputWorkflow : StateMachineWorkflowDecider
    {
        public class Start : StateMachineWorkflowState
        {
            protected override void executeCurrentState(string input)
            {
                executeActivity<SaveLargeInputActivity >( WorkflowExecutionContext.GetWorkflowInput(  ) );
            }

            

            protected override void processStateTransition(string resultOfLastActivity)
            {
                WorkflowExecutionContext.CompleteCurrentWorkflow(null);
            }
        }


        public override StateMachineWorkflowState GetInitialState()
        {
            return new Start();
        }
    }
}
