using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class ProjectTests
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
        public void TestProjectAdd()
        {
            XElement inElem = XElement.Parse(
                    @"<project>
						<ktc_External_Data_Source_ID>Owner201400724624</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>otherEDS</ktc_External_Data_Source_Name>
                        <ktc_Project_Name>Test Project D</ktc_Project_Name>
                        <ktc_Project_address1_line1>4400 ONTARIO MILLS PKWY</ktc_Project_address1_line1>
                        <ktc_Project_Estimated_Valuation_High>2999999</ktc_Project_Estimated_Valuation_High>
                        <ktc_Project_Owner_Class>Private</ktc_Project_Owner_Class>
                     </project>");

            ProjectLoader prjLoader = new ProjectLoader(serviceProxy, ua, inElem);
            try
            {
                Guid projectId;
                prjLoader.CreateRecord(out projectId);
                if (projectId == Guid.Empty)
                    Assert.Fail("no account Id created.");
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestProjPrimaryMatch()
        {
            string ktcid = "201500412734";
            ProjectLoader projLdr = new ProjectLoader(serviceProxy, ua);

            ktc_project CRMproj = projLdr.PrimaryMatch(ktcid);

            Assert.IsNotNull(CRMproj);

            Guid projId = CRMproj.Id;
            Console.WriteLine("Project Id: " + projId + ", name: " + CRMproj.ktc_project_name );
            
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public void TestProjectUpdate()
        {
            XElement inElem = XElement.Parse(
//                    @"<project>
//						<ktc_External_Data_Source_ID>Owner201400724624</ktc_External_Data_Source_ID>
//                        <ktc_External_Data_Source_Name></ktc_External_Data_Source_Name>
//                        <ktc_Project_Name>iFLY Indoor Free Fall Simulator (Ontario CA)</ktc_Project_Name>
//                        <ktc_Project_address1_line1>4400 ONTARIO MILLS PKWY</ktc_Project_address1_line1>
//                        <ktc_Project_Estimated_Valuation_High>4999999</ktc_Project_Estimated_Valuation_High>
//                        <ktc_Project_Owner_Class>Private</ktc_Project_Owner_Class>
//                     </project>");
                    @"<project>
						<ktc_External_Data_Source_ID>201400724621</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>McGraw-Hill Construction Dodge</ktc_External_Data_Source_Name>
                     </project>");
            string ktc_matchid = "McGraw-Hill Construction Dodge201400724621";

            ProjectLoader prjLoader = new ProjectLoader(serviceProxy, ua, inElem);
            try
            {
                ktc_project CRMproj = prjLoader.PrimaryMatch(ktc_matchid);

                Assert.IsNotNull(CRMproj, "did not find project to update");

                Guid projId = CRMproj.Id;
                Console.WriteLine("Project Id: " + projId + ", name: " + CRMproj.ktc_project_name);

                prjLoader.UpdateRecord();

                string new_ktc_matchId = "McGraw-Hill Construction Dodge201400724621";
                
                CRMproj = prjLoader.PrimaryMatch(new_ktc_matchId);
                Assert.IsNotNull(CRMproj, "did not find project after update");

                Guid newprojId = CRMproj.Id;
                Console.WriteLine("New Project Id: " + newprojId + ", name: " + CRMproj.ktc_project_name);
            
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        [TestCategory("CRMUpdate")]
        public static int DeleteProjectByEDSMId(string ext_d_src_match_id)
        {
            int n = 0;
            ColumnSet colset = new ColumnSet(new string[] { "ktc_projectid", "ktc_project_name" });
            QueryExpression query = 
                new QueryExpression 
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
                                Values = {ext_d_src_match_id}
                            }
                        }
                    }
                };
            
            DataCollection<Entity> projs = serviceProxy.RetrieveMultiple(query).Entities;
            //Assert.IsTrue(projs.Count  > 0);
            Console.WriteLine("number of records found: " + projs.Count);
            n = projs.Count;
            ktc_project project;
 
            for (int i = 0; i < n; i++)
            {
               project  = (ktc_project) projs[i];
               serviceProxy.Delete(ktc_project.EntityLogicalName, project.Id);
            }
            return n;
        }

        [TestMethod]
        public void TestGetSecMatchingAttributes()
        {
            XElement xe = XElement.Parse(
                    @"<project>
						<ktc_External_Data_Source_ID>Owner201400724624</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>otherEDS</ktc_External_Data_Source_Name>
                        <ktc_Project_address1_line1>6600 MEDICINE LAKE RD</ktc_Project_address1_line1>
	                    <ktc_Project_address1_city>CRYSTAL</ktc_Project_address1_city>
	                    <ktc_Project_address1_stateorprovince>MN</ktc_Project_address1_stateorprovince>
	                    <ktc_Project_address1_country>USA</ktc_Project_address1_country>
                        <ktc_Project_Estimated_Valuation_High>2999999</ktc_Project_Estimated_Valuation_High>
                     </project>");
            ProjectLoader prjldr = new ProjectLoader(serviceProxy, ua);
            Dictionary<string, string> attribs = prjldr.GetSecondaryMatchAttributes(xe);

            Assert.IsTrue(attribs.Count > 0);
            foreach (string k in attribs.Keys)
            {
                Console.WriteLine(k + " , " + attribs[k]);
            }
        }

        [TestMethod]
        public void TestProjectSecondaryMatch()
        {
            XElement xe = XElement.Parse(
                    @"<project>
						<ktc_External_Data_Source_ID>201400724624</ktc_External_Data_Source_ID>
                        <ktc_External_Data_Source_Name>otherEDS</ktc_External_Data_Source_Name>
                        <ktc_Project_address1_line1>6600 MEDICINE LAKE RD</ktc_Project_address1_line1>
	                    <ktc_Project_address1_city>CRYSTAL</ktc_Project_address1_city>
	                    <ktc_Project_address1_stateorprovince></ktc_Project_address1_stateorprovince>
	                    <ktc_Project_address1_country>USA</ktc_Project_address1_country>
                        <ktc_Project_Estimated_Valuation_High>2999999</ktc_Project_Estimated_Valuation_High>
                     </project>");
            ProjectLoader projLdr = new ProjectLoader(serviceProxy, ua);
            Dictionary<string, string> attribs = projLdr.GetSecondaryMatchAttributes(xe);


            ktc_project CRMproj = projLdr.SecondaryMatch(attribs);

            Assert.IsNotNull(CRMproj);

            Guid projId = CRMproj.Id;
            Console.WriteLine("Project Id: " + projId + ", name: " + CRMproj.ktc_project_name);

            CRMproj.ktc_Project_address1_stateorprovince = "MN";
            serviceProxy.Update(CRMproj);
            
        }

        [TestMethod]
        public void TestFindProjectAddressDetails()
        {
            //use criteria in PrimaryMatch but make sure address fields are returned
            string ktcid = "201500412734";
            ProjectLoader projLdr = new ProjectLoader(serviceProxy, ua);
            ktc_project CRMproj = null;
            ColumnSet cols = new ColumnSet(new string[] { "ktc_projectid", "ktc_project_name",
                                "ktc_project_address1_line1", "ktc_project_address1_city",
                "ktc_project_address1_stateorprovince", "ktc_project_address1_country"});
            
            //identify and return record for which ktc_External_Data_Source_Match_ID has the value provided in primaryIdentifier
            QueryExpression findByKtcMatchId = new QueryExpression 
            {
                EntityName = ktc_project.EntityLogicalName,
                ColumnSet = cols,
                Criteria = new FilterExpression 
                {
                    Conditions = 
                    {
                        new ConditionExpression 
                        {
                            AttributeName = "ktc_external_data_source_match_id",
                            Operator = ConditionOperator.Equal,
                            Values = {ktcid }
                        }
                    }
                }
            };

            try
            {
                DataCollection<Entity> projects = serviceProxy.RetrieveMultiple(findByKtcMatchId).Entities;
                if (projects != null && projects.Count > 0)
                {
                    CRMproj = (ktc_project)projects[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //Assert.IsNotNull(CRMproj);
            if (CRMproj != null)
            {
                Guid projId = CRMproj.Id;
                Console.WriteLine("Project Id: " + projId + ", name: " + CRMproj.ktc_project_name);
                Console.WriteLine("ktc_project_address1_line1 : " + CRMproj.ktc_Project_address1_line1);
                Console.WriteLine("ktc_project_address1_city : " + CRMproj.ktc_Project_address1_city);
                Console.WriteLine("ktc_project_address1_stateorprovince : " + CRMproj.ktc_Project_address1_stateorprovince);
                Console.WriteLine("ktc_project_address1_country : " + CRMproj.ktc_Project_address1_country);
            }

        }

        [ClassCleanup]
        public static void CleanupProjects()
        {
            string matchId = "otherEDSOwner201400724624";
            int nrdel = DeleteProjectByEDSMId(matchId);
            Console.WriteLine("Deleted: " + nrdel.ToString() + " records");
        }
    }
}
