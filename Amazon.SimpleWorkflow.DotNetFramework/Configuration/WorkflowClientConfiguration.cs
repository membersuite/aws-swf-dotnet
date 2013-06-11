using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    
    public class WorkflowClientConfiguration : ConfigurationElement 
    {
         

        [ConfigurationProperty("workflowName", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return this["workflowName"] as string;
            }
            set
            {
                this["workflowName"] = value;
            }
        }

        [ConfigurationProperty("workflowVersion", IsRequired = false)]
        public string Version
        {
            get
            {
                return this["workflowVersion"] as string;
            }
            set
            {
                this["workflowVersion"] = value;
            }
        }

        
    }
}
