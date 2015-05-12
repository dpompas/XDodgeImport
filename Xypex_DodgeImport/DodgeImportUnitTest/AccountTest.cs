using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DodgeImportLoader;
using XypexCRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using System.Xml.Linq;

namespace DodgeImportUnitTest
{
    [TestClass]
    public class AccountLoadTest
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
        [TestCategory("CRMUpdate")]
        public void TestAccountUpdate()
        {
            try
            {
                AccountLoader testAccount = new AccountLoader(serviceProxy, new UserOrTeamAssignment(serviceProxy));
                Account findAccount = testAccount.PrimaryMatch("McGraw-Hill Constructi201300423713");
                Assert.IsNotNull(findAccount);

                testAccount.LoadAttributes = new Dictionary<string, string>();
                if (!testAccount.LoadAttributes.ContainsKey("name"))
                {
                    testAccount.LoadAttributes.Add("name", string.Empty);
                }

                if (findAccount.Name.Contains("updated"))
                {
                    //Assert.Fail("findAccount.Name.Length is: " + findAccount.Name.Length.ToString());
                    testAccount.LoadAttributes["name"] = findAccount.Name.Substring(8);
                }
                else
                {
                    testAccount.LoadAttributes["name"] = "updated " + findAccount.Name;
                }
                testAccount.LoadAttributes.Add("address1_line1", "updated");
                //AccountLoader UpdateRecord is obsolete, we no longer update all the fields in Account
                //testAccount.UpdateRecord();

                findAccount.Name = testAccount.LoadAttributes["name"];
                findAccount.Address1_Line1 = testAccount.LoadAttributes["address1_line1"];
                serviceProxy.Update(findAccount);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountAdd()
        {
            Dictionary<string, string> accountAttributes = new Dictionary<string, string>();
            accountAttributes.Add("name", "Eigth Coffee");
            accountAttributes.Add("address1_line1", "5 Hana Road");
            accountAttributes.Add("address1_city", "Kula");

            AccountLoader acctLoader = new AccountLoader(serviceProxy, ua, accountAttributes);
            try
            {
                Guid accountId;
                acctLoader.CreateRecord(out accountId);
                if (accountId == Guid.Empty)
                    Assert.Fail("no account Id created.");
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountAddNotMappedAttrib()
        {
            Dictionary<string, string> accountAttributes = new Dictionary<string, string>();
            accountAttributes.Add("name", "Drive Coffee");
            accountAttributes.Add("address1_line3", "5 Hana Road");
            accountAttributes.Add("address1_city", "Kula");
            accountAttributes.Add("address1_latitude", "23.3");

            AccountLoader acctLoader = new AccountLoader(serviceProxy, ua, accountAttributes);
            try
            {
                Guid accountId;
                acctLoader.CreateRecord(out accountId);
                if (accountId == Guid.Empty)
                    Assert.Fail("no account Id created.");
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountMatchPrimary()
        {
            Dictionary<string, string> accountAttributes = new Dictionary<string, string>();
            accountAttributes.Add("ktc_External_Data_Source_Match_ID", "McGraw-Hill Constructi201300423713");

            AccountLoader testAccount = new AccountLoader(serviceProxy, ua);
            try
            {
                Account foundAcct = testAccount.PrimaryMatch("McGraw-Hill Constructi201300423713");
                Assert.IsNotNull(foundAcct);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountMatchSecondary()
        {
            AccountLoader testAccount = new AccountLoader(serviceProxy, ua);
            Dictionary<string, string> accountAttributes = new Dictionary<string, string>();
            accountAttributes.Add("name", "Drive Coffee");

            Account findAccount = testAccount.SecondaryMatch(accountAttributes);

            Assert.IsNotNull(findAccount);
            Console.WriteLine("name: " + findAccount.Name);
            Console.WriteLine("external data source id: " + findAccount.ktc_External_Data_Source_ID);
            Console.WriteLine("external data source name: " + findAccount.ktc_External_Data_Source_Name);
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountUpdateSelectedFields()
        {
            XElement inElem = XElement.Parse(
                    @"<contact>
						<ktc_External_Data_Source_ID>CONCA5423</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>EDS1</ktc_External_Data_Source_Name>
						<ktc_External_Data_Source_Match_ID>EDS1CONCA5423</ktc_External_Data_Source_Match_ID>
						<firstname>Chris</firstname>
						<lastname>Brown</lastname>
                        <name>Contour Entertainment, Inc.</name>
						<jobtitle>CEO</jobtitle>
    					<address1_line1>15036 DELANO ST</address1_line1>
						<address1_county>NORTHERN LA COUNTY</address1_county>
						<address1_city>VAN NUYS</address1_city>
						<address1_stateorprovince>CA</address1_stateorprovince>
						<address1_postalcode>914112016</address1_postalcode>
						<address1_country>USA</address1_country>
						<telephone1>8187780815</telephone1>
						<emailaddress1>info@contourentertainment.com</emailaddress1>
						<websiteurl>www.contourentertainment.com</websiteurl>
                        <ktc_External_Source_Profile_URL>http://network2.construction.com/ExternalClick.aspx?source=NETWORK_EXPRESS&amp;page=Company.aspx&amp;companyId=CKS000001121861</ktc_External_Source_Profile_URL>
                      </contact>");
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            attributes.Add("name", "Contour Entertainment, Inc.");
            attributes.Add("address1_line1", "15036 DELANO ST");
            attributes.Add("address1_city", "VAN NUYS");
            attributes.Add("address1_stateorprovince", "CA");
            attributes.Add("address1_country", "USA");
            attributes.Add("ktc_External_Data_Source_Name", "testEDS");
            attributes.Add("ktc_External_Data_Source_ID", "CONCA5423");
            attributes.Add("address1_postalcode", "914112016");
            attributes.Add("telephone1", "8187780815");
            attributes.Add("websiteurl", "www.contourentertainment.com");
            attributes.Add("ktc_External_Source_Profile_URL", "http://network2.construction.com/ExternalClick.aspx?source=NETWORK_EXPRESS&amp;page=Company.aspx&amp;companyId=CKS000001121861");
            AccountLoader lder = new AccountLoader(serviceProxy, ua, attributes);

            Dictionary<string, string> matchingAttribs = lder.GetSecondaryMatchAttributes(attributes);
            Account crmAcct = lder.SecondaryMatch(matchingAttribs);

            Assert.IsNotNull(crmAcct, "could not find account using secondary match");
            Console.WriteLine("Id : " + crmAcct.Id + ", name: " + crmAcct.Name);

            lder.UpdateSelectedFieldsInRecord();

            Account crmAcct1 = lder.PrimaryMatch("testEDSCONCA5423");
            Assert.IsNotNull(crmAcct1, "could not find account after update");
            Console.WriteLine("Id : " + crmAcct1.Id + ", name: " + crmAcct1.Name);

        }


        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestAccountAssign()
        {
            AccountLoader testAccount = new AccountLoader(serviceProxy, ua);
            Assert.Fail("test not implemented");
        }

        [ClassCleanup]
        [TestCategory("CRMUpdate")]
        public static void TearDownAndCleanup()
        {
            ColumnSet cols = new ColumnSet(new string[] { "accountid"});
            
            //identify and return record for which ktc_External_Data_Source_Match_ID has the value provided in primaryIdentifier
            QueryExpression findByName = new QueryExpression 
            {
                EntityName = Account.EntityLogicalName,
                ColumnSet = cols,
                Criteria = new FilterExpression 
                {
                    FilterOperator = LogicalOperator.Or, 
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Fifth Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Sixth Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Seventh Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Eigth Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Eighth Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"Drive Coffee"}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = {"updated Drive Coffee"}
                        }
                    }
                }
            };

            Account acct;
            EntityCollection entCol = serviceProxy.RetrieveMultiple(findByName);
            foreach (Entity acctEnt in entCol.Entities)
            {
                acct = (Account) acctEnt;
                serviceProxy.Delete(Account.EntityLogicalName, acct.AccountId.Value );

            }
        }
    }
}
