using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    [ConfigurationCollection(typeof(WorkflowClientConfiguration), AddItemName = "client")] 
    public class WorkflowClientConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new WorkflowClientConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WorkflowClientConfiguration)(element)).Name;
        }

        public WorkflowClientConfiguration this[int idx]
        {
            get
            {
                return base.BaseGet(idx) as WorkflowClientConfiguration;
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

        public new WorkflowClientConfiguration this[string key]
        {
            get
            {
                return base.BaseGet(key) as WorkflowClientConfiguration;
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
