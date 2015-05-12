using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DodgeImportLoader;
using XypexCRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;

namespace DodgeImportUnitTest
{
    [TestClass]
    public class VariousTests
    {
        [TestMethod]
        public void TestGetMatchAttributes()
        {
            Dictionary<string, string> attributes = new Dictionary<string,string>();
            attributes.Add("name", "abc");
            attributes.Add("address1_street1", "Haro");
            attributes.Add("address1_City", "Vancouver");
            attributes.Add("postal_code", "V5N 1H3");
            attributes.Add("bank", "RBC");
            attributes.Add("address1_StateOrProvince", "BC");

            OrganizationServiceProxy serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
            serviceProxy.EnableProxyTypes();
            AccountLoader acctLoader = new AccountLoader(serviceProxy, new UserOrTeamAssignment(serviceProxy));

            Dictionary<string, string> matchAttribs = acctLoader.GetSecondaryMatchAttributes(attributes);

            Assert.IsTrue(matchAttribs.Count > 0);
            foreach (string key in matchAttribs.Keys)
            {
                Console.WriteLine(key + ": " + matchAttribs[key]);
            }
        }
        
    }
}
