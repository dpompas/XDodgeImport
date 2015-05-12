using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DodgeImportLoader;
using XypexCRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;

namespace DodgeImportUnitTest
{
    [TestClass]
    public class OwnerAssignmentTests
    {
        public static OrganizationServiceProxy serviceProxy;
        public static UserOrTeamAssignment ua;

        [ClassInitialize]
        public static void EstablishConnection(TestContext DodgeTests)
        {
            serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
            serviceProxy.EnableProxyTypes();
            ua = new UserOrTeamAssignment(serviceProxy);

        }

        [TestMethod]
        public void TestGetIntegrationUserId()
        {
            EntityReference userEntRef = UserOrTeamAssignment.IntegrationUserId(serviceProxy);
            Assert.IsNotNull(userEntRef);

            Console.WriteLine(userEntRef.LogicalName);
            Console.WriteLine(userEntRef.Id);
        }

        [TestMethod]
        public void TestGetProjectAdminTeam()
        {
            EntityReference projAdminER = UserOrTeamAssignment.ProjectAdminTeamId(serviceProxy);
            Assert.IsNotNull(projAdminER);
            Console.WriteLine(projAdminER.LogicalName);
            Console.WriteLine(projAdminER.Id);
        }

    }
}
