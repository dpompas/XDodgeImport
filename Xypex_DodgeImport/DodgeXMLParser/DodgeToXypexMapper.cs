using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using DodgeImportLoader;

namespace DodgeXMLParser
{
    #region DodgeToXypexMap class
    public class DodgeToXypexMap
    {
        public static XElement _theMap;
        public static string _mapFilePath = "C:\\Users\\Diana\\Documents\\Xypex\\Dodge XML\\MapForDodgeReports.xml";
        
        private static Dictionary<string, string> _titleCodeSequenceDescendants;
        private static IEnumerable<string> _targetEntities;
        private static Dictionary<string, string> _srcEntityAndElementNames;

        private static void ReadMapXML()
        {
            //get the XML file location
            //load into _theMap
            FileStream fileStream = new FileStream(_mapFilePath, FileMode.Open);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            try
            {
                using (XmlReader mapXML = XmlReader.Create(fileStream, settings))
                {
                    _theMap = XElement.Load(mapXML);
                }
            }
            catch (XmlException xe)
            {
                XypexLogger.HandleException(xe);
                throw xe;
            }
            catch (Exception e)
            {
                XypexLogger.HandleException(e);
                throw e;
            }
            finally
            {
                fileStream.Close();
            }


        }

        public static XElement GetMapInstance()
        {
            if (_theMap == null)
                ReadMapXML();
            return _theMap;
        }

        /// <summary>
        /// This returns a helper for parsing input Dodge files
        /// indicating sequence elements below "<titlecode>" elements that
        /// need ot be parsed, without the need to process the Dodge XML schema.
        /// Refer to MHC_Dodge_News_Schema_Doc_2014.xsd.
        /// </summary>
        /// <remarks>If an element of interest does not have a titlecode subelement, then the value in the 
        /// dictionary will be empty
        /// </remarks>
        public static Dictionary<string, string> TitleCodeSequenceDescendants
        {
            get
            {
                if (_titleCodeSequenceDescendants == null)
                {
                    _titleCodeSequenceDescendants = new Dictionary<string, string>();
                    _titleCodeSequenceDescendants.Add("summary", "");
                    _titleCodeSequenceDescendants.Add("proj-title", "");
                    _titleCodeSequenceDescendants.Add("p-location", "");
                    _titleCodeSequenceDescendants.Add("project-status", "");
                    _titleCodeSequenceDescendants.Add("status", "");
                    _titleCodeSequenceDescendants.Add("project-valuation", "");
                    _titleCodeSequenceDescendants.Add("additional-details", "");
                    _titleCodeSequenceDescendants.Add("project-type", "");
                    _titleCodeSequenceDescendants.Add("project-stage", "");
                    _titleCodeSequenceDescendants.Add("structural-data", "");
                    _titleCodeSequenceDescendants.Add("proj-notes", "");
                    _titleCodeSequenceDescendants.Add("project-contact-information", "project-contact");
                    _titleCodeSequenceDescendants.Add("project-bidder-information", "");

                }
                return _titleCodeSequenceDescendants;
            }
        }

        /// <summary>
        /// Get a list of entity names from TargetEntityName attribute in the map
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetEntitiesToLoadTo()
        {
            if (_targetEntities == null)
            {
                XElement maps = _theMap.Element("EntityMaps");
                _targetEntities =
                    (from el in maps.Elements("EntityMap")
                    let s = el.Attribute("TargetEntityName").Value
                    select s).Distinct();
            }
            return _targetEntities;
        }

