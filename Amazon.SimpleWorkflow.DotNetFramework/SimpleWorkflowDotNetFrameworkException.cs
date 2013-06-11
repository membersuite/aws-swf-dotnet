using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework
{
    public class SimpleWorkflowDotNetFrameworkException : Exception 
    {
        public string ErrorCode { get; set; }

        public SimpleWorkflowDotNetFrameworkException(string errorCode, string message) : base( message )
        {
            ErrorCode = errorCode;
        }
    }
}
