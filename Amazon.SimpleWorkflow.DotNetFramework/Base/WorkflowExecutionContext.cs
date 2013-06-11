using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.S3.Model;
using Amazon.SimpleWorkflow.DotNetFramework;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.DotNetFramework.Constants;
using Amazon.SimpleWorkflow.DotNetFramework.Util;
using Amazon.SimpleWorkflow.Model;


namespace Amazon.SimpleWorkflow.DotNetFramework.Base
{
    public static class WorkflowExecutionContext
    {
        public delegate void InitializeThreadForTaskDelegate(object sender);
        public static event InitializeThreadForTaskDelegate OnInitializeThreadForTask;

        #region Activities

        [ThreadStatic]
        private static ActivityTask _currentActivityTask;

        
        public static ActivityTask CurrentActivityTask
        {
            get
            {

                if (_currentActivityTask == null)
                    throw new ApplicationException("There is no current activity task.");

                return _currentActivityTask;
            }
        }

        public static void InitializeThread(ActivityTask activityTask)
        {
            _currentActivityTask = activityTask;
            _current = activityTask.WorkflowExecution;
            _currentDecisionTask = null;        // IMPORTANT!! Threads are reused!!

            if (OnInitializeThreadForTask != null)
                OnInitializeThreadForTask(activityTask);
            
        }

        public static bool IsInActivity
        {
            get { return _currentActivityTask != null; }
        }

        #endregion

        #region Deciders

        [ThreadStatic]
        private static DecisionTask _currentDecisionTask;

        public static bool IsInDecider
        {
            get { return _currentDecisionTask != null; }
        }

        public static DecisionTask CurrentDecisionTask
        {
            get
            {
                if (_currentDecisionTask == null)
                    throw new ApplicationException("There is no current decision task.");

                return _currentDecisionTask;
            }
        }

        public static void InitializeThread(DecisionTask decisionTask)
        {
            _currentDecisionTask = decisionTask;
            _currentActivityTask = null;        // IMPORTANT!! Threads are reused!!
            _current = decisionTask.WorkflowExecution;

            if (OnInitializeThreadForTask != null)
                OnInitializeThreadForTask(decisionTask);
            
        }

        #endregion

        #region Execution

        [ThreadStatic]
        private static WorkflowExecution _current;

        public static WorkflowExecution Current
        {
            get
            {
                if (_current == null)
                    throw new ApplicationException("There is no current workflow execution context.");

                return _current;
            }
        }

        public static string TaskToken
        {
            get {
                if (_currentDecisionTask != null) return _currentDecisionTask.TaskToken;
                if (_currentActivityTask != null) return _currentActivityTask.TaskToken;

                return null;
            }
             
        }

        #endregion

        #region Activity-Related Utilities

        public static string GetActivityInput()  
        {
            if ( !IsInActivity ) throw new NotSupportedException("This method can only be called from inside an activity.");

            string input = _getInputFromS3IfNecessary(WorkflowExecutionContext.CurrentActivityTask.Input);
            if (input == null) return null;

            return input;
        }

        #endregion

        #region Workflow-Related Utilities

        // the maximum size of an input
        public const int MAX_INPUT_SIZE = 32768;
        public const string CHECK_S3_PREFIX = "@@$InputIsInS3!@~|";

        public static string GetWorkflowInput()
        {
            if (WorkflowExecutionContext.IsInDecider)
            {
                var decisionTask = WorkflowExecutionContext.CurrentDecisionTask;
                if (decisionTask.Events.Count == 0 ||
                    decisionTask.Events[0].WorkflowExecutionStartedEventAttributes == null)
                    return null;

                return _getInputFromS3IfNecessary(decisionTask.Events[0].WorkflowExecutionStartedEventAttributes.Input);
            }

            var execution = WorkflowExecutionContext.Current;

            var history =
                WorkflowManager.SWFClient.GetWorkflowExecutionHistory(new GetWorkflowExecutionHistoryRequest().
                                                                          WithDomain(
                                                                              WorkflowManager.Domain).
                                                                          WithMaximumPageSize(1).WithExecution(execution))
                    .GetWorkflowExecutionHistoryResult.History;

            if (history.Events.Count == 0 || history.Events[0].WorkflowExecutionStartedEventAttributes == null)
                return null;

            return history.Events[0].WorkflowExecutionStartedEventAttributes.Input;
        }



        
        public static MarkerRecordedEventAttributes GetWorkflowVariable(string variableName)
        {
            
            var markerEvent = FindMostRecentEvent( x =>
                                                                         x.EventType ==
                                                                         WorkflowHistoryEventTypes.MarkerRecorded &&
                                                                         x.MarkerRecordedEventAttributes != null &&
                                                                         x.MarkerRecordedEventAttributes.MarkerName ==
                                                                         variableName);



            if (markerEvent != null)
                return markerEvent.MarkerRecordedEventAttributes;

            return null; // nothing to return

        }

