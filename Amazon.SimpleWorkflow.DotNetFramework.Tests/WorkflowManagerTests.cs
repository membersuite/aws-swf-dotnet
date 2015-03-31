using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleWorkflow.DotNetFramework.Configuration;
using Amazon.SimpleWorkflow.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.SimpleWorkflow.DotNetFramework.Tests
{
    [TestClass]
    public class WorkflowManagerTests
    {
        [TestMethod]
        public void AutomatedWorkflowTypeGeneration_Test()
        {
            // makes sure that the system automatically generates all workflow types
            // we'll start by deleting any existing workflow types

            // IMPORTANT - you need to go to the app config and update the workflow version with an unused version
            // or this test will FAIL. You can't delete a workflow type, only deprecate it
            const string workflowVersion = "15";
            AmazonSimpleWorkflowClient client = new AmazonSimpleWorkflowClient();

            var domain = SimpleWorkflowFoundationSettings.Settings.Domain;
            var workflowTypes = client.ListWorkflowTypes(new Model.ListWorkflowTypesRequest()
                {
                    Domain = (domain),
                    RegistrationStatus = ("REGISTERED")
                });

            //// let's go through and delete all the types
            //foreach (var type in workflowTypes.ListWorkflowTypesResult.WorkflowTypeInfos.TypeInfos)
            //    client.DeprecateWorkflowType(new DeprecateWorkflowTypeRequest().WithDomain(domain).WithWorkflowType(type.WorkflowType));

            // now, based on the configuration, the workflow manager should automatically create the workflow
            WorkflowManager.Initialize();
            // This breaks all the other workflow tests! WorkflowManager.Shutdown();

            // now, let's see if our workflow was cretaed
            var workflowTypeDetail = client.DescribeWorkflowType(new DescribeWorkflowTypeRequest()
            {
                Domain = (domain),
                WorkflowType = new WorkflowType() { Name = ("TestDecider"), Version = (workflowVersion) }
            })
                .WorkflowTypeDetail;
            var config = workflowTypeDetail.Configuration;

            // if the description failed, we would have had an exception by now
            Assert.AreEqual("ABANDON", config.DefaultChildPolicy);
            Assert.AreEqual("232", config.DefaultExecutionStartToCloseTimeout);
            Assert.AreEqual("SomeTaskList", config.DefaultTaskList.Name);
            Assert.AreEqual("22", config.DefaultTaskStartToCloseTimeout);
            Assert.AreEqual("SomeDescription", workflowTypeDetail.TypeInfo.Description);

        }

        [TestMethod]
        public void AutomatedActivityTypeGeneration_Test()
        {
            // makes sure that the system automatically generates all activity types

            // IMPORTANT - you need to go to the app config and update the activity version with an unused version
            // or this test will FAIL. You can't delete a workflow type, only deprecate it
            const string activityVersion = "5";
            AmazonSimpleWorkflowClient client = new AmazonSimpleWorkflowClient();

            var domain = SimpleWorkflowFoundationSettings.Settings.Domain;

            // now, based on the configuration, the workflow manager should automatically create the workflow
            WorkflowManager.Initialize();
            // This breaks all the other workflow tests! WorkflowManager.Shutdown();

            // now, let's see if our workflow was cretaed
            var activityTypeDetail = client.DescribeActivityType(new DescribeActivityTypeRequest()
            {
                Domain = (domain),
                ActivityType = (new ActivityType() { Name = ("TestActivity"), Version = (activityVersion) })
            }).ActivityTypeDetail;
            var config = activityTypeDetail.Configuration;

            // if the description failed, we would have had an exception by now


            Assert.AreEqual("4324", config.DefaultTaskHeartbeatTimeout);
            Assert.AreEqual("122", config.DefaultTaskScheduleToCloseTimeout);
            Assert.AreEqual("954", config.DefaultTaskScheduleToStartTimeout);
            Assert.AreEqual("SomeTaskList2", config.DefaultTaskList.Name);
            Assert.AreEqual("323", config.DefaultTaskStartToCloseTimeout);
            Assert.AreEqual("ADescription", activityTypeDetail.TypeInfo.Description);

        }
    }
}
