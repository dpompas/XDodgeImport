using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DodgeXMLParser;
using System.Xml;
using System.Xml.Linq;
using DodgeImportLoader;
using DodgeImportLoader.XypexSDKTypes;
using Microsoft.Xrm.Sdk.Client;

namespace DodgeXMLParserImportTests
{
    [TestClass]
    public class XMLParserTests
    {
        public static List<XElement> inNodes;
        public static XElement dodgeReport;
        public static string sampleFilePath = "C:\\Users\\Diana\\Documents\\Xypex\\Dodge XML\\OneDodgeReport_Wet_Well_01232015.xml";
        public static string outputFilePath = "C:\\Users\\Diana\\Documents\\Xypex\\Dodge XML\\\\Output\\OneParsedReport.xml";
        public static string preparsedFilePath = "C:\\Users\\Diana\\Documents\\Xypex\\Dodge XML\\\\Output\\OnePreParsedReport.xml";

        [ClassInitialize]
        public static void ReadDodgeXML(TestContext DodgeXMLTests)
        {
            FileStream fileStream = new FileStream(sampleFilePath, FileMode.Open);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            try
            {
                using (XmlReader dodgeXML = XmlReader.Create(fileStream, settings))
                {
                    dodgeReport = XElement.Load(dodgeXML);
                }
            }
            catch (XmlException xe)
            {
                //ExceptionHandler.HandleException(xe);
                throw xe;
            }
            catch (Exception e)
            {
                //ExceptionHandler.HandleException(e);
                throw e;
            }
            finally
            {
                fileStream.Close();
            }
        }

        

        [TestMethod]
        [TestCategory("XMLload")]
        public void TestLoadMap()
        {
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            DodgeToXypexMap.GetMapInstance();
            Assert.IsTrue(DodgeToXypexMap._theMap.Value.Length > 0);
            Assert.IsNotNull(DodgeToXypexMap._theMap.Element("EntityMaps"));
            foreach(XNode node in  DodgeToXypexMap._theMap.Nodes())
            {
                Console.WriteLine(node.GetType().ToString(), ": ", node.ToString());
            }
        }

        [TestMethod]
        [TestCategory("XMLLoad")]
        public void TestGetSourceElementsMapForTarget()
        {
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            DodgeToXypexMap.GetMapInstance();
            IEnumerable<XElement> sourceElems = DodgeToXypexMap.GetSourceElementsMapsForTargetEntity("ktc_project");
            Assert.IsNotNull(sourceElems);
            foreach (XElement e in sourceElems)
            {
                Console.WriteLine(e);
            }
        }

        [TestMethod]
        public void TestParseDodgeReport()
        {
            DodgeReportNodeParser rpt = new DodgeReportNodeParser(dodgeReport);
            inNodes = rpt.FilterNodesOfInterest();
            foreach (XElement node in inNodes)
            {
                if (node != null)
                {
                    Console.WriteLine(node.Name);
                }
            }
        }

        [TestMethod]
        public void TestGetSourceNodesMappedToTarget()
        {
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            DodgeToXypexMap.GetMapInstance();
            IEnumerable<XElement> sourceElems = DodgeToXypexMap.GetSourceElementsMapsForTargetEntity("account");

            DodgeReportNodeParser rpt = new DodgeReportNodeParser(dodgeReport);
            inNodes = rpt.FilterNodesOfInterest();
            Console.WriteLine(inNodes.Count);
            foreach (XElement node in inNodes)
            {
                Console.WriteLine(node.Name.ToString());
            }
           
            //loop through each source element and add to Account Xelement 
            foreach (XElement srcMap in sourceElems)
            {
                string srcName = srcMap.Attribute("SourceEntityName").Value;
                Console.WriteLine(srcMap.Attribute("SourceEntityName").Value);
                //find the corresponding in node to be mapped
                 XElement srcNode = inNodes.Find(x => srcName.Contains(x.Name.ToString()));
                if (srcNode != null)
                {
                    Console.WriteLine(srcNode);
                }
                else
                {
                    Console.WriteLine("Not found");
                }

            }
        }

