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

    public class ContactLoader
    {
        IOrganizationService service;
        XElement _xloadAttributes;
        Contact _thisContact;
        UserOrTeamAssignment _assigner;

        public ContactLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
        }

        public ContactLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner, XElement connectionAttributes)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            _xloadAttributes = connectionAttributes;
            //PrepareLoadAttributes();
        }

        public XElement LoadAttributes
        {
            get
            {
                return _xloadAttributes;
            }
            set
            {
                _xloadAttributes = value;
            }
        }

        public Guid LoadEntity(XElement inElement)
        {
            Contact crmContact;
            Guid contactId = Guid.Empty;

            if (inElement.Element("firstname") == null && inElement.Element("lastname") == null)
                return Guid.Empty;

            LoadAttributes = inElement;

            //identify the contact using primary match
            if (inElement.Element("ktc_External_Data_Source_Match_ID") != null)
            {
                crmContact = PrimaryMatch(inElement.Element("ktc_External_Data_Source_Match_ID").Value);

                //updates are to be performed only if found by secondary match 
                //and only for external data source identifying fields
                if (crmContact != null)
                {
                    contactId = crmContact.Id;//return contactId for use in connections
                }
                else
                {
                    crmContact = SecondaryMatch(inElement);
                    if (crmContact != null)
                    {
                        contactId = crmContact.Id;
                        UpdateExtDataSourceFieldsInRecord();
                    }
                }
                if (crmContact == null)
                {
                    CreateRecord(out contactId);
                }
            }
            return contactId;
        }

        public Contact PrimaryMatch(string primaryIdentifier)
        {
            ColumnSet cols = new ColumnSet(new string[] { "contactid", "fullname" });

            //identify and return record for which ktc_External_Data_Source_Match_ID has the value provided in primaryIdentifier
            QueryExpression findByKtcMatchId = new QueryExpression
            {
                EntityName = Contact.EntityLogicalName,
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
                DataCollection<Entity> contacts = service.RetrieveMultiple(findByKtcMatchId).Entities;
                if (contacts != null && contacts.Count > 0)
                {
                    _thisContact = (Contact)contacts[0];
                    return _thisContact;
                }
                else
                    return null;
            }
            catch
            {
                throw;
            }
        }

        public Dictionary<string, string> GetSecondaryMatchAttributes(XElement attribElems)
        {

            Dictionary<string, string> matchAttribs = new Dictionary<string, string>();

            var matchKeys = new HashSet<string> {"firstname", "lastname",
                "address1_line1", "address1_city", "address1_stateorprovince", "address1_country"};

            IEnumerable<XElement> matchElems = from e in attribElems.Elements()
                                               where matchKeys.Contains(e.Name.LocalName)
                                               select e;

            matchAttribs = matchElems.ToDictionary(p => (p.Name.LocalName).ToLower(), p => p.Value);

            return matchAttribs;
        }

        public Contact SecondaryMatch(XElement inElement)
        {

            Dictionary<string, string> matchingAttributes = GetSecondaryMatchAttributes(inElement);

            ColumnSet colset = new ColumnSet(new string[] { "contactid", "firstname", "lastname", "ktc_external_data_source_id" });
            FilterExpression fe = new FilterExpression();
            foreach (string attrib in matchingAttributes.Keys)
            {
                if (matchingAttributes[attrib] != String.Empty)
                    fe.Conditions.Add(new ConditionExpression(attrib, ConditionOperator.Equal, matchingAttributes[attrib]));
            }
            fe.Conditions.Add(new ConditionExpression("ktc_external_data_source_name", ConditionOperator.NotEqual, "McGraw-Hill Construction Dodge"));
            QueryExpression qe = new QueryExpression(Contact.EntityLogicalName);
            qe.ColumnSet = colset;
            qe.Criteria = fe;

            try
            {
                DataCollection<Entity> contacts = service.RetrieveMultiple(qe).Entities;
                if (contacts != null && contacts.Count > 0)
                {
                    _thisContact = (Contact)contacts[0];
                    return _thisContact;
                }
                else
                    return null;
            }
            catch
            {
                throw;
            }

        }

        public bool CreateRecord(out Guid contactId)
        {
            bool created = false;
            Contact newContact = InitializeContact(null);
            try
            {
                contactId = service.Create(newContact);
                if (contactId != Guid.Empty)
                {
                    created = _assigner.AssignEntityToTeam(UserOrTeamAssignment.ProjectAdminTeamId(service),
                        new EntityReference(Contact.EntityLogicalName, contactId));
                }
            }
            catch
            {
                throw;
            }

            return created;
       }


        private Contact InitializeContact(Contact thisContact)
        {
            if (thisContact == null)
            {
                thisContact = new Contact();
            }

            if (LoadAttributes.Attribute("accountId") != null)
                thisContact.ParentCustomerId = new EntityReference(Account.EntityLogicalName, (Guid) LoadAttributes.Attribute("accountId"));

            if (LoadAttributes.Element("ktc_External_Data_Source_Name") != null)
                thisContact.ktc_External_Data_Source_Name = LoadAttributes.Element("ktc_External_Data_Source_Name").Value;
            if (LoadAttributes.Element("ktc_External_Data_Source_ID") != null)
                thisContact.ktc_External_Data_Source_ID = LoadAttributes.Element("ktc_External_Data_Source_ID").Value;

            if (LoadAttributes.Element("firstname") != null)
                thisContact.FirstName = LoadAttributes.Element("firstname").Value;
            if (LoadAttributes.Element("lastname") != null)
                thisContact.LastName = LoadAttributes.Element("lastname").Value;

            if (LoadAttributes.Element("jobtitle") != null)
                thisContact.JobTitle = LoadAttributes.Element("jobtitle").Value;
            if (LoadAttributes.Element("address1_line1") != null)
                thisContact.Address1_Line1 = LoadAttributes.Element("address1_line1").Value;
            if (LoadAttributes.Element("address1_line2") != null)
                thisContact.Address1_Line2 = LoadAttributes.Element("address1_line2").Value;
            if (LoadAttributes.Element("address1_line3") != null)
                thisContact.Address1_Line3 = LoadAttributes.Element("address1_line3").Value;
            if (LoadAttributes.Element("address1_line2") != null)
                thisContact.Address1_Line2 = LoadAttributes.Element("address1_line2").Value;
            if (LoadAttributes.Element("address1_county") != null)
                thisContact.Address1_County = LoadAttributes.Element("address1_county").Value;
            if (LoadAttributes.Element("address1_city") != null)
                thisContact.Address1_City = LoadAttributes.Element("address1_city").Value;
            if (LoadAttributes.Element("address1_stateorprovince") != null)
                thisContact.Address1_StateOrProvince = LoadAttributes.Element("address1_stateorprovince").Value;
            if (LoadAttributes.Element("address1_postalcode") != null)
                thisContact.Address1_PostalCode = LoadAttributes.Element("address1_postalcode").Value;
            if (LoadAttributes.Element("address1_country") != null)
                thisContact.Address1_Country = LoadAttributes.Element("address1_country").Value;
            if (LoadAttributes.Element("telephone1") != null)
                thisContact.Telephone1 = LoadAttributes.Element("telephone1").Value;
            if (LoadAttributes.Element("emailaddress1") != null)
                thisContact.EMailAddress1 = LoadAttributes.Element("emailaddress1").Value;

            return thisContact;
        }

        public bool UpdateExtDataSourceFieldsInRecord()
        {
            //only external_data_source fields are to be updated
            bool updated = false;
            if (LoadAttributes.Element("ktc_External_Data_Source_Name") != null)
                _thisContact.ktc_External_Data_Source_Name = LoadAttributes.Element("ktc_External_Data_Source_Name").Value;
            if (LoadAttributes.Element("ktc_External_Data_Source_ID") != null)
                _thisContact.ktc_External_Data_Source_ID = LoadAttributes.Element("ktc_External_Data_Source_ID").Value;

            try
            {
                service.Update(_thisContact);
                updated = true;
            }
            catch
            {
                throw;
            }
            return updated;

        }
    }
}