        public static WorkflowExecutionDetail DescribeCurrentlyExecutingWorkflow()
        {
            return WorkflowManager.SWFClient.DescribeWorkflowExecution(new DescribeWorkflowExecutionRequest().WithDomain(WorkflowManager.Domain)
                .WithExecution(Current)).DescribeWorkflowExecutionResult.WorkflowExecutionDetail ;
        }

        public static string PrepareInputForWorkflow(string input)
        {
            if (input == null) return null;

            if (input.Length < MAX_INPUT_SIZE)
                return input;

            // we have to save this to S3
            var s3 = Amazon.AWSClientFactory.CreateAmazonS3Client();
            PutObjectRequest req = new PutObjectRequest();
            req.BucketName = SimpleWorkflowFoundationSettings.Settings.S3BucketName;


            req.Key = Guid.NewGuid().ToString();

            // compress the entire packet
            var bytes = Encoding.ASCII.GetBytes(input);
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            req.WithInputStream(ms);

            // put the object in  S3
            s3.PutObject(req);

            
            if (File.Exists(@"c:\temp\inputToS3.txt"))
                File.Delete(@"c:\temp\inputToS3.txt");
            File.WriteAllBytes(@"c:\temp\inputToS3.txt", bytes);    // now write it out

            return CHECK_S3_PREFIX + req.Key;
        }

        private static string _getInputFromS3IfNecessary(string input)
        {
            if (input == null)
                return null;

            if (!input.StartsWith(CHECK_S3_PREFIX)) // input is not in S3
                return input;

            // ok, so the input is in S3
            // first, we need to find the guid
            string[] parts = input.Split('|');
            if (parts.Length < 2) // something's wrong
                return null;

            string guid = parts[1];

            var s3 = Amazon.AWSClientFactory.CreateAmazonS3Client();
            GetObjectRequest req = new GetObjectRequest();
            req.BucketName = SimpleWorkflowFoundationSettings.Settings.S3BucketName;

            req.WithKey(guid);

            var response = s3.GetObject(req);

            MemoryStream ms = new MemoryStream();
            response.ResponseStream.CopyTo(ms);

            var bytes = ms.ToArray();
            var s3Data = Encoding.ASCII.GetString(bytes);

            if (File.Exists(@"c:\temp\outputFromS3.txt"))
                File.Delete(@"c:\temp\outputFromS3.txt");
            File.WriteAllBytes(@"c:\temp\outputFromS3.txt", bytes);    // now write it out

            return s3Data;
        }

        /// <summary>
        /// Responds with a decision that the workflow is done
        /// </summary>
        /// <param name="result"></param>
        public static void CompleteCurrentWorkflow(string result)
        {
            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(new RespondDecisionTaskCompletedRequest()
            .WithTaskToken(WorkflowExecutionContext.CurrentDecisionTask.TaskToken).WithDecisions(new List<Decision> {
                 DecisionGenerator.GenerateWorkflowCompletedDecision( result ) }));
        }

        /// <summary>
        /// Responds with a decision that the workflow is done
        /// </summary>
        /// <param name="result"></param>
        public static void CancelCurrentWorkflow(string result)
        {
            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(new RespondDecisionTaskCompletedRequest()
            .WithTaskToken(WorkflowExecutionContext.CurrentDecisionTask.TaskToken).WithDecisions(new List<Decision> {
                 DecisionGenerator.GenerateWorkflowCancelledDecision( result ) }));
        }

        /// <summary>
        /// Responds with a decision that the workflow has failed
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="details"></param>
        public static void FailCurrentWorkflow(string reason, string details)
        {
            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(new RespondDecisionTaskCompletedRequest()
            .WithTaskToken(WorkflowExecutionContext.CurrentDecisionTask.TaskToken).WithDecisions(new List<Decision> {
                 DecisionGenerator.GenerateWorkflowFailedDecision( reason, details ) }));
        }
        #endregion