        [TestMethod]
        [TestCategory("Parsing")]
        public void TestGetFunctionMaps()
        {
            Dictionary<string, string> inMap = new Dictionary<string, string>();
            inMap.Add("=Concatenate(%1, telephone1)", "area-code");
            inMap.Add("=Concatenate(%2, telephone1)", "phone-nbr"); 
            inMap.Add("=Concatenate(%3, telephone1)", "phone-ext"); 
            inMap.Add("=Concatenate(%1, ktc_External_Data_Source_Match_ID)", "{PUBLISHER}"); 
            inMap.Add("=Concatenate(%2, ktc_External_Data_Source_Match_ID)", "dcis-factor-code");
            inMap.Add("=Lookup(ktc_project_types,%1,ktc_Project_Type_Id)", "proj-type");
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            //DodgeToXypexMap.GetMapInstance();

            Dictionary<string, Dictionary<int, string>> outMaps =
                mapper.GetFunctionMaps(inMap);

            foreach (string attrKey in outMaps.Keys)
            {
                Console.WriteLine("Out attrib:" + attrKey);
                foreach(int parKey in outMaps[attrKey].Keys)
                {
                    Console.WriteLine("param " + parKey.ToString() + ": " + (outMaps[attrKey])[parKey]);
                }
            }

            Assert.IsTrue(outMaps.Count == 3);

        }

