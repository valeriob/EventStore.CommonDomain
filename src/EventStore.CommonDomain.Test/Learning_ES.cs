using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace EventStore.CommonDomain.Test
{
    /// <summary>
    /// Summary description for Learning_ES
    /// </summary>
    [TestClass]
    public class Learning_ES
    {
        public Learning_ES()
        {
            //
            // TODO: Add constructor logic here
            //
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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void read_all_events()
        {
            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));

            try
            {
               
                //var events = connection.ReadEventStreamForward("$all", 0, int.MaxValue);
                var events = connection.ReadAllEventsForward(ClientAPI.Position.Start, int.MaxValue);
            }
            catch (Exception ex)
            { 
            }
        }
    }
}
