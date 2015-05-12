using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DodgeImportLoader;
using DodgeImportLoader.XypexSDKTypes;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace DodgeXMLParser
{
    public class LookupObjectNotFound
    {
        public enum LookupNotFound
        {
            DictionaryNotFound = -1,
            KeyNotFoundInDictionary = -2
        }

        public LookupNotFound Code;

        public LookupObjectNotFound(LookupNotFound value)
        {
            Code = value;
        }

    }

    public class Lookups
    {
        static Dictionary<string, Dictionary<string, object>> _lookupsDict;

        public static void LoadLookupsDictionary(OrganizationServiceProxy serviceProxy)
        {
            _lookupsDict = new Dictionary<string, Dictionary<string, object>>();
            AddProjectTypes(serviceProxy);
            AddProjectStages(serviceProxy);
            AddConnectionRoles(serviceProxy);
            AddHighValuations();
            AddCurrencies(serviceProxy);

        }

        private static void AddProjectTypes(OrganizationServiceProxy serviceProxy)
        {
            ProjectTypeTranslation lookupObject = new ProjectTypeTranslation(serviceProxy);
            Dictionary<string, object/*Guid*/> projectTypesDict = lookupObject.RetrieveIdDictionary();

            _lookupsDict.Add("ktc_project_types", projectTypesDict);
        }

        private static void AddProjectStages(OrganizationServiceProxy serviceProxy)
        {
            ProjectStageTranslation lookupObject = new ProjectStageTranslation(serviceProxy);
            Dictionary<string, object> projectStagesDict = lookupObject.RetrieveIdDictionary();

            _lookupsDict.Add("ktc_project_stages", projectStagesDict);
        }

        private static void AddConnectionRoles(OrganizationServiceProxy serviceProxy)
        {
            //this will be a 2 hops lookup:
            //- first, to get the translated connection names ("codes")
            ConnectionRoleTranslation lookupObject1 = new ConnectionRoleTranslation(serviceProxy);
            Dictionary<string, object> connRoleNamesDict = lookupObject1.RetrieveCodeDictionary();

            //-second, get the corresponding connection roles ids
            ConnectionRoles.RetrieveConnectionRolesOfInterest(new List<string>(new string[] {"Stakeholder"}), serviceProxy);
            Dictionary<string, Guid> connRoleIdsDict = ConnectionRoles.ConnectionRolesDictionary;

            Dictionary<string, object> connectionRolesLookupDict = new Dictionary<string, object>();
            foreach (string extConnRole in connRoleNamesDict.Keys)
            {   //some external names are not mapped
                if (connRoleNamesDict[extConnRole] != null)
                {
                    string connRoleIdsDictKey = "1000." + connRoleNamesDict[extConnRole].ToString();
                    if (connRoleIdsDict.ContainsKey(connRoleIdsDictKey))
                    {
                        connectionRolesLookupDict.Add(extConnRole, connRoleIdsDict[connRoleIdsDictKey]);
                    }
                }
            }
            _lookupsDict.Add("ktc_connection_roles", connectionRolesLookupDict);
        }

        private static void AddHighValuations()
        {
            Dictionary<string, object> HighValuations = new Dictionary<string,object>();

            HighValuations.Add("A",	"99999");
            HighValuations.Add("B",	"199999");
            HighValuations.Add("C",	"299999");
            HighValuations.Add("D",	"399999");
            HighValuations.Add("E", "499999");
            HighValuations.Add("F", "749999");
            HighValuations.Add("G",	"999999");
            HighValuations.Add("H", "2999999");
            HighValuations.Add("I", "4999999");
            HighValuations.Add("J", "9999999");
            HighValuations.Add("K", "14999999");
            HighValuations.Add("L", "24999999");
            HighValuations.Add("M", "49999999");
            HighValuations.Add("N", "999999999999999");
            _lookupsDict.Add("EstValueHigh", HighValuations);
        }

        private static void AddCurrencies(OrganizationServiceProxy serviceProxy)
        {
            TransactionCurrencyLookups crcyLookup = new TransactionCurrencyLookups(serviceProxy);
            Dictionary<string, object> currencies = crcyLookup.RetrieveAll();
            _lookupsDict.Add("Currencies", currencies);
        }

        public static object FindLookupValue(string dictionaryName, string lookupKey)
        {
            object retval;
            if (!_lookupsDict.ContainsKey(dictionaryName))
            {
                retval = new LookupObjectNotFound(LookupObjectNotFound.LookupNotFound.DictionaryNotFound);
                return retval;
            }

            Dictionary<string, object> foundDict = _lookupsDict[dictionaryName];
            if (!foundDict.ContainsKey(lookupKey))
            {
                retval = new LookupObjectNotFound(LookupObjectNotFound.LookupNotFound.KeyNotFoundInDictionary);
                return retval;
            }

            retval = foundDict[lookupKey];
            return retval;
        }

        public static bool DoesLookupKeyExist(string dictionaryName, string lookupKey)
        {
            if (!_lookupsDict.ContainsKey(dictionaryName))
            {
                return false;
            }

            return (_lookupsDict[dictionaryName].ContainsKey(lookupKey));
        }
    }
}