        public static Dictionary<string, string> GetSrcEntityElementNamesFromMap()
        {
            if (_srcEntityAndElementNames == null)
            {
                _srcEntityAndElementNames = new Dictionary<string, string>();
                IEnumerable<KeyValuePair<string, string>> srcEntityAndElementNameVPs;
                XElement maps = _theMap.Element("EntityMaps");
                srcEntityAndElementNameVPs =
                    from entMap in maps.Elements("EntityMap")
                    let x = new KeyValuePair<string, string>(entMap.Attribute("SourceEntityElementName").Value,
                                                            entMap.Attribute("SourceEntityName").Value)
                    select x;
                foreach (KeyValuePair<string, string> pair in srcEntityAndElementNameVPs)
                {
                    if (_srcEntityAndElementNames.ContainsKey(pair.Key))
                        continue;
                    _srcEntityAndElementNames.Add(pair.Key, GetXElemNameFromSrcEntName(pair.Value));
                }
            }
            return _srcEntityAndElementNames;
        }

        /// <summary>
        /// SourceEntityName in the map starts with a made up XElement name, get that name.
        /// </summary>
        /// <param name="mappedSourceEntityName">the value loaded from the SourceEntityName in the map</param>
        /// <returns></returns>
        public static string GetXElemNameFromSrcEntName(string mappedSourceEntityName)
        {
            int i = mappedSourceEntityName.IndexOf("/");
            if (i != -1)
                return mappedSourceEntityName.Substring(0, i);
            else
                return mappedSourceEntityName;
        }

        public static IEnumerable<XElement> GetSourceElementsMapsForTargetEntity(string targetEntityName)
        {
            XElement startElement = _theMap.Element("EntityMaps");

            IEnumerable<XElement> sourceElementMaps =
                from el in startElement.Elements("EntityMap")
                where (string)el.Attribute("TargetEntityName") == targetEntityName
                select el;

            return sourceElementMaps;
        }

        /// <summary>
        /// Build a dictionary of attribute mappings for which no formula is used, 
        /// keyed by the target attribute
        /// </summary>
        /// <param name="mapElem">the map element at EntityMap node level</param>
        /// <returns>Dictionary with key= target attribute and value = source attribute</returns>
        /// <remarks>if the taget attribute is duplicated in mapElem, any occurrence after the first is ignored</remarks>
        public static Dictionary<string, string> GetSimpleAttributesMap(XElement mapElem)
        {
            //get straigt one-to-one mappings from mapElem, including subnodes to be processed,
            //excluding those mappings that have formula
            XElement attribMaps = new XElement(mapElem.Element("AttributeMaps"));
            IEnumerable<XElement> simpleAttribMaps =
                from attrMap in attribMaps.Elements("AttributeMap")
                where (((string)attrMap.Element("ProcessCode")).Contains("Process") &&
                    attrMap.Element("TargetAttributeName") != null &&
                    !((string)attrMap.Element("TargetAttributeName")).StartsWith("="))
                select attrMap;

            //build a dictionary for mappings
            Dictionary<string, string> mapDict = new Dictionary<string, string>();

            foreach (XElement e in simpleAttribMaps)
            {
                string tgtAttr = ((string)e.Element("TargetAttributeName")).Trim();
                if (!mapDict.Keys.Contains(tgtAttr))
                    mapDict.Add(tgtAttr, ((string)e.Element("SourceAttributeName")).Trim());
            }

            return mapDict;
        }

        /// <summary>
        /// Build a dictionary of attribute mappings for which a formula IS used, 
        /// keyed by the target attribute
        /// </summary>
        /// <param name="mapElem">the map element at EntityMap node level</param>
        /// <returns>Dictionary with key= target attribute and value = source attribute</returns>
        /// <remarks>if the taget attribute is duplicated in mapElem, any occurrence after the first is ignored</remarks>
        public static Dictionary<string, string> GetComplexAttributesMap(XElement mapElem)
        {
            //get mappings from mapElem, including subnodes to be processed,
            //filtering those mappings that have formula
            XElement attribMaps = new XElement(mapElem.Element("AttributeMaps"));
            IEnumerable<XElement> simpleAttribMaps =
                from attrMap in attribMaps.Elements("AttributeMap")
                where (((string)attrMap.Element("ProcessCode")).Contains("Process") &&
                    attrMap.Element("TargetAttributeName") != null &&
                    ((string)attrMap.Element("TargetAttributeName")).StartsWith("="))
                select attrMap;

            //build a dictionary for mappings
            Dictionary<string, string> mapDict = new Dictionary<string, string>();
            foreach (XElement e in simpleAttribMaps)
            {
                string tgtAttr = ((string)e.Element("TargetAttributeName")).Trim();
                if (!mapDict.Keys.Contains(tgtAttr))
                    mapDict.Add(tgtAttr, ((string)e.Element("SourceAttributeName")).Trim());
            }

            return mapDict;
        }
    }
#endregion

