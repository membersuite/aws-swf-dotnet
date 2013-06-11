using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.StateMachine
{
    class SampleStateMachineWorkflow : StateMachineWorkflowDecider
    {
        public override StateMachineWorkflowState GetInitialState()
        {
            return new IntroductionState();
        }

        public class IntroductionState : StateMachineWorkflowState
        {
            protected override void executeCurrentState(string input)
            {
                // this code is not necessary - the default implementation will automatically try to 
                // use reflection to get the appropriate activity. It figures out the activity by replacing the
                // word "State" at the end of the current class with "Activity"  - so it figures it should
                // call the IntroductionActivity for the IntroductionState. Convention over configuration.
                // But we'll put it here for doc purposes
                //base.executeCurrentState(input);


                executeActivity("IntroductionActivity", input);
            }
            protected override void processStateTransition(string resultOfLastActivity)
            {
                transitionTo<ProcessPaymentState>( null );
            }
        }

        public class ProcessPaymentState : StateMachineWorkflowState
        {
            protected override void processStateTransition(string resultOfLastActivity)
            {
                transitionTo<CheckShippingState>(null);
            }
        }

        public class CheckShippingState : StateMachineWorkflowState
        {
            protected override void processStateTransition(string resultOfLastActivity)
            {
                if ( resultOfLastActivity == "EXPEDITE")
                transitionTo<GetShippingInfoState>(null);
                else
                    transitionTo<CompleteOrderState>(null);
            }
        }

        public class GetShippingInfoState : StateMachineWorkflowState
        {
            protected override void processStateTransition(string resultOfLastActivity)
            {
                transitionTo<CompleteOrderState>(null);
            }
        }

        public class CompleteOrderState : StateMachineWorkflowState
        {
            protected override void processStateTransition(string resultOfLastActivity)
            {
                WorkflowExecutionContext.CompleteCurrentWorkflow(null);
            }
        }
    }
}
