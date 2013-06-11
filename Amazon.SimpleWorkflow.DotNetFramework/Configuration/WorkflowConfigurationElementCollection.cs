using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    [ConfigurationCollection(typeof(WorkflowConfiguration), AddItemName = "workflow")] 
    public class WorkflowConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new WorkflowConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WorkflowConfiguration)(element)).Name;
        }

        public WorkflowConfiguration this[int idx]
        {
            get
            {
                return base.BaseGet(idx) as WorkflowConfiguration;
            }
            set
            {
                if (base.BaseGet(idx) != null)
                {
                    base.BaseRemoveAt(idx);
                }
                this.BaseAdd(idx, value);
            }
        }

        public new WorkflowConfiguration this[string key]
        {
            get
            {
                return base.BaseGet(key) as WorkflowConfiguration;
            }
            set
            {
                if (base.BaseGet(key) != null)
                {
                    base.BaseRemove(key);
                }
                this.BaseAdd(this.Count, value);
            }
        }
    }
}
