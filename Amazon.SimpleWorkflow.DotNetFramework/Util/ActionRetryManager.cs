using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Amazon.SimpleWorkflow.DotNetFramework.Util
{
    /// <summary>
    /// A helper class for actions that you would like to retry using an exponential backoff
    /// </summary>
    public class ActionRetryManager
    {
        protected long _retryInterval;
        protected long _maximumBackOff;
        protected string _activityName;
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionRetryManager" /> class.
        /// </summary>
        /// <param name="activityName">Name of the activity.</param>
        /// <param name="retryInterval">The retry invterval in milliseconds.</param>
        /// <param name="maximumBackOff">The maximum back off time in milliseconds before an exception is thrown.</param>
        public ActionRetryManager( string activityName, long retryInterval, long maximumBackOff)
        {
            _retryInterval = retryInterval;
            _maximumBackOff = maximumBackOff;
            _activityName = activityName;
        }

        protected long exponentialBackOffCount = 0;
         public long CalculateExponentialBackOff()
        {
            long backOffCount = Interlocked.Increment(ref exponentialBackOffCount);

            // first, we have a base amount
            long backOffValue = backOffCount * _retryInterval;

            // now we need a random value between 0 and 900 ms
            backOffValue += new Random().Next(0, 900);

            if (backOffValue > _maximumBackOff)
                throw new ApplicationException(string.Format("{0} has exceeded the backoff threshold. We are giving up.",
                    _activityName));

            return backOffValue;

        }

        /// <summary>
        /// Resets the exponential backoff, indicating a successful polling.
        /// </summary>
        public void ResetExponentialBackoff()
        {
            exponentialBackOffCount = 0;
        }

    }
}
