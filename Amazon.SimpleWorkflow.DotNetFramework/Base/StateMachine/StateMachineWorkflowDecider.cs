using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;
using MemberSuite;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base.StateMachine
{
    /// <summary>
    /// This class encapsulates the flow of a state machine-like workflow,
    /// where execution rests in a single state and transitions among states in an orderly
    /// fashion. They key concept here is that a State has an Execute method that typically schedules
    /// an activity, and a ProcessStateTransition that harvests the results of the activity and moves to another state
    /// </summary>
    /// <remarks>Important thing to understand; in this type of workflow, the "state" is synonmous with the current
    /// activity that is to be run. Thus, to "transition to" a state is equivalent to setting a marker for that state
    /// and executing the associated activity.</remarks>
    public abstract class StateMachineWorkflowDecider : WorkflowDeciderBase
    {
        public const string STATE_MARKER_NAME = "WorkflowState";
        public const string DEFAULT_STATE = "Start";


        /// <summary>
        /// Gets the initial state of this workflow
        /// </summary>
        /// <returns>StateMachineWorkflowState.</returns>
        public abstract StateMachineWorkflowState GetInitialState();

        public override void respondWithDecision()
        {
            var taskToDecide = WorkflowExecutionContext.CurrentDecisionTask;
            if (taskToDecide == null) throw new ArgumentNullException("taskToDecide");

            // ok, the first thing we need to do is figure out what state we are in
            var currentStateMarker = WorkflowExecutionContext.GetWorkflowVariable(STATE_MARKER_NAME);

            // now, to figure out what state we are in
            StateMachineWorkflowState state = null;

            if (currentStateMarker == null)   // we're at the initial state
            {
                state = GetInitialState();

                if (state == null)
                    throw new ApplicationException("No initial state given.");

                //WorkflowLogging.Debug("WFM: Initiating workflow ID {1} with state '{2}'",
                //    GetType().Name, WorkflowExecutionContext.Current.WorkflowId, state.GetType().Name);
            }
            else
            {

                // let's figure out the state
                string currentState =   currentStateMarker.Details;

                // now, let's go ahead and get the state
                state = getStateFromName(currentState);

                if (state == null)
                    throw new ApplicationException(string.Format("Unable to locate a class named '{0}' that derives from StateMachineWorkflowState", currentState));

               // WorkflowLogging.Debug("WFM: Continuing workflow ID {1} with state '{2}'",
               // GetType().Name, WorkflowExecutionContext.Current.WorkflowId, state.GetType().Name);
                
            }

            if (state.DefaultInputToUse == null)
                state.WithDefaultInput(WorkflowExecutionContext.GetWorkflowInput());    // always use the default input

             
            // ok, now the question is; is this decision task running because an activity has finished? In other words,
            // has an activity completed since we set the state marker?
            var lastEvent = WorkflowExecutionContext.FindMostRecentEvent( x =>
                                                                          (x.EventType ==
                                                                         WorkflowHistoryEventTypes.MarkerRecorded &&
                                                                         x.MarkerRecordedEventAttributes != null &&
                                                                         x.MarkerRecordedEventAttributes.MarkerName ==
                                                                         STATE_MARKER_NAME)
                                                                         || x.EventType == WorkflowHistoryEventTypes.ActivityTaskCanceled
                                                                         || x.EventType == WorkflowHistoryEventTypes.ActivityTaskCompleted
                                                                         || x.EventType == WorkflowHistoryEventTypes.ActivityTaskTimedOut
                                                                         || x.EventType == WorkflowHistoryEventTypes.ActivityTaskFailed);



            if (lastEvent == null ||       // we've got no event, so this is the beginning of the workflow
                lastEvent.EventType == WorkflowHistoryEventTypes.MarkerRecorded)  // the last event was the state, so execute the state
            {
                //WorkflowLogging.Debug("WFM: Workflow '{0}' - executing state '{1}'",
                //    GetType().Name, state.GetType().Name);

                state.Execute( GetInitialStateInput() );
                return;
            }

            // ok, so the last event was an activity
            switch (lastEvent.EventType)
            {
                case WorkflowHistoryEventTypes.ActivityTaskCompleted: // an activity completed
                    state.ProcessStateTransition(lastEvent.ActivityTaskCompletedEventAttributes.Result);
                    break;

                case WorkflowHistoryEventTypes.ActivityTaskFailed:
                    state.HandleActivityFailure(lastEvent, lastEvent.ActivityTaskFailedEventAttributes.Reason, lastEvent.ActivityTaskFailedEventAttributes.Details);
                    break;

                case WorkflowHistoryEventTypes.ActivityTaskTimedOut:
                    state.HandleActivityFailure(lastEvent, "Activity Timed Out", lastEvent.ActivityTaskTimedOutEventAttributes.Details);
                    break;

                case WorkflowHistoryEventTypes.ActivityTaskCanceled:
                    state.HandleActivityFailure(lastEvent, "Activity Canceled", lastEvent.ActivityTaskCanceledEventAttributes.Details);
                    break;

                default:
                    state.HandleActivityFailure(lastEvent, "Unknown Failure", null );
                    break;
            }



        }

        protected virtual StateMachineWorkflowState getStateFromName(string currentState)
        {
            StateMachineWorkflowState state = null;

            // ok, now we have the current state. We need to find the class in this nested class that encapsulates that state
            Type nestedControllerType = _getNestedTypeForTypeAndItsBase(GetType(), currentState);


            if (nestedControllerType != null) // then that's it
                state = Container.GetOrCreateInstance(nestedControllerType) as StateMachineWorkflowState;
            return state;
        }

        /// <summary>
        /// Recursively searches a type, and all parent types, for a class
        /// </summary>
        /// <param name="typeToCheck"></param>
        /// <param name="nameOfNestedType"></param>
        /// <returns></returns>
        private Type _getNestedTypeForTypeAndItsBase(Type typeToCheck, string nameOfNestedType)
        {
            if (typeToCheck == null) throw new ArgumentNullException("typeToCheck");
            if (nameOfNestedType == null) throw new ArgumentNullException("nameOfNestedType");

            Type nestedType = typeToCheck.GetNestedType(nameOfNestedType );
            if (nestedType != null)
                return nestedType;

            if (typeToCheck == typeof(object) || typeToCheck.BaseType == null )    // forget it, we're at the top of the hiearchy
                return null;

            return _getNestedTypeForTypeAndItsBase(typeToCheck.BaseType, nameOfNestedType);

        }

        protected virtual string GetInitialStateInput()
        {
            return null;
        }
    }
}
