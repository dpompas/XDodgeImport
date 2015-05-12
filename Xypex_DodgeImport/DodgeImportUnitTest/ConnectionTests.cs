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
    public class ConnectionTests
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
        public void TestRetrieveConnectionRoles()
        {
            //ConnectionLoader connLdr = new ConnectionLoader(serviceProxy);

            List<string> categs = new List<string> { "Stakeholder", "Project Bidder" };
            ConnectionRoles.RetrieveConnectionRolesOfInterest(categs, serviceProxy);

            Assert.IsTrue(ConnectionRoles.ConnectionRolesDictionary != null);
            foreach (string key in ConnectionRoles.ConnectionRolesDictionary.Keys)
            {
                Console.WriteLine(key + ", " + ConnectionRoles.ConnectionRolesDictionary[key]);
            }
        }
/*
        [TestMethod]
        public void TestGetCategOptionValues()
        {
            ConnectionLoader connLdr = new ConnectionLoader(serviceProxy);

            List<string> categs = new List<string> { "Stakeholder", "Project Bidder" };
            int[] optionValues = connLdr.GetCategoryOptionSetValues(categs);

            Assert.IsNotNull(optionValues);
            foreach(int i in optionValues)
                Console.WriteLine(i.ToString());
        }
*/
        [TestMethod]
        public void TestCreateConnection()
        {
           ConnectionLoader connLdr = new ConnectionLoader(serviceProxy, ua);
           ConnectionRoles.RetrieveConnectionRolesOfInterest(new List<string>(new string[] { "Stakeholder", "Project Bidder" }), serviceProxy);
            //get existing project Id
            ktc_project project;
            string primaryIdentifier = "McGraw-Hill Construction DodgeOwner201400724621";
            ColumnSet colset = new ColumnSet (new string[] {"ktc_projectid"});
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "ktc_external_data_source_match_id",
                            Operator = ConditionOperator.Equal,
                            Values = {primaryIdentifier }
                        }
                    }
                }
            };
            DataCollection<Entity> projects  = serviceProxy.RetrieveMultiple(query).Entities;
            project = (ktc_project) projects[0];
            Guid projectId = project.Id;

            //get existing contact Id
            Contact contact;
            query = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { "contactid" }),
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "fullname",
                            Operator = ConditionOperator.Equal,
                            Values = {"Pete Fortin"}
                        }
                    }
                }
            };
            DataCollection<Entity> contacts = serviceProxy.RetrieveMultiple(query).Entities;
            contact = (Contact)contacts[0];
            Guid contactId = contact.Id;

            //connect them 
            Dictionary<string, Guid> attribs = new Dictionary<string, Guid>();
            attribs.Add("Record1Id", projectId);
            attribs.Add("Record2Id", contactId);
            //attribs.Add ("Record1RoleId", ConnectionRoles.ConnectionRolesDictionary["1000.Owner (Private)"]);
            attribs.Add("Record2RoleId", ConnectionRoles.ConnectionRolesDictionary["1000.Construction Manager"]);
            connLdr.IsPrimary = false;
            connLdr.LoadAttributes = attribs;

            Guid connectionId;
            connLdr.CreateConnectionRecord(out connectionId);
            Assert.IsNotNull(connectionId);
            ColumnSet concolset = new ColumnSet(new string[] {"connectionid", "record1id", "record2id", "record1roleid", "record2roleid", "relatedconnectionid", "ismaster"});
            //check if reciprocal is created
            Connection primConnection = (Connection) serviceProxy.Retrieve(Connection.EntityLogicalName, connectionId, concolset);

            Console.WriteLine("connectionid: " + primConnection.Id + " record1id: " + primConnection.Record1Id.Id + " record1roleid: " + (primConnection.Record1RoleId != null ? primConnection.Record1RoleId.Id.ToString() : "") + " ismaster:" + primConnection.IsMaster.ToString());
            Console.WriteLine("record2id: " + primConnection.Record2Id.Id);
            if (primConnection.Record2RoleId != null)
            {
                Console.WriteLine(" record2roleid: " + primConnection.Record2RoleId.Id );}
            if (primConnection.RelatedConnectionId != null)
                Console.WriteLine(" relatedconnectionid: " + primConnection.RelatedConnectionId.Id);

            if (primConnection.RelatedConnectionId != null)
            {
                Connection secndConnection = (Connection)serviceProxy.Retrieve(Connection.EntityLogicalName, primConnection.RelatedConnectionId.Id, concolset); 
                if (secndConnection == null)
                {
                    Console.WriteLine("no secondary connection");
                }
                else
                {
                    Console.WriteLine("connectionid: " + secndConnection.Id + " record1id: " + secndConnection.Record1Id.Id +  " ismaster:" + secndConnection.IsMaster.ToString());
                    if (secndConnection.Record1RoleId != null)
                        Console.WriteLine(" record1roleid: " + secndConnection.Record1RoleId.Id);
                    Console.WriteLine("record2id: " + secndConnection.Record2Id.Id + " record2roleid: " + (secndConnection.Record2RoleId != null ? secndConnection.Record2RoleId.Id.ToString() : "") + " relatedconnectionid: " + secndConnection.RelatedConnectionId.Id);
                }


            }
        }

        [TestMethod]
        public void TestFindConnections()
        {
            ConnectionLoader connLdr = new ConnectionLoader(serviceProxy, ua);
            ConnectionRoles.RetrieveConnectionRolesOfInterest(new List<string>(new string[] { "Stakeholder", "Project Bidder" }), serviceProxy);
            //get existing project Id
            ktc_project project;
            string primaryIdentifier = "McGraw-Hill Construction DodgeOwner201400724621";
            ColumnSet colset = new ColumnSet(new string[] { "ktc_projectid" });
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "ktc_external_data_source_match_id",
                            Operator = ConditionOperator.Equal,
                            Values = {primaryIdentifier }
                        }
                    }
                }
            };
            DataCollection<Entity> projects = serviceProxy.RetrieveMultiple(query).Entities;
            project = (ktc_project)projects[0];
            Guid projectId = project.Id;

            //find all connections for which Record1Id represent this project
            colset = new ColumnSet(new string[] {"connectionid", "record1id", "record1objecttypecode", "record1roleid", "record2id", "record2objecttypecode", "record2roleid", "relatedconnectionid", "ismaster"});
            query = new QueryExpression
            {
                EntityName = Connection.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "record1id",
                            Operator = ConditionOperator.Equal,
                            Values = {projectId}
                        }
                    }
                }
            };
            DataCollection<Entity> connections = serviceProxy.RetrieveMultiple(query).Entities;
            Connection conn;
            foreach (Entity connEntity in connections)
            {
                conn = (Connection)connEntity;
                Assert.AreEqual(ktc_project.EntityTypeCode, (int) conn.Record1ObjectTypeCode.Value);
                Console.WriteLine("record1objecttypecode: " + conn.Record1ObjectTypeCode.Value.ToString() + " record2objecttyecode: " + conn.Record2ObjectTypeCode.Value.ToString());
                Console.WriteLine("connectionid: " + conn.Id + " record1id: " + conn.Record1Id.Id + " ismaster:" + conn.IsMaster.ToString());
                if (conn.Record1RoleId != null)
                        Console.WriteLine(" record1roleid: " + conn.Record1RoleId.Id);
                Console.WriteLine("record2id: " + conn.Record2Id.Id + " record2roleid: " + (conn.Record2RoleId != null ? conn.Record2RoleId.Id.ToString() : "") + " relatedconnectionid: " + conn.RelatedConnectionId.Id);
            }
        }

        [TestMethod]
        public void TestFindConnection()
        {
            ConnectionLoader connLdr = new ConnectionLoader(serviceProxy, ua);
            ConnectionRoles.RetrieveConnectionRolesOfInterest(new List<string>(new string[] { "Stakeholder", "Project Bidder" }), serviceProxy);
            //get existing project Id
            ktc_project project;
            string primaryIdentifier = "McGraw-Hill Construction DodgeOwner201400724621";
            ColumnSet colset = new ColumnSet(new string[] { "ktc_projectid" });
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "ktc_external_data_source_match_id",
                            Operator = ConditionOperator.Equal,
                            Values = {primaryIdentifier }
                        }
                    }
                }
            };
            DataCollection<Entity> projects = serviceProxy.RetrieveMultiple(query).Entities;
            project = (ktc_project)projects[0];
            Guid projectId = project.Id;

            //get existing contact Id
            Contact contact;
            query = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { "contactid" }),
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "fullname",
                            Operator = ConditionOperator.Equal,
                            Values = {"Pete Fortin"}
                        }
                    }
                }
            };
            DataCollection<Entity> contacts = serviceProxy.RetrieveMultiple(query).Entities;
            contact = (Contact)contacts[0];
            Guid contactId = contact.Id;

            Guid roleId = ConnectionRoles.ConnectionRolesDictionary["1000.Construction Manager"];

            //find the connection using FindConnection method of ConnectionLoader
            Connection conn = connLdr.FindConnection(projectId, contactId, roleId);

            Assert.IsNotNull(conn);
            Console.WriteLine("record1objecttypecode: " + conn.Record1ObjectTypeCode.Value.ToString() + " record2objecttyecode: " + conn.Record2ObjectTypeCode.Value.ToString());
            Console.WriteLine("connectionid: " + conn.Id + " record1id: " + conn.Record1Id.Id + " ismaster:" + conn.IsMaster.ToString());
            if (conn.Record1RoleId != null)
                Console.WriteLine(" record1roleid: " + conn.Record1RoleId.Id);
            Console.WriteLine("record2id: " + conn.Record2Id.Id + " record2roleid: " + (conn.Record2RoleId != null ? conn.Record2RoleId.Id.ToString() : "") + " relatedconnectionid: " + conn.RelatedConnectionId.Id);
         
        }
    }
}