        [TestMethod]
        [TestCategory("Parsing")]
        public void TestTransformComplexMap()
        {
            XElement inElem = XElement.Parse(
                    @"<contact-information>
						<s-contact-role code=""10001"">Owner</s-contact-role>
						<s-contact-category>Owner</s-contact-category>
						<s-contact-group>Owner</s-contact-group>
						<firm-name>City of Scottsdale</firm-name>
						<contact-name>Alison Tymkiw</contact-name>
						<contact-title>CIP, PE, LEED AP</contact-title>
						<c-addr-line>
							<c-addr-line1>9191 E San Salvador Dr</c-addr-line1>
						</c-addr-line>
						<c-county-name>MARICOPA</c-county-name>
						<c-fips-county-id>AZ013</c-fips-county-id>
						<c-city-name>Scottsdale</c-city-name>
						<c-state-id>AZ</c-state-id>
						<c-zip-code>852585533</c-zip-code>
						<c-zip-code5>85258</c-zip-code5>
						<c-country-id>USA</c-country-id>
						<area-code>480</area-code>
						<phone-nbr>3123481</phone-nbr>
						<fax-area-code>480</fax-area-code>
						<fax-nbr>3125701</fax-nbr>
						<email-id>atymkiw@ScottsdaleAZ.gov</email-id>
						<www-url>www.scottsdaleaz.gov</www-url>
						<dcis-factor-code>SCOAZ0006</dcis-factor-code>
						<dcis-factor-cntct-code>AAK</dcis-factor-cntct-code>
						<ckms-site-id>CKS000000822665</ckms-site-id>
						<ckms-process-ind>Y</ckms-process-ind>
						<company-rpt-parent-cnt-sys-id>205346</company-rpt-parent-cnt-sys-id>
						<cn-company-site-url>http://network2.construction.com/ExternalClick.aspx?source=NETWORK_EXPRESS&amp;page=Company.aspx&amp;companyId=CKS000000822665</cn-company-site-url>
						<ckms-contact-id>CKC000001768904</ckms-contact-id>
						<contact-rpt-cnt-sys-id>205372</contact-rpt-cnt-sys-id>
						<ckms-contact-process-ind>Y</ckms-contact-process-ind>
					</contact-information>");
            
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            DodgeToXypexMap.GetMapInstance();
            IEnumerable<XElement> sourceElemMaps = DodgeToXypexMap.GetSourceElementsMapsForTargetEntity("account");
            //get complex maps for contact only
            Dictionary<string, string> complexAttrMaps = null;
            XElement mapElem = null;
            foreach (XElement srcMap in sourceElemMaps)
            {
                if (srcMap.Attribute("SourceEntityName").Value.Contains("projectContact"))
                {
                    complexAttrMaps = DodgeToXypexMap.GetComplexAttributesMap(srcMap);
                    mapElem = srcMap;
                    break;
                }
            }

            XElement outElem = null;
            outElem = mapper.TransformComplexMap(inElem, mapElem, complexAttrMaps, outElem);

            Console.WriteLine(outElem.ToString());

        }

        [TestMethod]
        [TestCategory("Parsing")]
        public void TestTransformComplexMapContact()
        {
            XElement inElem = XElement.Parse(
                    @"<contact-information>
						<s-contact-role code=""10001"">Owner</s-contact-role>
						<s-contact-category>Owner</s-contact-category>
						<s-contact-group>Owner</s-contact-group>
						<firm-name>City of Scottsdale</firm-name>
						<contact-name>Alison Tymkiw</contact-name>
						<contact-title>CIP, PE, LEED AP</contact-title>
						<c-addr-line>
							<c-addr-line1>9191 E San Salvador Dr</c-addr-line1>
						</c-addr-line>
						<c-county-name>MARICOPA</c-county-name>
						<c-fips-county-id>AZ013</c-fips-county-id>
						<c-city-name>Scottsdale</c-city-name>
						<c-state-id>AZ</c-state-id>
						<c-zip-code>852585533</c-zip-code>
						<c-zip-code5>85258</c-zip-code5>
						<c-country-id>USA</c-country-id>
						<area-code>480</area-code>
						<phone-nbr>3123481</phone-nbr>
						<fax-area-code>480</fax-area-code>
						<fax-nbr>3125701</fax-nbr>
						<email-id>atymkiw@ScottsdaleAZ.gov</email-id>
						<www-url>www.scottsdaleaz.gov</www-url>
						<dcis-factor-code>SCOAZ0006</dcis-factor-code>
						<dcis-factor-cntct-code>AAK</dcis-factor-cntct-code>
						<ckms-site-id>CKS000000822665</ckms-site-id>
						<ckms-process-ind>Y</ckms-process-ind>
						<company-rpt-parent-cnt-sys-id>205346</company-rpt-parent-cnt-sys-id>
						<cn-company-site-url>http://network2.construction.com/ExternalClick.aspx?source=NETWORK_EXPRESS&amp;page=Company.aspx&amp;companyId=CKS000000822665</cn-company-site-url>
						<ckms-contact-id>CKC000001768904</ckms-contact-id>
						<contact-rpt-cnt-sys-id>205372</contact-rpt-cnt-sys-id>
						<ckms-contact-process-ind>Y</ckms-contact-process-ind>
					</contact-information>");

            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            DodgeToXypexMap.GetMapInstance();
            IEnumerable<XElement> sourceElemMaps = DodgeToXypexMap.GetSourceElementsMapsForTargetEntity("contact");
            //get complex maps for contact only
            Dictionary<string, string> complexAttrMaps = null;
            XElement mapElem = null;
            foreach (XElement srcMap in sourceElemMaps)
            {
                if (srcMap.Attribute("SourceEntityName").Value.Contains("projectContact"))
                {
                    complexAttrMaps = DodgeToXypexMap.GetComplexAttributesMap(srcMap);
                    mapElem = srcMap;
                    break;
                }
            }

            XElement outElem = null;
            outElem = mapper.TransformComplexMap(inElem, mapElem, complexAttrMaps, outElem);

            Console.WriteLine(outElem.ToString());

        }

        [TestMethod]
        [TestCategory("FunctionResolver")]
        public void TestSplit()
        {
            string fullname = "Diana L Pompas";
            Dictionary<int, string> paramValues0 = new Dictionary<int,string>();
            paramValues0.Add(0, fullname);
            Dictionary<int, string> paramValues1 = new Dictionary<int,string>();
            paramValues1.Add(1, fullname);

            DodgeToXypexMapper mapper = new DodgeToXypexMapper();

            string firstname = mapper.ResolveFunction("Split", paramValues0);
            string lastname = mapper.ResolveFunction("Split", paramValues1);

            Assert.AreEqual("Diana L", firstname);
            Assert.AreEqual("Pompas", lastname);
        }

        [TestMethod]
        public void TestFilterNodesOfInterest()
        {
            DodgeToXypexMap.GetMapInstance();
            Loader loader = new Loader();
            Lookups.LoadLookupsDictionary(loader.LoaderServiceProxy);
            DodgeReportNodeParser drnp = new DodgeReportNodeParser(dodgeReport);

            List<XElement> inMapNodes = drnp.FilterNodesOfInterest();

            foreach(XElement xx in inMapNodes)
            {
                Console.WriteLine(xx);
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Auto;
            //settings.IgnoreWhitespace = true;
            //settings.IgnoreComments = true;

            XmlWriter xWriter = XmlWriter.Create(preparsedFilePath, settings);
            foreach (XElement outel in inMapNodes)
            {
                xWriter.WriteStartElement(outel.Name.LocalName);
                foreach (XElement innerel in outel.Elements())
                {
                    xWriter.WriteElementString(innerel.Name.LocalName, innerel.Value);
                }
                xWriter.WriteEndElement();

            }
            xWriter.Close();
        }

        [TestMethod]
        public void TestParseProject()
        {
            DodgeToXypexMap.GetMapInstance();
            DodgeReportNodeParser drnp = new DodgeReportNodeParser(dodgeReport);
            Loader ldr = new Loader();
            Lookups.LoadLookupsDictionary(ldr.LoaderServiceProxy);

            List<XElement> inMapNodes = drnp.FilterNodesOfInterest();
            List<XElement> outMapNodes = drnp.TransformNodes(inMapNodes);

            foreach (XElement oe in outMapNodes)
            {
                Console.WriteLine(oe);
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            //settings.IgnoreWhitespace = true;
            //settings.IgnoreComments = true;

            XmlWriter xWriter = XmlWriter.Create(outputFilePath, settings);
            foreach (XElement outel in outMapNodes)
            {
                xWriter.WriteStartElement(outel.Name.LocalName);
                foreach (XElement innerel in outel.Elements())
                {
                    xWriter.WriteElementString(innerel.Name.LocalName, innerel.Value);
                }
                xWriter.WriteEndElement();
            }
            xWriter.Close();
        }

        [TestMethod]
        public void TestGetSrcEntitiesFroMap()
        {
            DodgeToXypexMap.GetMapInstance();
            Dictionary<string, string> srcEnts = DodgeToXypexMap.GetSrcEntityElementNamesFromMap();
            Assert.IsTrue(srcEnts.Count > 0);
            foreach (string key in srcEnts.Keys)
            {
                Console.WriteLine("SourceEntityElementName" + ": " + key + ", SourceEntityName: " + srcEnts[key]);
            }

        }

        [TestMethod]
        public void TestGetEntitiesToLoadTo()
        {
            DodgeToXypexMap.GetMapInstance();
            IEnumerable<string> ents = DodgeToXypexMap.GetEntitiesToLoadTo();

            foreach (string s in ents)
            {
                Console.WriteLine(s);
            }
        }

        [TestMethod]
        [TestCategory("Lookups")]
        public void TestFindLookupValue()
        {
            Loader loader = new Loader();
            Lookups.LoadLookupsDictionary(loader.LoaderServiceProxy);

            object StageId = Lookups.FindLookupValue("ktc_project_stages", "Pre-Design");
            Assert.IsNotInstanceOfType(StageId, typeof(LookupObjectNotFound), "object not found");
            Console.WriteLine("Found stage id: " + StageId.ToString());

            StageId = Lookups.FindLookupValue("ktc_project_stages", "Pre-Qualification");
            Assert.IsNotInstanceOfType(StageId, typeof(LookupObjectNotFound), "object not found");
            Console.WriteLine("Found stage id: " + StageId.ToString());

            object TypeId = Lookups.FindLookupValue("ktc_project_types", "Bridge");
            Assert.IsNotInstanceOfType(TypeId, typeof(LookupObjectNotFound), "object not found");
            Console.WriteLine("Found type id: " + TypeId.ToString());

            object connRoleId = Lookups.FindLookupValue("ktc_connection_roles", "Structural Engineer");
            Assert.IsNotInstanceOfType(connRoleId, typeof(LookupObjectNotFound), "object not found");
            Console.WriteLine("Found conn role id: " + connRoleId.ToString());

            connRoleId = Lookups.FindLookupValue("ktc_connection_roles", "Mechanical Engineer");
            //Assert.IsNotInstanceOfType(connRoleId, typeof(LookupObjectNotFound), "object not found");
            if (connRoleId.GetType() == typeof(LookupObjectNotFound))
                if (((LookupObjectNotFound)connRoleId).Code == LookupObjectNotFound.LookupNotFound.KeyNotFoundInDictionary)
                    Console.WriteLine("conn role not found for: " + "Mechanical Engineer");
                else
                    Console.WriteLine("dictionary not found for: " + "ktc_connection_roles");
            else
                Console.WriteLine("Found conn role id: " + connRoleId.ToString());

        }
    }
}
