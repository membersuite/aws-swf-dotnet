using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Configuration
{
    [ConfigurationCollection(typeof(ActivityConfiguration), AddItemName = "activity")] 
    public class ActivityConfigurationElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ActivityConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ActivityConfiguration)(element)).Name;
        }

        public ActivityConfiguration this[int idx]
        {
            get
            {
                return base.BaseGet(idx) as ActivityConfiguration;
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

        public new ActivityConfiguration this[string key]
        {
            get
            {
                return base.BaseGet(key) as ActivityConfiguration;
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
