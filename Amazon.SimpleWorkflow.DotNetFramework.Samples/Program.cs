using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.SimpleWorkflow.DotNetFramework.Util;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            WorkflowLogging.OnError += new WorkflowLogging.LogHandler(WorkflowLogging_Output);
            WorkflowLogging.OnDebug  += new WorkflowLogging.LogHandler(WorkflowLogging_Output);
            WorkflowManager.Initialize();

            Console.WriteLine("Welcome to the AWS SWF .NET Sample Application. Select a sample below.");
            Console.WriteLine();
            Console.WriteLine("1. Sample State Machine Workflow");
            Console.WriteLine("2. Sample Recurring Task Workflow");

            Console.WriteLine();
            var key = Console.ReadKey();

            switch (key.KeyChar)
            {
                case '1':
                    WorkflowManager.StartWorkflow("SampleStateMachineWorkflow", Guid.NewGuid().ToString(), "AllTasks", null, null);
                    break;

                case '2':
                    WorkflowManager.StartWorkflow("SampleRecurringWorkflow", Guid.NewGuid().ToString(), "AllTasks", null, null);
                    break;
            }

            do
            {
                Thread.Sleep(1000);
            } while (true);


        }

        static void WorkflowLogging_Output(string msg, params object[] args)
        {
            Console.WriteLine(msg, args);
        }
    }
}
