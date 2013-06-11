using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    /// <summary>
    /// Defines a class capable of orchestrating a workflow. A class implementing this interface
    /// will function as a "decider" in the AWS Simple Workflow Foundation architecture.
    /// </summary>
    public interface IWorkflowDecider 
    {

        void Decide();
    }
}
