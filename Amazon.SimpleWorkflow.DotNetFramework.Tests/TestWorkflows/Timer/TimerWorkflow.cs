using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SimpleWorkflow.DotNetFramework.Base.RecurringTask;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests.TestWorkflows.Timer 
{
    public class TimerWorkflow : RecurringTaskWorkflowDecider
    {
        protected override Type getActivityTask()
        {
            return typeof(TimerWorkflowActivity);
        }

        //protected override int getMaximumNumberOfExecutions()
        //{
        //    return 3;
        //}
        protected override TimeSpan getTimerInterval()
        {
            return TimeSpan.FromSeconds(5);
        }

        //protected override bool TerminateWorkflowAfterMaxExecutions
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}
    }
}