    public class DodgeToXypexMapper
    {
        # region fileLevelVariables
        string _publisher;
        string _ktc_project;
        string _contact;
        #endregion

        public DodgeToXypexMapper()
        {
            //DodgeToXypexMap.ReadMapXML();
            GetFileLevelValues();
        }


        private void GetFileLevelValues ()
        {
            //todo: read from Summary/publisher
            _publisher = "McGraw-Hill Construction Dodge";
            _ktc_project = "ktc_project";
            _contact = "contact";
            return;
        }


        /// <summary>
        /// The main entry method of the mapper
        /// </summary>
        /// <param name="inNodes">a list of input "source" elements</param>
        /// <returns>the list of output transformed elements</returns>
        public List<XElement> MapAndTransformNodes(List<XElement> inNodes)
        {
            List<XElement> nodesToLoad = new List<XElement>();
            List<XElement> entityNodesToLoad;

            foreach (string entityName in DodgeToXypexMap.GetEntitiesToLoadTo())
            {
                entityNodesToLoad = MapAndTransformEntity(inNodes, entityName);
                nodesToLoad.AddRange(entityNodesToLoad);
            }
            return nodesToLoad;
        }

        public List<XElement> MapAndTransformEntity(List<XElement> inNodes, string entityName)
        {
            List <XElement> entities = new List<XElement>();
            //find source entities / input Xelements that are mapped to target entity e.g. Account
            IEnumerable<XElement> sourceElemMaps = DodgeToXypexMap.GetSourceElementsMapsForTargetEntity(entityName);
            
            //loop through each source element and add to Account Xelement 
            foreach (XElement srcMap in sourceElemMaps)
            {
                 //find the corresponding in node to be mapped
                string mapSrcElemName = srcMap.Attribute("SourceEntityName").Value;
                string srcElemName = DodgeToXypexMap.GetXElemNameFromSrcEntName(srcMap.Attribute("SourceEntityName").Value);

                XElement srcNode = inNodes.Find(x => srcElemName == x.Name.LocalName);
                if (srcNode != null)
                {
                    //get the various map dictionaries
                    Dictionary<string, string> simpleAttrMaps = DodgeToXypexMap.GetSimpleAttributesMap(srcMap);
                    Dictionary<string, string> complexAttrMaps = DodgeToXypexMap.GetComplexAttributesMap(srcMap);

                    //we may have a sequence of similar elements e.g. for contact information and bid information
                    //so we need to loop through all similar nodes that will each output an entity 
                    List<XElement> mapOutNodes = TransformMultipleSimilarNodesForEntity(srcNode, srcMap, simpleAttrMaps, complexAttrMaps);
                    if (mapOutNodes.Count != 0)
                    {
                        if (mapOutNodes.Count == 1 && entityName == "ktc_project")
                        {
                            if (entities.Count == 1)
                            {
                                entities[0].Add(mapOutNodes[0].Elements());
                            }
                            else
                            {
                                entities.Add(mapOutNodes[0]);
                            }
                        }
                        else
                        {
                            entities.AddRange(mapOutNodes);
                        }
                    }
                }
            }

            return entities;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inElement"></param>
        /// <param name="entMapElement"></param>
        /// <returns></returns>
        public XElement FilterAttributesOfInterest(XElement inElement, XElement entMapElement)
        {
            XElement outElem = inElement;
            //see if there are any special process codes
            XElement attribMaps = new XElement(entMapElement.Element("AttributeMaps"));
            IEnumerable<XElement> specialAttribMaps =
                from attrMap in attribMaps.Elements("AttributeMap")
                where ((attrMap.Element("ProcessCode")).Value != "Process")
                select attrMap;

            if (specialAttribMaps != null)
            {
                foreach (XElement aMap in specialAttribMaps)
                {
                    string processCode = aMap.Element("ProcessCode").Value;
                    XElement inAttributeElement = outElem.Element(aMap.Element("SourceAttributeName").Value);
                    if (inAttributeElement != null)
                    {
                        switch (processCode)
                        {
                            case "Ignore":
                                inAttributeElement.Remove();
                                break;
                            case "IfPiTrueProcess":
                                XAttribute piAttrib = inAttributeElement.Attribute("pi");
                                if (piAttrib == null || piAttrib.Value == "N")
                                    inAttributeElement.Remove();
                                break;
                            default:
                                break;
                        }
                    }
                }

            }


            return outElem;
        }

        public List<XElement> TransformMultipleSimilarNodesForEntity(XElement inElem, XElement srcMap,
            Dictionary<string, string> simpleMaps, Dictionary<string, string> complexMaps)
        {                    
            List<XElement> outElems = new List<XElement>();
            //get descendant path of inElem based on source entity name eg SourceEntityName="projectContact/project-contact/contact-information"
            string srcEntName = (string) srcMap.Attribute("SourceEntityName");
            string descPath = srcEntName.Substring(srcEntName.LastIndexOf('/')+1);
            
            IEnumerable<XElement> singleInNodes = 
                from XElement e in inElem.Descendants(descPath)
                select e;
            XElement singleInEl;
            //loop through in nodes and create transformed output
            foreach (XElement inEl in singleInNodes)
            {
                //get rid of <title-code> if required
                if (inEl.Element("title-code") != null)
                    singleInEl = inEl.Element("title-code");
                else
                    singleInEl = inEl;

                //get rid of not needed source attributes
                //filter attributes/elements of interest by looking at the ProcessCode in the entityMap
                singleInEl = FilterAttributesOfInterest(singleInEl, srcMap);

                if (singleInEl != null)
                {

                    //build the out element:
                    //1. get the straight 1 to 1 mappings ignoring those starting with "=", including
                    //   any constant source values like {PUBLISHER}
                    XElement outEntity = TransformSimpleMap(singleInEl, srcMap, simpleMaps);

                    //2. get mappings using transformations ie starting with "=" (later use transformation maps?)
                    outEntity = TransformComplexMap(singleInEl, srcMap, complexMaps, outEntity);

                    if (outEntity != null)
                    {
                        outElems.Add(outEntity);
                    }
                }
            }
            return outElems;
        }

        public XElement TransformSimpleMap(XElement singleInEl, XElement mapElem, Dictionary<string, string> mapDict)
        {
            //1. get the straight 1 to 1 mappings ignoring those starting with "="
            XElement singleOutElem = TransformOneToOneSimple(singleInEl, mapElem, mapDict);
            //2. add any constant source values like {PUBLISHER}
            TransformOneToOneConstants(mapDict, singleOutElem);

            return singleOutElem;
        }

        /// <summary>
        /// Transform attributes with complex mappings
        /// </summary>
        /// <param name="singleInEl"></param>
        /// <param name="mapElem"></param>
        /// <param name="mapDict">attribute map dictionary keyed by target attribute, which actually is a function expression</param>
        /// <param name="outElement"></param>
        /// <returns></returns>
        public XElement TransformComplexMap(XElement singleInEl, XElement mapElem, Dictionary<string, string>mapDict, XElement outElement)
        {
            if (mapDict.Count == 0)
                return outElement;

            if (outElement == null)
            {
                outElement = new XElement((string)mapElem.Attribute("TargetEntityName"));
            }
            Dictionary<string, Dictionary<int, string>> funcMaps = GetFunctionMaps(mapDict);

            foreach (string funcKey in funcMaps.Keys)
            {
                string outAttrName = GetOutputAttributeNameFromFunctionKey(funcKey);
                Dictionary<int, string> paramValues = GetParamValuesForFunction(singleInEl, funcKey, funcMaps[funcKey]);
                //if there is no input data, no need to add the output
                if (paramValues.Count > 0)
                {
                    string outValue = string.Empty;
                    try
                    {
                        outValue = ResolveFunction(GetFunctionVerb(funcKey), paramValues);
                    }
                    catch (Exception e)
                    {

                        XypexLogger.HandleException(e);
                        Console.WriteLine("funcKey: " + funcKey + ", attrib: " + outAttrName + ", attrib.Value: " + outValue);
                        Console.WriteLine(e.Message);
                    }
                    outElement.Add(new XElement(outAttrName, outValue));
                }
            }

            return outElement;
        }

        public XElement TransformOneToOneConstants(Dictionary<string, string> mapDict, XElement outElem)
        {
            IEnumerable<KeyValuePair<string, string>> constMapAttribs =
                from kvp in mapDict
                where kvp.Value.StartsWith("{")
                select kvp;

            if (constMapAttribs != null)
            {
                foreach (KeyValuePair<string, string> mapAttr in constMapAttribs)
                {                    
                    AddConstantAttributeToElement(mapAttr.Value, mapAttr.Key, outElem);
                }
            }

            return outElem;
        }

        /// <summary>
        /// Transform and output the Xelement corresponding to one CRM entity (ie one database record)
        /// </summary>
        /// <param name="singleInEl"></param>
        /// <param name="mapElem">the XElement map at EntityMap node level</param>
        /// <param name="mapDict">a map dictionary keyed by target attribute having one-to-one mappings</param>
        /// <returns>Xelement corresponding to one CRM entity</returns>
        public XElement TransformOneToOneSimple(XElement singleInEl, XElement mapElem, Dictionary<string, string> mapDict)
        {
            XElement singleOutEl = new XElement((string)mapElem.Attribute("TargetEntityName"));
            /*
            IEnumerable<XElement> attribsOfInterest =
                from XElement inAttr in singleInEl.Descendants()
                where mapDict.ContainsValue(inAttr.Name.ToString())
                select inAttr;
            */
            foreach (KeyValuePair<string,string>  attribMap in mapDict)
            {
                XElement inAttrib = null, outAttrib = null;
                if (attribMap.Value.StartsWith("{"))
                    continue;
                try
                {   //get the "in" attribute from the input element
                    IEnumerable<XElement> attribsOfInterest =
                         from XElement inAttr in singleInEl.Descendants()
                         where inAttr.Name.LocalName == attribMap.Value //.StartsWith(attribMap.Value)
                         select inAttr;
                    
                    //inAttrib = singleInEl.Element(attribMap.Value);
                    foreach (XElement a in attribsOfInterest)
                    {
                        inAttrib = a;
                        outAttrib = new XElement(attribMap.Key, inAttrib.Value);
                        if (inAttrib.Attribute("ci") != null)
                            outAttrib.Add(new XAttribute(inAttrib.Attribute("ci")));
                        singleOutEl.Add(outAttrib);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("map target attrib: " + attribMap.Key + ", map source attrib: " + attribMap.Value );
                    Console.WriteLine("inAttrib.Value: " + (inAttrib != null ? inAttrib.Value : ""));
                    Console.WriteLine(e.Message);
                }
            }

            return singleOutEl;
        }


        private void AddConstantAttributeToElement(string constantName, string attrName, XElement outElem)
        {

            outElem.Add(new XElement(attrName, GetConstantAttributeValue(constantName)));

        }

        private string GetConstantAttributeValue(string constantName)
        {
            string value = String.Empty;
            switch (constantName)
            {
                case "{PUBLISHER}":
                    value = _publisher;
                    break;
                case "{PROJECT}":
                    value = _ktc_project;
                    break;
                case "{CONTACT}":
                    value = _contact;
                    break;
                default:
                    break;
            }
            return value;
        }

        #region functions
        /// <summary>
        /// Split the input map into individual function maps, affecting the same output attribute
        /// </summary>
        /// <param name="inMap"></param>
        /// <returns>
        /// A dictionary keyed by output attribute name, of dictionaries representing the parameters,
        /// which include the ordinal value of processing and the parameter value (input attribute or constant)
        /// </returns>
        public Dictionary<string, Dictionary<int, string>> GetFunctionMaps(Dictionary<string, string> inMap)
        {
            Dictionary<string, Dictionary<int, string>> outMaps = new Dictionary<string,Dictionary<int,string>>();

            foreach (KeyValuePair<string, string> inMapItem in inMap)
            {
                List<string> outAttributes = ParseFunctionOutputAttributes(inMapItem.Key);
                foreach (string outAttribute in outAttributes)
                {
                    string funcKey= outAttribute + ParseFunctionName(inMapItem.Key);
                    if (!outMaps.ContainsKey(funcKey))
                    {
                        //add dictionary
                        outMaps.Add(funcKey, new Dictionary<int,string>()); 
                    }
                    //add the parameter
                    int ord = GetParamOrdinalValue(inMapItem.Key);
                    outMaps[funcKey].Add(ord, inMapItem.Value);
                }
            }
            return outMaps;
        }

        /// <summary>
        /// Find a list of target attributes that show at the end of a function expression
        /// </summary>
        /// <param name="mapvalue">a function expression from the value of a target attribute in the map</param>
        /// <returns></returns>
        /// <example>From mapvalue= "=Concatenate(%2, telephone)" return a list with one element, "telephone"</example>
        private List<string> ParseFunctionOutputAttributes(string mapvalue)
        {
            List<string> outAttributes = new List<string>();
            //strip the closing bracket
            string stripped = mapvalue.Substring(0,mapvalue.Length-1);
            //find the last occurrence of ","
            int i = stripped.LastIndexOf(",");
            while (i !=-1)
            {
                string name = (stripped.Substring(i + 1)).TrimStart();
                if (name.StartsWith("%"))
                    break;
                outAttributes.Add(name);
                stripped = stripped.Substring(0, i);
                i = stripped.LastIndexOf(",");
            }
            return outAttributes;
        }

        private string ParseFunctionName(string mapValue)
        {
            int bracketStart = mapValue.IndexOf("(");
            string funcName = mapValue.Substring(0, bracketStart);
            //special case for Lookup and LookupIfAlpha
            if (funcName.StartsWith("=Lookup"))
            {
                //also append ":" followed by the dictionary name
                int commaIndex = mapValue.IndexOf(",");
                funcName = funcName + ":";
                if (commaIndex > bracketStart)
                {
                    funcName = funcName + mapValue.Substring(bracketStart + 1, commaIndex - bracketStart - 1);
                }

            }
            else
            {
                if (funcName.StartsWith("=Concatenate") && mapValue.Contains("*"))
                {
                    funcName = "=ConcatAll";
                }
            }

            return funcName;
        }

        private string ExtractVerbExtension(string functionVerb)
        {
            string extra = String.Empty;
            int i = functionVerb.IndexOf(":");
            if (i != -1)
            {
                extra = functionVerb.Substring(i + 1);
            }
            return extra;
        }
        
        private string ExtractRealVerb(string functionVerb)
        {
            string verb = functionVerb;
            int i = functionVerb.IndexOf(":");
            if (i != -1)
            {
                verb = functionVerb.Substring(0,i);
            }
            return verb;
        }

        /// <summary>
        /// return zero-based ordinal value of parameter from an input string in the format %1 or *
        /// </summary>
        /// <param name="mapvalue"></param>
        /// <returns></returns>
        private int GetParamOrdinalValue(string mapvalue)
        {
            int i = mapvalue.IndexOf("%");
            if (i == -1)
            {
                i = mapvalue.IndexOf("*");
                if (i != -1)
                    return 0;
                else
                    return i;
            }

            i++;
            int j = (mapvalue.Substring(i)).IndexOf(",");
            if (j == -1)
                j = mapvalue.Length - i - 1;
            return Convert.ToInt32(mapvalue.Substring(i, j)) - 1;
        }

        /// <summary>
        /// Get the name of the return attribute
        /// </summary>
        /// <param name="funcKey">function key represented as [attribName]=[functionverb]</attribName></param>
        /// <returns>attribName</returns>
        private string GetOutputAttributeNameFromFunctionKey(string funcKey)
        {
            int i = funcKey.IndexOf("=");
            return funcKey.Substring(0, i);
        }

        /// <summary>
        /// Get the name of the function verb
        /// </summary>
        /// <param name="funcKey">function key represented as [attribName]=[functionVerb]</attribName></param>
        /// <returns>functionVerb</returns>
        private string GetFunctionVerb(string funcKey)
        {
            int i = funcKey.IndexOf("=");
            return funcKey.Substring(i + 1);
        }

        private Dictionary<int, string> GetParamValuesForFunction(XElement singleInEl, string functionKey, Dictionary<int, string> funcMap)
        {
            Dictionary<int, string> paramVals = new Dictionary<int, string>();
            foreach (int i in funcMap.Keys)
            {
                //get the value of the attribute in the input element having the tag as the value in the map dictionary
                //or a constant value
                try
                {
                    if (funcMap[i].StartsWith("{"))
                    {
                        paramVals.Add(i, GetConstantAttributeValue(funcMap[i])); 
                    }
                    else
                    {
                        //PiAttrib case
                        if (functionKey.Contains("PiAttrib"))
                        {
                            string attrVal = GetParamValueForPiAttribFunction(singleInEl, funcMap[i]);
                            paramVals.Add(i, attrVal);
                        }
                        else
                        {
                            if (functionKey.Contains("IfPiTrue"))
                            {
                                string attrVal = GetParamValueForIfPiTrueFunction(singleInEl, funcMap[i]);
                                paramVals.Add(i, attrVal);
                            }
                            else
                            {
                                if (functionKey.Contains("ConcatAll"))
                                {
                                    string attrVal = GetParamValueForConcatAllFunction(singleInEl, funcMap[i]);
                                    paramVals.Add(i, attrVal);
                                }
                                else
                                {
                                    //check if the source attribute exists in the in element
                                    if (singleInEl.Element(funcMap[i]) != null)
                                    {
                                        paramVals.Add(i, (string)singleInEl.Element(funcMap[i]));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("i: " + i.ToString() + "funcMap[i]: " + (string)funcMap[i]);
                    Console.WriteLine(e.Message);
                }
            }
            return paramVals;
        }

        private string GetParamValueForPiAttribFunction(XElement inElement, string paramSourceName)
        {
            string paramValue = string.Empty;
            foreach (XElement xe in inElement.Elements())
            {
                if (xe.Name.LocalName.Contains(paramSourceName))
                {
                    XAttribute xa = xe.Attribute("pi");
                    if (xa != null)
                    {
                        paramValue = xa.Value;
                        break;//foreach
                    }
                }
            }
            return paramValue;
        }

        private string GetParamValueForIfPiTrueFunction(XElement inElement, string paramSourceName)
        {
            string paramValue = string.Empty;
            foreach (XElement xe in inElement.Elements())
            {
                if (xe.Name.LocalName.Contains(paramSourceName))
                {
                    XAttribute xa = xe.Attribute("pi");
                    if ((xa != null) && (xa.Value == "Y"))
                    {
                        paramValue = xe.Value;
                        break;//foreach
                    }
                }
            }
            return paramValue;
        }

        private string GetParamValueForConcatAllFunction(XElement inElement, string paramSourceName)
        {
            string paramValue = string.Empty;
            foreach (XElement xe in inElement.Elements(paramSourceName))
            {
                paramValue += xe.Value;
            }
            return paramValue;
        }

        public string ResolveFunction(string verb, Dictionary<int, string> paramValues)
        {
            string outVal = string.Empty;
            string realVerb = ExtractRealVerb(verb);

            switch (realVerb)
            {
                case "Concatenate":
                    for (int i = 0; i < paramValues.Keys.Count; i++)
                    {
                        outVal += paramValues[i];
                    }
                    break;
                case "Split":
                    //this implementation assumes there are always only two parameters
                    //i.e. Split splits an input string into two values having a space separator
                    //with the rightmost separator determining the value of parameter %2 (paramvalues[1])
                    if (paramValues.Count > 1)
                        throw new Exception("Cannot process more than one parameter at a time in Split.");
                    KeyValuePair<int, string> pair = paramValues.ElementAt(0);
                    int startOfValue = pair.Value.LastIndexOf(" ");
                    switch (pair.Key)
                    {
                        case 0:
                            outVal = pair.Value.Substring(0, startOfValue);
                            break;
                        case 1:
                            outVal = pair.Value.Substring(startOfValue + 1);
                            break;
                        default:
                            throw new Exception("Cannot process more than 2 parameters in Split. Index out of order.");
                    }
                    break;
                case "Lookup":
                    if (paramValues.Count > 1)
                        throw new Exception("Cannot process more than one parameter in Lookup.");
                    //the verb extension represents the dictionary name to be used for lookup
                    string dictName = ExtractVerbExtension(verb);
                    string key = paramValues[0];
                    object outObj = Lookups.FindLookupValue(dictName, key);
                    if (outObj.GetType() == typeof(LookupObjectNotFound))
                    {
                        if (((LookupObjectNotFound)outObj).Code == LookupObjectNotFound.LookupNotFound.DictionaryNotFound)
                            throw new Exception("Cannot find lookup dictionary: " + dictName + " for function " + verb);
                        else
                            throw new Exception("Cannot find key: " + key + " in dictionary '" + dictName + "'" + " for function " + verb);
                    }
                    outVal = outObj.ToString();
                    break;
                case "LookupIfAlpha":
                    if (paramValues.Count > 1)
                        throw new Exception("Cannot process more than one parameter in Lookup.");
                    //try
                    //{
                    //    decimal decval = Convert.ToDecimal(paramValues[0]);
                    //}
                    if (paramValues[0].Length == 1)
                    {
                        string dictName1 = ExtractVerbExtension(verb);
                        object outObj1 = Lookups.FindLookupValue(dictName1, paramValues[0]);
                        if (outObj1.GetType() == typeof(LookupObjectNotFound))
                        {
                            if (((LookupObjectNotFound)outObj1).Code == LookupObjectNotFound.LookupNotFound.DictionaryNotFound)
                                throw new Exception("Cannot find lookup dictionary: " + dictName1 + " for function " + verb);
                            else
                                throw new Exception("Cannot find key: " + paramValues[0] + " in dictionary '" + dictName1 + "'" + " for function " + verb);
                        }
                        outVal = outObj1.ToString();

                    }
                    else
                    {
                        outVal = paramValues[0];
                    }
                    break;
                case "PiAttrib":
                    if (paramValues.Count > 1)
                        throw new Exception("Cannot process more than one parameter in PiAttrib.");
                    outVal = paramValues[0];
                    break;
                default:
                    outVal = paramValues[0];
                    break;
            }

            return outVal;
        }

        #endregion
    }
}

