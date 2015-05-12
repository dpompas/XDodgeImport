using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DodgeImportLoader;

namespace DodgeXMLParser
{     
    /// <summary>
    /// This class has methods that process a read dodge-report node by transforming the input xml to the loading XML
    /// and calling the appropriate load processing logic
    /// </summary>
    public class DodgeReportNodeParser
    {
        XElement _thenode;

        public DodgeReportNodeParser(XmlReader nodeReader)
        {
            ReadNode(nodeReader);
        }

        public DodgeReportNodeParser(XElement element)
        {
            _thenode = new XElement(element);
        }

        public void ReadNode(XmlReader reader)
        {
           _thenode = XElement.Load(reader);
        }

        public void ParseAndProcess(Loader loader)
        {
            List<XElement> mapFromNodes = FilterNodesOfInterest();
            List<XElement> mapToNodes = TransformNodes(mapFromNodes);

            //loader takes care of the ER order in which to load
            loader.LoadDodgeReportEntities(mapToNodes);
            return;
        }

        /// <summary>
        /// Select only nodes for which TitleCode has no attribute, separated by mapping sections
        /// </summary>
        public List<XElement> FilterNodesOfInterest()
        {
            List<XElement> inNodes = new List<XElement>();
            //the summary element contains information about the project
            XElement xSummary = GetXSummary();
            inNodes.Add(xSummary);

            //the data elements contain additional information about the project
            //and information corresponding to other source elements/CRM entities
            List<XElement> xData = GetXData(xSummary);
            inNodes.InsertRange(1, xData);

            return inNodes;
        }

        #region parseinputDatanodes

        private XElement GetXSummary()
        {
            return new XElement("project", _thenode.Element("summary"));
        }

        private List<XElement> GetXData(XElement xsummary)
        {   List<XElement> dataElements = new List<XElement>();
            XElement allData = _thenode.Element("data");

            //need to figure out the nodes of interest based on mappings
            XElement innerDataElement;
            Dictionary<string, string> entElementNames = DodgeToXypexMap.GetSrcEntityElementNamesFromMap();
            foreach (KeyValuePair<string, string> pathAndElemName in entElementNames)
            {
                //get only that mapped node for which titlecode has no attributes
                innerDataElement = GetSourceEntityElement(allData, pathAndElemName/*, dataElements*/);
                if (innerDataElement == null)
                    continue;

                //apply any special filtering based on node contents
                innerDataElement = FilterSourceEntityElement(innerDataElement);
                if (innerDataElement == null)
                    continue;

                if (DodgeToXypexMap.GetXElemNameFromSrcEntName(innerDataElement.Name.LocalName) == "project")
                {
                    //add to existing project element
                    xsummary.Add(innerDataElement.FirstNode);
                }
                else
                {
                    dataElements.Add(innerDataElement);
                }
            }
            return dataElements;
        }

        /// <summary>
        /// Applies node and contents-specific filtering to input nodes, by removing those nodes that should not be processed
        /// </summary>
        /// <param name="inputElement">an XElement from the input file, could be at a higher level than an "entity" level</param>
        /// <returns>an XElement that may have some of the original contents filtered out or null</returns>
        public XElement FilterSourceEntityElement(XElement inputElement)
        {
            switch (inputElement.Name.LocalName)
            {
                case "projectContact":
                    IEnumerable<XElement> filteredElements = 
                            from contact in inputElement.Elements("project-contact")
                            where contact.Element("contact-role") != null && 
                                Lookups.DoesLookupKeyExist("ktc_connection_roles", contact.Element("contact-role").Value)
                            select contact;
                    if (filteredElements != null )
                        return new XElement(inputElement.Name.LocalName, filteredElements);
                    else 
                        //we don't want to process this 
                        return null;
                default:
                    return inputElement;
            }


        }

        /// <summary>
        /// Build a new XElement (source element) from the subelement of the original element
        /// having the indicated path, name it with the correspondent name in the pair value and 
        /// add the new element to a list of elements
        /// </summary>
        /// <param name="XData">the original XElement</param>
        /// <param name="pathAndElementName">KeyValuePair with the key being the path(name) of the subelement of the original XElement
        /// and the value being the name that will be given to the output element</param>
        /// <returns>An XElement that will be used as source for mapping</returns>
        private XElement GetSourceEntityElement(XElement XData, KeyValuePair<string, string> pathAndElementName)
        {
            XElement sourceElement = null;
            string titleCodeSequenceDescendantPath = String.Empty;
            string path = pathAndElementName.Key;
            string newElemName = pathAndElementName.Value;

            if (DodgeToXypexMap.TitleCodeSequenceDescendants.ContainsKey(path))
                titleCodeSequenceDescendantPath = DodgeToXypexMap.TitleCodeSequenceDescendants[path];
           

            if (XData.Element(path) != null)
            {
                XElement pathRoot;
                if (titleCodeSequenceDescendantPath != string.Empty)
                {
                    pathRoot = new XElement(XData.Element(path));
                    sourceElement = new XElement(newElemName,
                            from titlecode in pathRoot.Elements("title-code")
                            where titlecode.HasAttributes == false
                            select titlecode.Elements(titleCodeSequenceDescendantPath));
                }
                else
                {
                    sourceElement = new XElement(newElemName, new XElement(XData.Element(path)));
                }
            }
            return sourceElement;
        }

        #endregion

        public List<XElement>  TransformNodes(List<XElement> inNodes)
        {
            List<XElement> outNodes = new List<XElement>();
            DodgeToXypexMapper mapper = new DodgeToXypexMapper();
            return mapper.MapAndTransformNodes(inNodes);
        }

    }
}
