using System;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.TestStateMachineWorkflow
{
    public class TestStateMachineWorkflow : StateMachineWorkflowDecider
    {

        public override StateMachineWorkflowState GetInitialState()
        {
            return new FirstState();
        }

        public class FirstState : StateMachineWorkflowState
        {
            
 

            protected override void processStateTransition(string resultOfLastActivity)
            {
                Console.WriteLine("First state process transition: " + resultOfLastActivity );

                if (resultOfLastActivity == "second")
                    transitionTo<SecondState>( null );
                else if (resultOfLastActivity == "exception")
                    transitionTo<FailedState>( null );
                else 
                    transitionTo<ThirdState>( null );
            }

          
        }

        public class SecondState : StateMachineWorkflowState
        { 



            protected override void processStateTransition(string resultOfLastActivity)
            {
                WorkflowExecutionContext.CompleteCurrentWorkflow(null);

            }

            
        }

        public class ThirdState : StateMachineWorkflowState
        {
             



            protected override void processStateTransition(string resultOfLastActivity)
            {
                WorkflowExecutionContext.CompleteCurrentWorkflow(null);

            }


        }

        public class FailedState : StateMachineWorkflowState
        {
            



            protected override void processStateTransition(string resultOfLastActivity)
            {
                WorkflowExecutionContext.FailCurrentWorkflow("An error has occurred", "details");

            }


        }

        
        public static string StaticString = "";
        public static string ExceptionString = "";

       
    }
}
