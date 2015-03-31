using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    public class SimpleWorkflowFoundationSettings : ConfigurationSection
    {
        const int DEFAULT_MAX_CONCURRENT_ACTIVITY_EXECUTIONS_PER_INSTANCE = 30;
        const int DEFAULT_MAX_CONCURRENT_DECIDER_EXECUTIONS_PER_INSTANCE = 50;
        const int DEFAULT_METRICS_COLLECTION_PERIOD = 1000;
        private static SimpleWorkflowFoundationSettings _settings = ConfigurationManager.GetSection("SimpleWorkflowFoundationSettings") as SimpleWorkflowFoundationSettings;
        

        public static SimpleWorkflowFoundationSettings Settings
        {
            get { return _settings; }
        }

        [ConfigurationProperty("workflows")]
        public WorkflowConfigurationElementCollection Workflows
        {
            get { return ((WorkflowConfigurationElementCollection)(base["workflows"])); }
        }

        [ConfigurationProperty("clients")]
        public WorkflowClientConfigurationElementCollection Clients
        {
            get { return ((WorkflowClientConfigurationElementCollection)(base["clients"])); }
        }

        [ConfigurationProperty("activities")]
        public ActivityConfigurationElementCollection Activities
        {
            get { return ((ActivityConfigurationElementCollection)(base["activities"])); }
        }

        [ConfigurationProperty("domain", IsRequired = true)]
        public string Domain
        {
            get
            {
                return this["domain"] as string;
            }
            set
            {
                this["domain"] = value;
            }
        }

        [ConfigurationProperty("s3BucketName", IsRequired = true)]
        public string S3BucketName
        {
            get
            {
                return this["s3BucketName"] as string;
            }
            set
            {
                this["s3BucketName"] = value;
            }
        }

        [ConfigurationProperty("prefixTaskListWithComputerName", IsRequired = false)]
        public bool PrefixTaskListWithComputerName
        {
            get
            {
                return Convert.ToBoolean(this["prefixTaskListWithComputerName"]);
            }
            set
            {
                this["prefixTaskListWithComputerName"] = value;
            }
        }

        [ConfigurationProperty("prefixTaskListWithProcessName", IsRequired = false)]
        public bool PrefixTaskListWithProcessName
        {
            get
            {
                return Convert.ToBoolean(this["prefixTaskListWithProcessName"]);
            }
            set
            {
                this["prefixTaskListWithProcessName"] = value;
            }
        }

        [ConfigurationProperty("enableWorkflowMetrics", IsRequired = false)]
        public bool EnableWorkflowMetrics
        {
            get
            {
                return Convert.ToBoolean(this["enableWorkflowMetrics"]);
            }
            set
            {
                this["enableWorkflowMetrics"] = value;
            }
        }

        [ConfigurationProperty("maxActivityThreads", IsRequired = false)]
        public int MaxActivityThreads
        {
            get
            {
                object value = this["maxActivityThreads"];

                if (value == null)
                    return DEFAULT_MAX_CONCURRENT_ACTIVITY_EXECUTIONS_PER_INSTANCE;

                return Convert.ToInt32(value);
            }
            set
            {
                this["maxActivityThreads"] = value;
            }
        }

        
        [ConfigurationProperty("maxDeciderThreads", IsRequired = false)]
        public int MaxDeciderThreads
        {
            get
            {
                object value = this["maxDeciderThreads"];

                if (value == null)
                    return DEFAULT_MAX_CONCURRENT_DECIDER_EXECUTIONS_PER_INSTANCE;
                return Convert.ToInt32(value);
            }
            set
            {
                this["maxDeciderThreads"] = value;
            }
        }

        [ConfigurationProperty("metricsCollectionPeriod", IsRequired = false)]
        public int MetricsCollectionPeriod  
        {
            get
            {
                object objPeriod = this["metricsCollectionPeriod"];

                if (objPeriod == null)
                    return DEFAULT_METRICS_COLLECTION_PERIOD;
                return Convert.ToInt32(objPeriod);
            }
            set
            {
                this["metricsCollectionPeriod"] = value;
            }
        }


        public WorkflowConfiguration FindWorkflow(string name)
        {
            if (Workflows != null)
                foreach (WorkflowConfiguration wf in Workflows)
                    if (wf.Name == name)
                        return wf;

            throw new ApplicationException("Unable to find workflow: " + name);
        }

        public ActivityConfiguration FindActivity(string name)
        {
            if (Activities != null)
                foreach (ActivityConfiguration wf in Activities)
                    if (wf.Name == name)
                        return wf;

            throw new ApplicationException("Unable to find Activity: " + name);
        }
    }
}
