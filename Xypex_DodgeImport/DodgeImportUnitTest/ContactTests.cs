using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DodgeImportLoader;
using XypexCRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using System.Xml;
using System.Xml.Linq;


namespace DodgeImportUnitTest
{
    [TestClass]
    public class ContactTests
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
        public void TestCreateContact()
        {
            //read input Xelement
            XElement inElem = XElement.Parse(
                    @"<contact>
						<ktc_External_Data_Source_ID>00SCOAZ0006AAK</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>McGraw-Hill Construction Dodge</ktc_External_Data_Source_Name>
						<ktc_External_Data_Source_Match_ID>McGraw-Hill Construction Dodge00SCOAZ0006AAK</ktc_External_Data_Source_Match_ID>
						<firstname>Diana</firstname>
						<lastname>Tymkiw</lastname>
						<jobtitle>CIP, PE, LEED AP</jobtitle>
    					<address1_line1>9191 E San Salvador Dr</address1_line1>
						<address1_county>MARICOPA</address1_county>
						<address1_city>Scottsdale</address1_city>
						<address1_stateorprovince>AZ</address1_stateorprovince>
						<address1_postalcode>852585533</address1_postalcode>
						<address1_country>USA</address1_country>
						<telephone1>4803123481</telephone1>
						<emailaddress1>atymkiw@ScottsdaleAZ.gov</emailaddress1>
						<www-url>www.scottsdaleaz.gov</www-url>
                     </contact>");

            //get an accountId
            Guid AcctId;
            AccountLoader acct = new AccountLoader(serviceProxy, ua);
            Account crmAcct = acct.PrimaryMatch("McGraw-Hill Construction DodgeSCOAZ0006");
            if (crmAcct != null)
            {
                AcctId = crmAcct.Id;
                Console.WriteLine(AcctId.ToString());
            }
            else
            {
                AcctId = Guid.Empty;
                Assert.Fail("Account not found.");
            }
            //pass the account id as attribute
            if (AcctId != Guid.Empty)
            {
                inElem.Add(new XAttribute("accountId", AcctId));
            }         
            //create contact
            ContactLoader contactLdr = new ContactLoader(serviceProxy, ua, inElem);
            Guid contactId;
            contactLdr.CreateRecord(out contactId);

            if (contactId != Guid.Empty)
            {
                Console.WriteLine("Contact created, ContactID: " + contactId);
            }
        }

        [TestMethod]
        public void TestContactPrimaryMatch()
        {
            ContactLoader cntctLdr = new ContactLoader(serviceProxy, ua);
            string identifier = "McGraw-Hill Construction DodgeSCOAZ0006AAK";

            Contact foundContact = cntctLdr.PrimaryMatch(identifier);
            
            Assert.IsNotNull(foundContact);
            Console.WriteLine("Id: " + foundContact.Id);
            Console.WriteLine("Full name: " + foundContact.FullName);
        }

        [TestMethod]
        public void TestFindAccountByKtcSourceId()
        {
            ContactLoader cntctLdr = new ContactLoader(serviceProxy, ua);
            string identifier = "SCOAZ0006AAK";

            //Contact foundContact = cntctLdr.PrimaryMatch(identifier);
            ColumnSet colset = new ColumnSet(new string[] {"contactid", "ktc_external_data_source_name", "ktc_external_data_source_match_id", "ktc_external_data_source_id", "firstname", "fullname", "lastname", "accountid"});
            QueryExpression query = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "ktc_external_data_source_id",
                            Operator = ConditionOperator.Equal,
                            Values = {identifier}
                        }
                    }
                }
            };
            DataCollection<Entity> contacts = serviceProxy.RetrieveMultiple(query).Entities;
            Assert.IsTrue(contacts.Count > 0);
            Contact foundContact = (Contact)contacts[0];

            Assert.IsNotNull(foundContact);
            Console.WriteLine("Id: " + foundContact.Id);
            Console.WriteLine("ktc_external_data_source_name: " + foundContact.ktc_External_Data_Source_Name);
            Console.WriteLine("ktc_external_data_source_match_id" + foundContact.ktc_External_Data_Source_Match_ID) ;
            Console.WriteLine("ktc_external_data_source_id" + foundContact.ktc_External_Data_Source_ID);
            Console.WriteLine("Full name: " + foundContact.FullName);
        }

        [TestMethod]
        public void TestFindAccountByName()
        {
            ContactLoader cntctLdr = new ContactLoader(serviceProxy, ua);
            string identifier = "Alison Tymkiw";
            //string identifier = "SCOAZ0006AAK";

            //Contact foundContact = cntctLdr.PrimaryMatch(identifier);
            ColumnSet colset = new ColumnSet(new string[] { "contactid", "ktc_external_data_source_name", "ktc_external_data_source_match_id", "ktc_external_data_source_id", "firstname", "fullname", "lastname", "accountid" });
            QueryExpression query = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "fullname",
                            Operator = ConditionOperator.Equal,
                            Values = {identifier}
                        }
                    }
                }
            };
            DataCollection<Entity> contacts = serviceProxy.RetrieveMultiple(query).Entities;
            Assert.IsTrue(contacts.Count > 0);
            Contact foundContact = (Contact)contacts[0];

            Assert.IsNotNull(foundContact);
            Console.WriteLine("Id: " + foundContact.Id);
            Console.WriteLine("ktc_external_data_source_name: " + foundContact.ktc_External_Data_Source_Name);
            Console.WriteLine("ktc_external_data_source_match_id" + foundContact.ktc_External_Data_Source_Match_ID);
            Console.WriteLine("ktc_external_data_source_id" + foundContact.ktc_External_Data_Source_ID);
            Console.WriteLine("Full name: " + foundContact.FullName);
        }

        [TestMethod]
        public void TestContactLoadEntity()
        {
            //read input Xelement
            XElement inElem = XElement.Parse(
                    @"<contact>
						<ktc_External_Data_Source_ID>SCOAZ0006AAP</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>McGraw-Hill Construction Dodge</ktc_External_Data_Source_Name>
						<ktc_External_Data_Source_Match_ID>McGraw-Hill Construction DodgeSCOAZ0006AAP</ktc_External_Data_Source_Match_ID>
						<firstname>Diana</firstname>
						<lastname>Pompas</lastname>
						<jobtitle>CIP, PE, LEED AP</jobtitle>
    					<address1_line1>9191 E San Salvador Dr</address1_line1>
						<address1_county>MARICOPA</address1_county>
						<address1_city>Scottsdale</address1_city>
						<address1_stateorprovince>AZ</address1_stateorprovince>
						<address1_postalcode>852585533</address1_postalcode>
						<address1_country>USA</address1_country>
						<telephone1>4803123481</telephone1>
						<emailaddress1>dpompas@shaw.ca</emailaddress1>
						<www-url>www.scottsdaleaz.gov</www-url>
                     </contact>");

            //get an accountId
            Guid AcctId;
            
            AccountLoader acct = new AccountLoader(serviceProxy, ua);
            Account crmAcct = acct.PrimaryMatch("McGraw-Hill Construction DodgeSCOAZ0006");
            if (crmAcct != null)
            {
                AcctId = crmAcct.Id;
                Console.WriteLine(AcctId.ToString());
            }
            else
            {
                AcctId = Guid.Empty;
                Assert.Fail("Account not found.");
            }
            //pass the account id as attribute
            if (AcctId != Guid.Empty)
            {
                inElem.Add(new XAttribute("accountId", AcctId));
            }
            //create contact
            ContactLoader contactLdr = new ContactLoader(serviceProxy, ua, inElem);
            Guid contactId = contactLdr.LoadEntity(inElem);

            if (contactId != Guid.Empty)
            {
                Console.WriteLine("Contact created, ContactID: " + contactId);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestContactSecondaryMatch()
        {
            //read input Xelement
            XElement inElem = XElement.Parse(
                    @"<contact>
						<ktc_External_Data_Source_ID>CONCA5423@@@</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>EDS1</ktc_External_Data_Source_Name>
						<ktc_External_Data_Source_Match_ID>EDS1CONCA5423@@@</ktc_External_Data_Source_Match_ID>
						<firstname>Chris</firstname>
						<lastname>Brown</lastname>
						<jobtitle>CEO</jobtitle>
    					<address1_line1>15036 DELANO ST</address1_line1>
						<address1_county>NORTHERN LA COUNTY</address1_county>
						<address1_city>VAN NUYS</address1_city>
						<address1_stateorprovince>CA</address1_stateorprovince>
						<address1_postalcode>914112016</address1_postalcode>
						<address1_country>USA</address1_country>
						<telephone1>8187780815</telephone1>
						<emailaddress1>info@contourentertainment.com</emailaddress1>
						<www-url>www.contourentertainment.com</www-url>
                     </contact>");

            //get an accountId
            Guid AcctId = Guid.Empty;
            ContactLoader ctctLdr = new ContactLoader(serviceProxy, ua);

            Dictionary<string, string> matches = ctctLdr.GetSecondaryMatchAttributes(inElem);
            Contact crmContact = ctctLdr.SecondaryMatch(inElem);

            Assert.IsNotNull(crmContact, "could not find contact using secondary match");

        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestContactUpdateSelected()
        {
            XElement inElem = XElement.Parse(
                    @"<contact>
						<ktc_External_Data_Source_ID>CONCA5423@@@</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>EDS1</ktc_External_Data_Source_Name>
						<ktc_External_Data_Source_Match_ID>EDS1CONCA5423@@@</ktc_External_Data_Source_Match_ID>
						<firstname>Chris</firstname>
						<lastname>Brown</lastname>
						<jobtitle>CEO</jobtitle>
    					<address1_line1>15036 DELANO ST</address1_line1>
						<address1_county>NORTHERN LA COUNTY</address1_county>
						<address1_city>VAN NUYS</address1_city>
						<address1_stateorprovince>CA</address1_stateorprovince>
						<address1_postalcode>914112016</address1_postalcode>
						<address1_country>USA</address1_country>
						<telephone1>8187780815</telephone1>
						<emailaddress1>info@contourentertainment.com</emailaddress1>
						<www-url>www.contourentertainment.com</www-url>
                     </contact>");
            ContactLoader ldr = new ContactLoader(serviceProxy, ua, inElem);
            Contact CRMct = ldr.SecondaryMatch(inElem);

            Assert.IsNotNull(CRMct, " could not find contact using secondary match");

            ldr.UpdateExtDataSourceFieldsInRecord();

            Contact CRMct1 = ldr.PrimaryMatch("");
            Assert.IsNotNull(CRMct1, " could not find contact after update");
            Console.WriteLine("source match id: " + CRMct1.ktc_External_Data_Source_Match_ID);
        }

        [TestMethod]
        public static void TestDeleteContactByName(string fullname)
        {
            ColumnSet colset = new ColumnSet(new string[] { "contactid", "fullname"});
            QueryExpression query = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "fullname",
                            Operator = ConditionOperator.Equal,
                            Values = {fullname}
                        }
                    }
                }
            };
            DataCollection<Entity> contacts = serviceProxy.RetrieveMultiple(query).Entities;
            Assert.IsTrue(contacts.Count > 0);
            Contact foundContact = (Contact)contacts[0];

            Assert.IsNotNull(foundContact);
            foreach (Entity e in contacts)
            {
                foundContact = (Contact)e;
                serviceProxy.Delete(Contact.EntityLogicalName, foundContact.Id);
            }

        }

        [ClassCleanup]
        public static void ContactCleanup()
        {
            TestDeleteContactByName("Diana Tymkiw");
        }
    }
}

