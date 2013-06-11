using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Tests.Activities;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.Deciders
{
    public class ThreeStepWorkflowDecider : WorkflowDeciderBase
    {
        public override void respondWithDecision()
        {
            var taskToDecide = WorkflowExecutionContext.CurrentDecisionTask;
            if (taskToDecide == null) throw new ArgumentNullException("taskToDecide");

            var ac = WorkflowManager.FindActivityByType(typeof(ThreeStepWorkflowActivity));

            var decision = DecisionGenerator.GenerateScheduleTaskActivityDecision(ac.DefaultTaskList ,
                                                                                ac.Name , ac.Version , null);

                   
            var resp = new RespondDecisionTaskCompletedRequest()
                        .WithTaskToken(taskToDecide.TaskToken)
                        .WithDecisions(decision);

            string fileName = WorkflowExecutionContext.GetWorkflowInput();

            var marker = WorkflowExecutionContext.GetWorkflowVariable("LineCount");

            int counter = 1;
            if ( marker != null )       // this is NOT the first event
                counter = int.Parse( marker.Details );

            if (counter <= 4)
            {
                Decision markerDecision = DecisionGenerator.GenerateMarkerDecision("LineCount", (counter + 1).ToString());
                resp.Decisions.Add(markerDecision); // record a marker

                decision.ScheduleActivityTaskDecisionAttributes.Input = fileName + "|" + counter;
                WorkflowManager.SWFClient.RespondDecisionTaskCompleted(resp);
                return;
            }
            
            // only four lines

            Decision completedDecision = DecisionGenerator.GenerateWorkflowCompletedDecision( null );

            resp.Decisions.Clear();
            resp.Decisions.Add(completedDecision);

            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(resp);
        }
    }
}
