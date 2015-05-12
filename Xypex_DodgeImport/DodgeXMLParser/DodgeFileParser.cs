using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using DodgeImportLoader;
using Microsoft.Xrm.Sdk.Client;

namespace DodgeXMLParser
{
    public class DodgeFileParser
    {
        public class ParserConfiguration
        {
            public string locationType;
            public string localFileLocation;
            public string FTPServer;
            //and so one
        }

        private static class DodgeFileParserConfig
        {
            public static ParserConfiguration parserConfig;

            public static readonly string ConfigFileLocation = Path.Combine(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XypexCrm"),
                "DodgeFilesParsing.xml");

            public static string GetFileLocation()
            {
                if (parserConfig == null)
                {
                    ReadConfiguration();
                }
                //return local file location for now
                return  parserConfig.localFileLocation;
            }

            public static bool IsLocal()
            {
                if (parserConfig == null)
                {
                    ReadConfiguration();
                }
                return (parserConfig.locationType == "local");
            }

            private static bool ReadConfiguration()
            {
                bool doesConfigExist = false;
                if (File.Exists(DodgeFileParserConfig.ConfigFileLocation))
                {
                    XElement configFromFile = 
                        XElement.Load(DodgeFileParserConfig.ConfigFileLocation);

                    ParserConfiguration newConfig = new ParserConfiguration();
                    var locationType = configFromFile.Element("LocationType");
                    if (locationType != null)
                        if (!String.IsNullOrEmpty(locationType.Value))
                            newConfig.locationType = locationType.Value;
                    switch (newConfig.locationType)
                    {
                        case "local":

                            XElement localStgs = configFromFile.Element("LocalSettings");
                            if (localStgs != null && localStgs.Element("Path") != null)
                                if (!String.IsNullOrEmpty(localStgs.Element("Path").Value))
                                    newConfig.localFileLocation = localStgs.Element("Path").Value;
                            break;
                        case "FTP":
                            XElement ftpStgs = configFromFile.Element("FTPSettings");
                            if (ftpStgs != null && ftpStgs.Element("RemoteServer") != null)
                                if (!String.IsNullOrEmpty(ftpStgs.Element("RemoteServer").Value))
                                    newConfig.FTPServer = ftpStgs.Element("RemoteServer").Value;
                            break;
                        default:
                            break;
                    }
                    parserConfig = newConfig;
                    doesConfigExist = true;
                }
                 return doesConfigExist;
            }

        }

        /// <summary>
        /// Returns a list of files ready to be processed found at the location indicated in the config file
        /// </summary>
        /// <returns></returns>
        public List<string> CheckFileLocation()
        {
            List<string> FilePathNames = new List<string>();
            if (DodgeFileParserConfig.IsLocal())
            { 
                //loop through the directory at the file location
                var fileNames = Directory.EnumerateFiles(DodgeFileParserConfig.GetFileLocation());
                foreach (string fileName in fileNames)
                    FilePathNames.Add(fileName);
            }
            return FilePathNames;
        }

        public void ParseFile(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            DodgeReportNodeParser rptParser = null;
            //intialize shared objects
            Loader reportloader = new Loader();
            XElement map = DodgeToXypexMap.GetMapInstance();
            Lookups.LoadLookupsDictionary(reportloader.LoaderServiceProxy);

            try
            {
                using (XmlReader dodgeXML = XmlReader.Create(fileStream, settings))
                {
                    ParsePreamble(dodgeXML);

                    do
                    {
                        switch (dodgeXML.NodeType)
                        {
                            case XmlNodeType.Element :
                                switch (dodgeXML.Name)
                                {
                                    case "report":
                                        ParseReportStart(dodgeXML);
                                        break;
                                    case "dodge-report":
                                        XmlReader XdodgeReport = ParseDodgeReport(dodgeXML);
                                        if (rptParser == null)
                                        {
                                            rptParser = new DodgeReportNodeParser(XdodgeReport);
                                        }
                                        else
                                        {
                                            rptParser.ReadNode(XdodgeReport);
                                        }
                                        //the guts of the processing
                                        rptParser.ParseAndProcess(reportloader);
                                        break;
                                    default:
                                        ParseReportExtra(dodgeXML);
                                        break;
                                }
                                break;
                            case XmlNodeType.EndElement:
                                switch (dodgeXML.Name)
                                {
                                    case "dodge-report":
                                        break;
                                    default:
                                        break;
                                }
                                break;
                        }
                    } while (dodgeXML.Read());

                }
            }
            catch (XmlException xe)
            {
                XypexLogger.HandleException(xe);
                //throw xe;
            }
            catch (Exception e)
            {
                XypexLogger.HandleException(e);
                //throw e;
            }
            finally
            {
                fileStream.Close();
            }
        }

        private void ParsePreamble(XmlReader reader)
        {
            reader.ReadStartElement("reports");
            return;
        }

        private void ParseReportStart(XmlReader dXML)
        {
            //dXML.ReadStartElement("report");
            return;
        }

        //skip over inner nodes in report node that are not of interest
        private void ParseReportExtra(XmlReader dXML)
        {
            //dXML.ReadToFollowing("addenda_ind_ol");
            dXML.Skip();
            return;
        }

        private XmlReader ParseDodgeReport(XmlReader dXML)
        {
            XmlReader retval = dXML.ReadSubtree();
            return retval;
        }
    }
}
