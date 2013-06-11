using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Base;
using Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.LargeInput;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests
{
    [TestClass]
    public class SimpleWorkflowTests
    {
        [TestMethod]
        public void SimpleWorkflow_Test()
        {
            Assert.Inconclusive("This test is designed to be run interactively.");
            // we're running a simple, three step workflow. It should update a file in a location we specified, and that file
            // should end up with the following:

            /* <blank>
            /* Line 1
             * Line 2
             * Line 3
             * Workflow is completed */

            // we accomplish this through the ThreeStepWorkflow. In this case, we're going to pass contextual info into the
            // activity, rather than using three separate activities, to make it happen

            // let's start the workflow
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            string fileName = Directory.GetCurrentDirectory() + "/threestepworkflow.txt";
            File.Create(fileName).Close();  // create the file (and close it)
            var workflowExecutionId = Guid.NewGuid().ToString();

            var tags = new List<string> { "SimpleWorkflow_Test" };
            var resp = WorkflowManager.StartWorkflow("ThreeStepWorkflow", workflowExecutionId, "ThreeStepWorkflow", fileName, tags  );

            // it should not take more than a few seconds

            WorkflowManager.WaitUntilWorkflowCompletes( workflowExecutionId, resp.StartWorkflowExecutionResult.Run.RunId);
            
            

            string[] lines = File.ReadAllLines(fileName);

            
            Assert.IsNotNull(lines, "No lines read - file not changed!");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                Console.WriteLine("File Line #{0}: {1}", i, line); // write the lines out
            }

            Assert.AreEqual(5, lines.Length, "Wrong # of file lines");
            Assert.AreEqual("Line 1", lines[1], "First line is wrong");
            Assert.AreEqual("Line 2", lines[2], "Second line is wrong");
            Assert.AreEqual("Line 3", lines[3], "Third line is wrong");
            Assert.AreEqual("Workflow is completed", lines[4], "Fourth line is wrong");
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // used to catch unhandled exceptions caused by threads
            Console.WriteLine("An unhandled AppDomain exception has occurred.");
        }

        [TestMethod]
        public void SimpleWorkflowWithS3InputStorage_Test()
        {
            // tests to ensure that the system can handle a large message that is input to the workflow
            StringBuilder msg = new StringBuilder();

            string fileName = Directory.GetCurrentDirectory() + "/largeinput.txt";
          
            SaveLargeInputActivity.FileName = fileName;

            for (int i = 0; i < WorkflowExecutionContext.MAX_INPUT_SIZE*2; i++)
                msg.Append("s");

            var workflowExecutionId = Guid.NewGuid().ToString();
            var resp = WorkflowManager.StartWorkflow("LargeInputWorkflow", workflowExecutionId, null, msg.ToString(), new List<string> { "SimpleWorkflowWithS3InputStorage_Test" });

            WorkflowManager.WaitUntilWorkflowCompletes(workflowExecutionId, resp.StartWorkflowExecutionResult.Run.RunId);

            FileInfo fi = new FileInfo(fileName);

            Assert.IsTrue(fi.Exists, "No file created");
            Assert.AreEqual(WorkflowExecutionContext.MAX_INPUT_SIZE * 2, fi.Length, "File length is wrong");

            var contents = File.ReadAllText(fileName);
            Assert.AreEqual(msg.ToString(), contents, "File contents are off");
        }

        [TestMethod]
        public void WorkflowManager_GetLatestVersionTest()
        {
            var version = WorkflowManager.GetLatestVersionOfWorkflow("TimerWorkflow");

            Assert.AreEqual("2", version, "Current version not correct");
        }
    }
}
