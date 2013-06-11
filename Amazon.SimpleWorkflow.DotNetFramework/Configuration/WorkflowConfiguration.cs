using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    
    public class WorkflowConfiguration : ConfigurationElement 
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get
            {
                return this["type"] as string;
            }
            set
            {
                this["type"] = value;
            }
        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("version", IsRequired = true)]
        public string Version
        {
            get
            {
                return this["version"] as string;
            }
            set
            {
                this["version"] = value;
            }
        }

        [ConfigurationProperty("maxConcurrentExecutionsForThisInstance", IsRequired = false)]
        public int? MaxConcurrentExecutionsForThisInstance
        {
            get
            {
                return this["maxConcurrentExecutionsForThisInstance"] as int?;
            }
            set
            {
                this["maxConcurrentExecutionsForThisInstance"] = value;
            }
        }

        [ConfigurationProperty("maxConcurrentExecutions", IsRequired = false)]
        public int? MaxConcurrentExecutions
        {
            get
            {
                return this["maxConcurrentExecutions"] as int?;
            }
            set
            {
                this["maxConcurrentExecutions"] = value;
            }
        }

        [ConfigurationProperty("defaultTaskList", IsRequired = false)]
        public string DefaultTaskList
        {
            get
            {
                var val = this["defaultTaskList"] as string;
                if (string.IsNullOrWhiteSpace(val))
                    return null;

                return val;
            }
            set
            {
                this["defaultTaskList"] = value;
            }
        }
        [ConfigurationProperty("defaultExecutionStartToCloseTimeout", IsRequired = false)]
        public int? DefaultExecutionStartToCloseTimeout
        {
            get
            {
                return this["defaultExecutionStartToCloseTimeout"] as int?;
            }
            set
            {
                this["defaultExecutionStartToCloseTimeout"] = value;
            }
        }

        [ConfigurationProperty("defaultTaskStartToCloseTimeout", IsRequired = false)]
        public int? DefaultTaskStartToCloseTimeout
        {
            get
            {
                return this["defaultTaskStartToCloseTimeout"] as int?;
            }
            set
            {
                this["defaultTaskStartToCloseTimeout"] = value;
            }
        }


        [ConfigurationProperty("defaultChildPolicy", IsRequired = false)]
        public string DefaultChildPolicy
        {
            get
            {
                return this["defaultChildPolicy"] as string;
            }
            set
            {
                this["defaultChildPolicy"] = value;
            }
        }

        [ConfigurationProperty("description", IsRequired = false)]
        public string Description
        {
            get
            {
                return this["description"] as string;
            }
            set
            {
                this["description"] = value;
            }
        }

         [ConfigurationProperty("alwaysKeepThisWorkflowRunning", IsRequired = false)]
        public bool AlwaysKeepThisWorkflowRunning
        {
            get
            {
                return Convert.ToBoolean( this["alwaysKeepThisWorkflowRunning"] );
            }
            set
            {
                this["alwaysKeepThisWorkflowRunning"] = value;
            }
        }
        
    }
}
