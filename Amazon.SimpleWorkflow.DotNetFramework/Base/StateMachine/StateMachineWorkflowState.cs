using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;
using MemberSuite;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine
{
    /// <summary>
    /// Class StateMachineWorkflowState
    /// </summary>
    public abstract class StateMachineWorkflowState
    {

        private string _defaultInputToUse = null;

        public string DefaultInputToUse
        {
            get { return _defaultInputToUse; }
        }

        public StateMachineWorkflowState WithDefaultInput(string input)
       {
          
           _defaultInputToUse = input;
           return this;
       }

        public void Execute(string input)
        {
            
            executeCurrentState(input ?? DefaultInputToUse  );
            

        }

        protected virtual void executeCurrentState(string input)
        {
            // by default, we just run the activity with the same name as the state
            string nameOfActivityToRun = GetType().Name;
            if (nameOfActivityToRun.EndsWith("State"))
                nameOfActivityToRun = nameOfActivityToRun.Substring(0, nameOfActivityToRun.Length - "State".Length) + "Activity";

            executeActivity(nameOfActivityToRun, input);
        }

        public void ProcessStateTransition(string resultOfLastActivity)
        {
            if (WorkflowExecutionContext.CurrentDecisionTask == null) throw new ArgumentNullException("WorkflowExecutionContext.CurrentDecisionTask");

            processStateTransition(resultOfLastActivity);
        }

        protected abstract void processStateTransition(string resultOfLastActivity);

        public virtual void HandleActivityFailure(HistoryEvent lastEvent, string reason, string details)
        {
            
            
            if (lastEvent.ActivityTaskFailedEventAttributes != null)
            {
                
                WorkflowLogging.Debug("Workflow '{0}' run '{1}' experienced a failed activity.. \r\nReason: {2}\r\nDetails {3}",
                    WorkflowExecutionContext.Current.WorkflowId,
                    WorkflowExecutionContext.Current.RunId,
                    reason, details);

                WorkflowExecutionContext.FailCurrentWorkflow(lastEvent.ActivityTaskFailedEventAttributes.Reason,
                               lastEvent.ActivityTaskFailedEventAttributes.Details);

                    
                return;
            }
            
            if (lastEvent.ActivityTaskTimedOutEventAttributes != null)
            {
                var timeoutType = lastEvent.ActivityTaskTimedOutEventAttributes.TimeoutType;
                
                WorkflowLogging.Debug("Workflow '{0}' run '{1}' experienced a timeout type {4}. \r\nReason: {2}\r\nDetails {3}",
                  WorkflowExecutionContext.Current.WorkflowId,
                  WorkflowExecutionContext.Current.RunId,
                  reason, details, timeoutType);

                WorkflowExecutionContext.FailCurrentWorkflow("Timeout",
                               string.Format("Activity timed out with type '{0}' .",
                                             timeoutType));

                return;

            }


            WorkflowLogging.Debug("Workflow '{0}' run '{1}' experienced an unknown error. Ending...");

            WorkflowExecutionContext.FailCurrentWorkflow("An activity failed and the workflow has ended.", null);
        }

        /// <summary>
        /// Instructs the workflow to move to a different state by executing a specific activity
        /// </summary>
        /// <param name="input"> </param>
        /// <typeparam name="T"></typeparam>
        protected RespondDecisionTaskCompletedResponse executeActivity<T>(string input) where T:IWorkflowActivity
        {
            return executeActivity(WorkflowManager.FindActivityByType(typeof(T)).Name , input);
        }
        protected RespondDecisionTaskCompletedResponse executeActivity(string name, string input) 
        {

            ActivityConfiguration wc = SimpleWorkflowFoundationSettings.Settings.FindActivity(name);

            string version = wc.Version;
            string taskList = wc.DefaultTaskList;

            var req = new RespondDecisionTaskCompletedRequest()
            {
                TaskToken = WorkflowExecutionContext.CurrentDecisionTask.TaskToken,
                Decisions =
                    new List<Decision> { 
                    DecisionGenerator.GenerateScheduleTaskActivityDecision(taskList, name, version, input),

                    // and, let's record the current state
                    DecisionGenerator.GenerateMarkerDecision( StateMachineWorkflowDecider.STATE_MARKER_NAME, 
                    GetType().Name )
                }
            };
            
            return WorkflowManager.SWFClient.RespondDecisionTaskCompleted( req );
        }

        protected void transitionTo<T>(string input) where T:StateMachineWorkflowState
        {
            // first, let's record the state
            var state = (StateMachineWorkflowState)Container.GetOrCreateInstance(typeof(T));
            
            state.Execute(input );

        }

        protected void transitionTo<T>() where T : StateMachineWorkflowState
        {
            transitionTo<T>(DefaultInputToUse);
        }
    }
}
