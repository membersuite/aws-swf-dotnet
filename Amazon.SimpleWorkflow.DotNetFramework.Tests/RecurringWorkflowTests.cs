﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.LargeInput;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests
{
    [TestClass]
    public class RecurringWorkflowTests
    {
        [TestMethod]
        public void RecurringWorkflow_Test()
        {
            Assert.Inconclusive("Test requires threads.");

            string workflowExecutionId = Guid.NewGuid().ToString();
            var resp  = WorkflowManager.StartWorkflow("TimerWorkflow", workflowExecutionId, null, null, null);
            WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, resp.StartWorkflowExecutionResult.Run.RunId);

            var history = WorkflowManager.SWFClient.GetWorkflowExecutionHistory(new Model.GetWorkflowExecutionHistoryRequest()
            .WithDomain(WorkflowManager.Domain)
            .WithExecution(new Model.WorkflowExecution().WithWorkflowId(workflowExecutionId).WithRunId(resp.StartWorkflowExecutionResult.Run.RunId))).GetWorkflowExecutionHistoryResult.History;

            // should be three runs
            Assert.AreEqual(3, history.Events.Count(x => x.EventType == WorkflowHistoryEventTypes.TimerFired), "Wrong number of timer fired");
        }
    }
}