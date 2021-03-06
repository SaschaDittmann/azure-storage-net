﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableClientTest.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class CloudTableClientTest : TableTestBase
    {
        #region Locals + Ctors
        public CloudTableClientTest()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        static List<CloudTable> createdTables = new List<CloudTable>();

        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // 20 random tables
            for (int m = 0; m < 20; m++)
            {
                CloudTable tableRef = tableClient.GetTableReference(GenerateRandomTableName());
                tableRef.CreateIfNotExistsAsync().AsTask().Wait();
                createdTables.Add(tableRef);
            }

            prefixTablesPrefix = "prefixtable" + GenerateRandomTableName();
            // 20 tables with known prefix
            for (int m = 0; m < 20; m++)
            {
                CloudTable tableRef = tableClient.GetTableReference(prefixTablesPrefix + m.ToString());
                tableRef.CreateIfNotExistsAsync().AsTask().Wait();
                createdTables.Add(tableRef);
            }
        }

        private static string prefixTablesPrefix = null;

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            foreach (CloudTable t in createdTables)
            {
                try
                {
                    t.DeleteIfExistsAsync().AsTask().Wait();
                }
                catch (Exception)
                {
                }
            }
        }

        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Ctor Tests
        [TestMethod]
        [Description("A test checks constructor of CloudTableClient.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientConstructor()
        {
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint);
            CloudTableClient tableClient = new CloudTableClient(baseAddressUri, TestBase.StorageCredentials);
            Assert.IsTrue(tableClient.BaseUri.ToString().StartsWith(TestBase.TargetTenantConfig.TableServiceEndpoint));
            Assert.AreEqual(TestBase.StorageCredentials, tableClient.Credentials);
            Assert.AreEqual(AuthenticationScheme.SharedKey, tableClient.AuthenticationScheme);
        }
        #endregion

        #region List Tables Segmented

        [TestMethod]
        [Description("Test List Tables Segmented Basic Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task ListTablesSegmentedBasicAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            do
            {
                segment = await tableClient.ListTablesSegmentedAsync(segment != null ? segment.ContinuationToken : null);
                totalResults.AddRange(segment);
            }
            while (segment.ContinuationToken != null);

           //  Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
        }

        [TestMethod]
        [Description("Test List Tables Segmented MaxResults Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task ListTablesSegmentedMaxResultsAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                segment = await tableClient.ListTablesSegmentedAsync(string.Empty, 10, segment != null ? segment.ContinuationToken : null, null, null);
                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            // Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
            Assert.IsTrue(segCount >= totalResults.Count / 10);
        }

        [TestMethod]
        [Description("Test List Tables Segmented With Prefix Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task ListTablesSegmentedWithPrefixAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                segment = await tableClient.ListTablesSegmentedAsync(prefixTablesPrefix, null, segment != null ? segment.ContinuationToken : null, null, null);
                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, 20);
            foreach (CloudTable tbl in totalResults)
            {
                Assert.IsTrue(tbl.Name.StartsWith(prefixTablesPrefix));
            }
        }

        [TestMethod]
        [Description("Test List Tables Segmented with Shared Key Lite")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableClientListTablesSegmentedSharedKeyLite()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            do
            {
                segment = await tableClient.ListTablesSegmentedAsync(segment != null ? segment.ContinuationToken : null);
                totalResults.AddRange(segment);
            }
            while (segment.ContinuationToken != null);

            Assert.IsTrue(totalResults.Count > 0);
        }

        #endregion

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableClientGetServiceStatsAsync()
        {
            AssertSecondaryEndpoint();

            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(await client.GetServiceStatsAsync());
        }


        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudTableClientServerTimeoutAsync()
        {
            CloudTableClient client = GenerateCloudTableClient();

            string timeout = null;
            OperationContext context = new OperationContext();
            context.SendingRequest += (sender, e) =>
            {
                IDictionary<string, string> query = HttpWebUtility.ParseQueryString(e.RequestUri.Query);
                if (!query.TryGetValue("timeout", out timeout))
                {
                    timeout = null;
                }
            };

            TableRequestOptions options = new TableRequestOptions();
            await client.GetServicePropertiesAsync(null, context);
            Assert.IsNull(timeout);
            await client.GetServicePropertiesAsync(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            await client.GetServicePropertiesAsync(null, context);
            Assert.AreEqual("90", timeout);
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            await client.GetServicePropertiesAsync(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            await client.GetServicePropertiesAsync(options, context);
            Assert.IsNull(timeout);
        }
    }
}
