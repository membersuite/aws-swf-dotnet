using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.Model;

namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    public interface IWorkflowActivity
    {
         
        void Process();
    }
}
