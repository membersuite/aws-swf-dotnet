using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base.RecurringTask;

namespace Amazon.SimpleWorkflow.DotNetFramework.Samples.Workflows.Recurring
{
    class SampleRecurringWorkflow : RecurringTaskWorkflowDecider
    {
        protected override Type getActivityTask()
        {
            return typeof(SampleRecurringWorkflowTask);
        }

        

        protected override TimeSpan getTimerInterval()
        {
            return TimeSpan.FromSeconds(1);
        }
    }
}
