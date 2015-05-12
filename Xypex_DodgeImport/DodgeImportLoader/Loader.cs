using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using XypexCRM;
using System.Xml;
using System.Xml.Linq;

namespace DodgeImportLoader
{
    public class Loader
    {
        OrganizationServiceProxy _serviceProxy;
        UserOrTeamAssignment _assigner;

#region AccountAndContacts definition
        public class AccountAndContacts
        {
            private XElement accountElement;
            private List<XElement> contactElements;
            private List<XElement> connectionElements;

            public AccountAndContacts(XElement account)
            {
                accountElement = account;
            }

            public AccountAndContacts(XElement account, List<XElement> contacts, List<XElement> connections)
            {
                accountElement = account;
                contactElements = contacts;
                connectionElements = connections;
            }
            
            public XElement AccountElement
            {
                get 
                {
                    return accountElement;
                }
                set
                {
                    accountElement = value;
                }
            }
            public List<XElement> ContactElements
            {
                get 
                {
                    return contactElements;
                }
                set
                {
                    contactElements = value;
                }
            }
            public List<XElement> ConnectionElements
            {
                get
                {
                    return connectionElements;
                }
                set
                {
                    connectionElements = value;
                }
            }
        }
#endregion
        public Loader ()
        {
            _serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
            _serviceProxy.EnableProxyTypes();
            _assigner = new UserOrTeamAssignment(_serviceProxy);
        }

        public OrganizationServiceProxy LoaderServiceProxy
        {
            get
            {
                return _serviceProxy;
            }
        }

        public void LoadDodgeReportEntities(List<XElement> entityElems)
        {

            //TO DO: add complete logic of loading entities in correct dependency order
            ProjectLoader projloader = new ProjectLoader(_serviceProxy, _assigner);
            AccountLoader acctLoader = new AccountLoader(_serviceProxy, _assigner);
            ContactLoader contLoader = new ContactLoader(_serviceProxy, _assigner);
            ConnectionLoader connLoader = new ConnectionLoader(_serviceProxy, _assigner);

            Guid projId = LoadProjectRelatedEntities(entityElems, projloader);

            Dictionary<string, AccountAndContacts> acctContactTree = BuildAccountContactsPairsTree(entityElems);
            LoadAccountsAndContactsAndConnections(projId, acctContactTree, acctLoader, contLoader, connLoader);

            return;
        }

        /// <summary>
        /// Load to ktc_project, ktc_project_type_assigned and ktc_project_stage_assigned
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public Guid LoadProjectRelatedEntities(List<XElement> elements, ProjectLoader projLoader)
        {
            Guid projectId = Guid.Empty;
            XElement xProject = elements.Find(x => "ktc_project" == x.Name.LocalName);
            if (xProject != null)
            {
                projectId = projLoader.LoadEntity(xProject);
            }

            IEnumerable<XElement> projTypesElements = GetProjTypeElements(elements);
            ProjectTypeLoader ptldr = new ProjectTypeLoader(_serviceProxy, _assigner);
            ptldr.LoadEntities(projTypesElements, projectId);
            /*
            ProjectStageLoader psldr = new ProjectStageLoader(_serviceProxy);
            psldr.LoadEntities(GetProjStageElements(elements), projectId);
            */
            return projectId;
        }

        private IEnumerable<XElement> GetProjTypeElements(List<XElement> inElements)
        {
            IEnumerable<XElement> projElements =
                            from el in inElements
                            where el.Name.ToString() == "ktc_project_type_assigned"
                            select el;
            return projElements;
        }
   
        public Dictionary<string, AccountAndContacts> BuildAccountContactsPairsTree(List<XElement> elements)
        {
            Dictionary<string, AccountAndContacts> acctDictionary = new Dictionary<string, AccountAndContacts>();
            IEnumerable<XElement> allAcctElements =
                from el in elements
                where el.Name.ToString() == "account"
                select el;

            List<XElement> allContElements =
                (from el in elements
                where el.Name.ToString() == "contact"
                select el).ToList();

            List<XElement> allConnElements =
                (from el in elements
                where el.Name.ToString() == "connection"
                select el).ToList();

            //foreach of them find the matching contacts/connections elements, by acct_Ktc_Data_Source_Id
            //and add them to the dictionary items
            foreach (XElement acctEntity in allAcctElements)
            {
                string matchId = acctEntity.Element("ktc_External_Data_Source_ID").Value;
                if (acctDictionary.ContainsKey(matchId))
                    continue;

                List<XElement> contactlist = new List<XElement>();
                List<XElement> connectionlist = new List<XElement>();
                for(int i = 0; i < allContElements.Count; i++)
                {
                    if (allContElements[i].Element("account_ktc_External_Data_Source_ID").Value == matchId)
                    {
                        contactlist.Add(allContElements[i]);
                        connectionlist.Add(allConnElements[i]);
                    }
                }
                acctDictionary.Add(matchId, new AccountAndContacts(acctEntity, contactlist, connectionlist));
            }
            return acctDictionary;

        }

        public void LoadAccountsAndContactsAndConnections(Guid projectId, Dictionary<string, AccountAndContacts> dict, AccountLoader acctLdr, 
            ContactLoader cntctLdr, ConnectionLoader connLdr)
        {

            //foreach of them call LoadEntity
            foreach (string key in dict.Keys)
            {
                XElement acctElem = ((AccountAndContacts)dict[key]).AccountElement;
                Guid accountId = acctLdr.LoadEntity(acctElem);

                AccountAndContacts acctContactsConnections =  (AccountAndContacts)dict[key];
                for (int contactIndex = 0; contactIndex < acctContactsConnections.ContactElements.Count; contactIndex++)
                {
                    //if first and last name are present, then process contact
                    //and create connection between project and contact
                    XElement contEl = acctContactsConnections.ContactElements[contactIndex];
                    Guid connectionId = Guid.Empty;
                    if (contEl.Element("firstname") != null && contEl.Element("firstname").Value != String.Empty &&
                        contEl.Element("lastname") != null && contEl.Element("lastname").Value != String.Empty)
                    {
                        contEl.Add(new XAttribute("accountId", accountId ));
                        Guid contactId = cntctLdr.LoadEntity(contEl);
                        connectionId = PrepareAndLoadConnection(connLdr, acctContactsConnections.ConnectionElements[contactIndex], projectId, contactId, "contact");
                    }
                    else
                    {
                        //create connection between project and account
                        connectionId = PrepareAndLoadConnection(connLdr, acctContactsConnections.ConnectionElements[contactIndex], projectId, accountId, "account");
                    }
                }
            }
            return;

        }

        public Guid PrepareAndLoadConnection(ConnectionLoader connLoader, XElement connElement, Guid record1Id, Guid record2Id, string record2type)
        {
            connElement.Add(new XAttribute("record1id", record1Id));
            connElement.Add(new XAttribute("record2id", record2Id));
            if (connElement.Element("record2typename") != null)
                connElement.Element("record2typename").Value = record2type;
            else
                connElement.Add(new XElement("record2typename", record2type));

            Guid connectionId = connLoader.LoadEntity(connElement);
            return connectionId;
        }
    }
}
