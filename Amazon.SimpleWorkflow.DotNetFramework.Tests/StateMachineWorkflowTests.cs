using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.TestStateMachineWorkflow;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests
{
    [TestClass]
    public class StateMachineWorkflowTests
    {
        [TestMethod]
        public void SimpleStateMachineWorkflow_Test()
        {
            Assert.Inconclusive("Workflow tests are meant to be run manually.");
            var workflowExecutionId = Guid.NewGuid().ToString();

            // let's do one run where the system should navigate to first, then second

            var tags = new List<string> { "SimpleStateMachineWorkflow_Test" };
            var resp = WorkflowManager.StartWorkflow("TestStateMachineWorkflow", workflowExecutionId, null, "second",
                                                     tags);
            WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, resp.Run.RunId);

            Assert.AreEqual("12", TestStateMachineWorkflow.StaticString,
                            "State machine didn't function correctly the first time");

            // now, run it again, with a different result

            TestStateMachineWorkflow.StaticString = "";

            resp = WorkflowManager.StartWorkflow("TestStateMachineWorkflow", workflowExecutionId, null, "3", tags);
            WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, resp.Run.RunId);

            Assert.AreEqual("13", TestStateMachineWorkflow.StaticString,
                            "State machine didn't function correctly the second time");


        }

        [TestMethod]
        public void SimpleStateMachineWorkflow_HandleFailure_Test()
        {

            var workflowExecutionId = Guid.NewGuid().ToString();

            // let's do one run where the system should navigate to first, then second
            var tags = new List<string> { "SimpleStateMachineWorkflow_HandleFailure_Test" };
            var resp = WorkflowManager.StartWorkflow("TestStateMachineWorkflow", workflowExecutionId, null, "exception",
                                                     tags);
            var runId = resp.Run.RunId;
            try
            {
                WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, runId);
                Assert.Fail("Exception not thrown");
            }
            catch (ApplicationException)
            {
            }

            Assert.AreEqual("exception", TestStateMachineWorkflow.ExceptionString,
                            "State machine didn't function correctly the first time");

            try
            {
                WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, runId);
                Assert.Fail("Exception not thrown");
            }
            catch (ApplicationException)
            {
            }

            var lastTask =
                WorkflowExecutionContext.FindMostRecentEvent(
                    new WorkflowExecution() { RunId = (runId), WorkflowId = workflowExecutionId },
                    null);

            Assert.AreEqual(WorkflowHistoryEventTypes.WorkflowExecutionFailed, lastTask.EventType);
            Assert.AreEqual("An error has occurred", lastTask.WorkflowExecutionFailedEventAttributes.Reason);
            Assert.AreEqual("details", lastTask.WorkflowExecutionFailedEventAttributes.Details);



        }
    }
}