        #region History-Related Utilities
        /// <summary>
        /// Finds the most recent event based on the event history from a decision task.
        /// </summary>
        /// <param name="matchCriteria">The match criteria.</param>
        /// <returns>HistoryEvent.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// swfClient
        /// or
        /// taskToDecide
        /// or
        /// matchCriteria
        /// </exception>
        /// <remarks>If the decision task has a NextPageToken, this method reverts to the
        /// overload that queries the workflow history.</remarks>
        public static HistoryEvent FindMostRecentEvent( Predicate<HistoryEvent> matchCriteria)
        {
            if (!IsInDecider)
                return FindMostRecentEvent( Current, matchCriteria);    // need to use workflow execution
            var taskToDecide = CurrentDecisionTask;
            
            
            if (taskToDecide.NextPageToken != null)   // there are other tasks to look at
                return FindMostRecentEvent(Current, matchCriteria);

            for (int i = taskToDecide.Events.Count - 1; i >= 0; i--)
            {
                var eventToCheck = taskToDecide.Events[i];
                if ( matchCriteria == null || matchCriteria(eventToCheck))  // it's a match
                    return eventToCheck;
            }

            return null;    // no event matches any criteria
        }

        public static HistoryEvent FindMostRecentEvent( WorkflowExecution execution, Predicate<HistoryEvent> matchCriteria)
        {
            
            string nextPageToken = null;

            do
            {
                //if (nextPageToken != null)
                //    LogWithContext.Debug ("Getting next batch of events...");
                var history =
                    WorkflowManager.SWFClient.GetWorkflowExecutionHistory(new GetWorkflowExecutionHistoryRequest().
                                                                              WithDomain(WorkflowManager.Domain)
                                                                              .WithExecution(execution).
                                                                              WithReverseOrder(true).WithNextPageToken(
                                                                                  nextPageToken)).
                        GetWorkflowExecutionHistoryResult.History;

                nextPageToken = history.NextPageToken; // set the next page token

                foreach (var eventToCheck in history.Events) // remember this is in reverse order now
                {
                    //LogWithContext.Debug("Examining event #{0}...", eventToCheck.EventId);
                    if (matchCriteria == null || matchCriteria(eventToCheck)) // it's a match
                    {
                        //LogWithContext.Debug("MATCH for criteria for event #{0}...", eventToCheck.EventId);
                        return eventToCheck;
                    }


                }


            } while (nextPageToken != null);    // keep going until there are no more pages

            return null;    // ok, no event was found
        }

        public static List<HistoryEvent> GetAllEvents(WorkflowExecution execution, Predicate<HistoryEvent> matchCriteria)
        {

            string nextPageToken = null;
            List<HistoryEvent> events = new List<HistoryEvent>();
            do
            {
                var history =
                    WorkflowManager.SWFClient.GetWorkflowExecutionHistory(new GetWorkflowExecutionHistoryRequest().
                                                                              WithDomain(WorkflowManager.Domain)
                                                                              .WithExecution(execution).
                                                                              WithReverseOrder(true).WithNextPageToken(
                                                                                  nextPageToken)).
                        GetWorkflowExecutionHistoryResult.History;

                nextPageToken = history.NextPageToken; // set the next page token

                foreach (var eventToCheck in history.Events)   // remember this is in reverse order now
                    if (matchCriteria == null || matchCriteria(eventToCheck))  // it's a match
                        events.Add( eventToCheck );


            } while (nextPageToken != null);    // keep going until there are no more pages

            return events;    // ok, no event was found
        }

        #endregion

        public static HistoryEvent FindMostRecentActivityRelatedEvent()
        {
            return WorkflowExecutionContext.FindMostRecentEvent(
               x => x.EventType == WorkflowHistoryEventTypes.ActivityTaskCanceled ||
                    x.EventType == WorkflowHistoryEventTypes.ActivityTaskCompleted ||
                       x.EventType == WorkflowHistoryEventTypes.ActivityTaskFailed ||
                           x.EventType == WorkflowHistoryEventTypes.ActivityTaskTimedOut);
        }

        public static void ContinueCurrentWorkflowAsNew(string result)
        {
            WorkflowManager.SWFClient.RespondDecisionTaskCompleted(
                       new RespondDecisionTaskCompletedRequest()
                           .WithTaskToken(WorkflowExecutionContext.CurrentDecisionTask.TaskToken)
                           .WithDecisions(new List<Decision> {
                                DecisionGenerator.GenerateContinueWorkflowAsNewDecision( result ) 
                                }));
        }
    }
}
