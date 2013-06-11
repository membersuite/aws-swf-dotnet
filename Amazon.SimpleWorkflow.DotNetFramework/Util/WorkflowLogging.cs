using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Amazon.SimpleWorkflow.DotNetFramework.Util
{
    public static class WorkflowLogging
    {
        public delegate void LogHandler(string msg, params object[] args);

        public static event LogHandler OnDebug;
        public static event LogHandler OnError;
        //public static event LogHandler OnHeartbeatFailure;

        public static void Debug(string msg, params object[] args)
        {
            
            if (OnDebug != null)
                OnDebug(msg, args);


        }

        public static void Error(string msg, params object[] args)
        {
             
            if (OnError != null)
                OnError(msg, args);

//#if DEBUG
//            File.AppendAllText(@"c:\temp\awserrors.txt", string.Format(msg, args));
//#endif
        }

        //public static void HeartbeatFailure( string activityType, string taskToken, Exception ex)
        //{
        //    try
        //    {
        //        AmazonSimpleWorkflowException sex = ex as AmazonSimpleWorkflowException;

        //        string exception = ex.ToString();
        //        if (sex != null)
        //            exception = string.Format("Simple Workflow Exception:\r\nError Code:{0}\r\nMessage:{1}",
        //                                      sex.ErrorCode, sex.Message);

        //        string msg =
        //            string.Format(
        //                "Activity '{0}' experienced a problem sending heartbeat for token ending '{1}'. The exception was:\r\n{2}",
        //                activityType, taskToken.Substring(taskToken.Length - 5, 5), exception);

        //        Console.WriteLine(msg);

        //        if (OnHeartbeatFailure != null)
        //            OnHeartbeatFailure(msg);
        //    }
        //    catch
        //    {
        //        // suppress issues
        //    }

        //}
    }
}
