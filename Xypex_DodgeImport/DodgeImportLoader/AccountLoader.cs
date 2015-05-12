using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using XypexCRM;
using System.Xml;
using System.Xml.Linq;


namespace DodgeImportLoader
{
    public class AccountLoader
    {
        IOrganizationService service;
        Dictionary<string, string> loadAttributes;
        Account _account;
        UserOrTeamAssignment _assigner;


        public AccountLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
        }

        public AccountLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner, Dictionary<string, string> accountAttributes)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            loadAttributes = accountAttributes;
        }

        public Dictionary<string, string> LoadAttributes
        {
            get
            {
                return loadAttributes;
            }
            set
            {
                loadAttributes = value;
            }
        }

        public Guid LoadEntity(XElement inEntity)
        {
            Dictionary<string, string> attribDict = new Dictionary<string, string>();
            Guid AcctId = Guid.Empty;

            foreach (XElement attrElem in inEntity.Elements())
            {
                attribDict.Add(attrElem.Name.LocalName, attrElem.Value);
            }

            LoadAttributes = attribDict;

            Account crmAcct = PrimaryMatch(LoadAttributes["ktc_External_Data_Source_Match_ID"]);
            if (crmAcct == null)
            {
                Dictionary<string, string> secMatchAttribs = GetSecondaryMatchAttributes(attribDict);
                crmAcct = SecondaryMatch(secMatchAttribs);
                if (crmAcct != null)
                {
                    UpdateSelectedFieldsInRecord();
                    AcctId = crmAcct.Id;
                }
            }
            else
            {
                AcctId = crmAcct.Id;
            }

            if (crmAcct == null)
            {
                CreateRecord(out AcctId);
            }

            return AcctId;
        }

        public Account PrimaryMatch(string primaryIdentifier)
        {
            ColumnSet cols = new ColumnSet(new string[] { "accountid", "name" });
            
            //identify and return record for which ktc_External_Data_Source_Match_ID has the value provided in primaryIdentifier
            QueryExpression findByKtcMatchId = new QueryExpression 
            {
                EntityName = Account.EntityLogicalName,
                ColumnSet = cols,
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

            try
            {
                DataCollection<Entity> accts = service.RetrieveMultiple(findByKtcMatchId).Entities;
                if (accts != null && accts.Count > 0)
                {
                    _account = (Account)accts[0];
                    return _account;
                }
                else
                    return null;
            }
            catch {
                throw;
            }
        }

        public Account SecondaryMatch(Dictionary<string, string> matchingAttributes)
        {
            ColumnSet colset = new ColumnSet(new string[] { "accountid", "name", "ktc_external_data_source_id" });
            FilterExpression fe = new FilterExpression();
            foreach (string attrib in matchingAttributes.Keys)
            {
                if (matchingAttributes[attrib] != String.Empty)
                    fe.Conditions.Add(new ConditionExpression(attrib, ConditionOperator.Equal, matchingAttributes[attrib]));
            }
            fe.Conditions.Add(new ConditionExpression("ktc_external_data_source_name", ConditionOperator.NotEqual, "McGraw-Hill Construction Dodge"));
            QueryExpression qe = new QueryExpression(Account.EntityLogicalName);
            qe.ColumnSet = colset;
            qe.Criteria = fe;

            try
            {
                DataCollection<Entity> accounts = service.RetrieveMultiple(qe).Entities;
                if (accounts != null && accounts.Count > 0)
                {
                    _account = (Account)accounts[0];
                    return _account;
                }
                else
                    return null;
            }
            catch
            {
                throw;
            }
        }

        public bool CreateRecord(out Guid accountId)
        {
            bool created = false;
            Account newAccount = InitializeAccount(null);
            try
            {
                accountId = service.Create(newAccount);
                if (accountId != Guid.Empty)
                {
                    created = _assigner.AssignEntityToTeam(UserOrTeamAssignment.ProjectAdminTeamId(service),
                        new EntityReference(Account.EntityLogicalName, accountId));
                }
            }
            catch
            {
                throw;
            }

            return created;
       }

        /// <summary>
        /// Update the Account record found by a previous match with the values from LoadAttributes
        /// </summary>
        /// <returns></returns>
        public bool UpdateSelectedFieldsInRecord()
        {
            bool updated = false;
            InitializeSelectedFieldsForUpdate(_account);
            try
            {
                service.Update(_account);
                updated = true;
            }
            catch
            {
                throw;
            }
            return updated;
        }

        public bool DeleteRecord()
        {
            throw new Exception("Not implemented.");
            //return false;
        }

        public bool AssignOwner()
        {
            throw new Exception("Not implemented.");
            //return false;
        }

        private Account InitializeAccount(Account thisAccount)
        {
            if (thisAccount == null)
            {
                thisAccount = new Account();
            }
            //ktc identifying fields
            //set the source date to the time the account is initialized for now
            DateTime sourceDate;
            try
            {
                if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Date") && LoadAttributes["ktc_External_Data_Source_Date"].Length == 8)
                {
                    String srcDateVal = LoadAttributes["ktc_External_Data_Source_Date"];
                    sourceDate = new DateTime(Convert.ToInt32(srcDateVal.Substring(0, 4)), Convert.ToInt32(srcDateVal.Substring(4, 2)), Convert.ToInt32(srcDateVal.Substring(6, 2)));
                }
                else
                {
                    sourceDate = DateTime.Now;
                }
            }
            catch
            {
                sourceDate = DateTime.Now;
            }

            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Date")) //????
                thisAccount.ktc_External_Data_Source_Date = sourceDate;
            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_ID"))
                thisAccount.ktc_External_Data_Source_ID = LoadAttributes["ktc_External_Data_Source_ID"];
            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Name"))
                thisAccount.ktc_External_Data_Source_Name = LoadAttributes["ktc_External_Data_Source_Name"];
            //Note: thisAccount.ktc_External_Data_Source_Match_ID has no setter, is readonly;

            //Note: thisAccount.Address1_Composite is readonly, no setter
            if (LoadAttributes.ContainsKey("name"))
                thisAccount.Name = LoadAttributes["name"];
            if (LoadAttributes.ContainsKey("address1_line1"))
                thisAccount.Address1_Line1 = LoadAttributes["address1_line1"];
            if (LoadAttributes.ContainsKey("address1_line2"))
                thisAccount.Address1_Line2 = LoadAttributes["address1_line2"];
            if (LoadAttributes.ContainsKey("address1_line3"))
                thisAccount.Address1_Line3 = LoadAttributes["address1_line3"];
            if (LoadAttributes.ContainsKey("address1_county")) 
                thisAccount.Address1_County = LoadAttributes["address1_county"];
            if (LoadAttributes.ContainsKey("address1_city"))
                thisAccount.Address1_City = LoadAttributes["address1_city"];
            if (LoadAttributes.ContainsKey("address1_stateorprovince"))
                thisAccount.Address1_StateOrProvince = LoadAttributes["address1_stateorprovince"];
            if (LoadAttributes.ContainsKey("address1_postalcode"))
                thisAccount.Address1_PostalCode = LoadAttributes["address1_postalcode"];
            if (LoadAttributes.ContainsKey("address1_counTRY"))
                thisAccount.Address1_Country = LoadAttributes["address1_counTRY"];
            //the attribute value for phone1 will be concatenated from 3 input fields by the XML parser-mapper
            if (LoadAttributes.ContainsKey("address1_telephone1"))
                 thisAccount.Address1_Telephone1 = LoadAttributes["address1_telephone1"];

            if (LoadAttributes.ContainsKey("telephone1"))
                thisAccount.Telephone1 = LoadAttributes["telephone1"];
            if (LoadAttributes.ContainsKey("websiteurl"))
                thisAccount.WebSiteURL = LoadAttributes["websiteurl"];
            if (LoadAttributes.ContainsKey("ktc_External_Source_Profile_URL"))
                thisAccount.ktc_External_Source_Profile_URL = LoadAttributes["ktc_External_Source_Profile_URL"];

            return thisAccount;
        }

        public Dictionary<string, string> GetSecondaryMatchAttributes(Dictionary<string, string> attribDict)
        {
            Dictionary<string, string> matchAttribs = new Dictionary<string, string>();

            var matchKeys = new HashSet<string> {"name", "address1_street1", "address1_city",
                "address1_stateorprovince", "address1_country"};

            var filter = attribDict
                .Where(p => matchKeys.Contains(p.Key))
                .ToDictionary(p => p.Key.ToLower(), p => p.Value);
            matchAttribs = filter;

            return matchAttribs;
        }

        private Account InitializeSelectedFieldsForUpdate(Account thisAccount)
        {
            if (thisAccount == null)
            {
                thisAccount = new Account();
            }
            //ktc identifying fields
            //set the source date to the time the account is initialized for now
            DateTime sourceDate;
            try
            {
                if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Date") && LoadAttributes["ktc_External_Data_Source_Date"].Length == 8)
                {
                    String srcDateVal = LoadAttributes["ktc_External_Data_Source_Date"];
                    sourceDate = new DateTime(Convert.ToInt32(srcDateVal.Substring(0, 4)), Convert.ToInt32(srcDateVal.Substring(4, 2)), Convert.ToInt32(srcDateVal.Substring(6, 2)));
                }
                else
                {
                    sourceDate = DateTime.Now;
                }
            }
            catch
            {
                sourceDate = DateTime.Now;
            }

            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Date")) //????
                thisAccount.ktc_External_Data_Source_Date = sourceDate;
            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_ID"))
                thisAccount.ktc_External_Data_Source_ID = LoadAttributes["ktc_External_Data_Source_ID"];
            if (LoadAttributes.ContainsKey("ktc_External_Data_Source_Name"))
                thisAccount.ktc_External_Data_Source_Name = LoadAttributes["ktc_External_Data_Source_Name"];
            if (LoadAttributes.ContainsKey("ktc_External_Source_Profile_URL"))
                thisAccount.ktc_External_Source_Profile_URL = LoadAttributes["ktc_External_Source_Profile_URL"];
            
            return thisAccount;
        }
    }
}
