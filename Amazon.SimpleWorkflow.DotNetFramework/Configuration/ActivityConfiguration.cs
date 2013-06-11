using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    
    public class ActivityConfiguration : ConfigurationElement 
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

        [ConfigurationProperty("version", IsRequired = true )]
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
                var val =  this["defaultTaskList"] as string;
                if ( string.IsNullOrWhiteSpace( val ) ) return null;
                return val;
            }
            set
            {
                this["defaultTaskList"] = value;
            }
        }
        [ConfigurationProperty("taskScheduleToStartTimeout", IsRequired = false)]
        public int? TaskScheduleToStartTimeout
        {
            get
            {
                return this["taskScheduleToStartTimeout"] as int?;
            }
            set
            {
                this["taskScheduleToStartTimeout"] = value;
            }
        }

        [ConfigurationProperty("taskStartToCloseTimeout", IsRequired = false)]
        public int? TaskStartToCloseTimeout
        {
            get
            {
                return this["taskStartToCloseTimeout"] as int?;
            }
            set
            {
                this["taskStartToCloseTimeout"] = value;
            }
        }

        [ConfigurationProperty("taskScheduleToCloseTimeout", IsRequired = false)]
        public int? TaskScheduleToCloseTimeout
        {
            get
            {
                return this["taskScheduleToCloseTimeout"] as int?;
            }
            set
            {
                this["taskScheduleToCloseTimeout"] = value;
            }
        }

        [ConfigurationProperty("heartbeatTimeout", IsRequired = true)]
        public int? HeartbeatTimeout
        {
            get
            {
                return this["heartbeatTimeout"] as int?;
            }
            set
            {
                this["heartbeatTimeout"] = value;
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


        
    }
}
