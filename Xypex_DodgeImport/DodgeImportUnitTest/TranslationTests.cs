using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using DodgeImportLoader;
using XypexCRM;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using System.Xml;
using System.Xml.Linq;
using DodgeImportLoader.XypexSDKTypes;

namespace DodgeImportUnitTest
{
    [TestClass]
    public class TranslationTests
    {
        public static OrganizationServiceProxy serviceProxy;

        [ClassInitialize]
        public static void EstablishConnection(TestContext DodgeTests)
        {
            serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
            serviceProxy.EnableProxyTypes();

        }

        [TestMethod]
        public void TestRetrieveProjectTypes()
        {
            ProjectTypeTranslation ptrans = new ProjectTypeTranslation(serviceProxy);

            Dictionary<string, object /*Guid*/> projectTypesDict = ptrans.RetrieveIdDictionary();

            Assert.IsNotNull(projectTypesDict);
            Assert.IsTrue(projectTypesDict.Count > 0);

            Console.WriteLine("Project Types:");
            foreach (string key in projectTypesDict.Keys)
            {
                Console.WriteLine(key + "->" + projectTypesDict[key]);
            }
        }

        [TestMethod]
        public void TestRetrieveProjectStages()
        {
            ProjectStageTranslation ptrans = new ProjectStageTranslation(serviceProxy);

            Dictionary<string, object/*Guid*/> projectStagesDict = ptrans.RetrieveIdDictionary();

            Assert.IsNotNull(projectStagesDict);
            Assert.IsTrue(projectStagesDict.Count > 0);

            foreach (string key in projectStagesDict.Keys)
            {
                Console.WriteLine(key + "->" + projectStagesDict[key]);
            }
        }
    }
}
