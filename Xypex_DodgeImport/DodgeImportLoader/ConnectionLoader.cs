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
    #region ConnectionRoles
    public static class ConnectionRoles
    {
        //dictionary of connection roles Ids retrieved from CRM where the key is represented [category].[name]
        private static Dictionary<string, Guid> _connectionRolesDict;

        /// <summary>
        /// Retrieve the connection roles for the specified categories and 
        /// build a static dictionary that will be later used to get connection role ids
        /// </summary>
        /// <param name="categories"></param>
        public static void RetrieveConnectionRolesOfInterest(List<string> categories, OrganizationServiceProxy service)
        {
            if (_connectionRolesDict != null && _connectionRolesDict.Count > 0)
                return;
            _connectionRolesDict = new Dictionary<string, Guid>();
            ColumnSet colset = new ColumnSet(new string[] { "category", "name", "connectionroleid" });

            int[] categOSValues = GetCategoryOptionSetValues(categories);
            ConditionExpression valuesCond = new ConditionExpression("category", ConditionOperator.In);
            for (int i = 0; i < categOSValues.Length; i++)
                valuesCond.Values.Add(categOSValues[i]);

            QueryExpression findroles = new QueryExpression
            {
                EntityName = ConnectionRole.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions = 
                    {
                        
                        new ConditionExpression 
                        {
                            AttributeName = "statecode",
                            Operator = ConditionOperator.Equal,
                            Values = {(int) ConnectionRoleState.Active}
                        }, 
                        valuesCond                        
                    }
                }
            };
            try
            {
                DataCollection<Entity> connRoleEntities = service.RetrieveMultiple(findroles).Entities;

                foreach (Entity connRole in connRoleEntities)
                {
                    _connectionRolesDict.Add(((ConnectionRole)connRole).Category.Value.ToString() + "." + ((ConnectionRole)connRole).Name,
                                            ((ConnectionRole)connRole).Id);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Dictionary of connection roles Ids retrieved from CRM where the key is represented [category].[name]
        /// </summary>
        public static Dictionary<string, Guid> ConnectionRolesDictionary
        {
            get
            {
                return _connectionRolesDict;
            }
        }
        
        private static int[] GetCategoryOptionSetValues(List<string> categnames)
        {
            List<int> categValues = new List<int>();
            int[] retVals;

            foreach (string name in categnames)
            {
                int optVal;
                switch (name)
                {
                    case "Stakeholder":
                        optVal = (int)connectionrole_category.Stakeholder;
                        break;
                    case "Project Bidder":
                        optVal = (int)connectionrole_category.ProjectBidder;
                        break;
                    default:
                        optVal = (int)connectionrole_category.Business;
                        break;
                }
                categValues.Add(optVal);
            }
            retVals = categValues.ToArray();
            return retVals;
        }

    }
    #endregion

    public class ConnectionLoader
    {        
        IOrganizationService service;
        UserOrTeamAssignment _assigner;
        XElement _xloadAttributes;
        Dictionary<string, Guid> loadAttributes;

        Connection _thisConnection;
        bool _isPrimary;
        string _record1typename;
        string _record2typename;
        const string defaultRoleCategoryId = "1000";

        public ConnectionLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            _isPrimary = false;
        }

        public ConnectionLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner, XElement connectionAttributes)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            _xloadAttributes = connectionAttributes;
            _isPrimary = false;
            PrepareLoadAttributes();
        }
        
        public Dictionary<string, Guid> LoadAttributes
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

        public bool IsPrimary
        {
            get
            {
                return _isPrimary;
            }
            set
            {
                _isPrimary = value;
            }
        }

        
        /// <summary>
        /// Populate Guids in LoadAttributes based on the XElement _xloadAttributes child node values
        /// </summary>
        /// <remarks>for now assume that record id's are passed in through _xloadAttributes
        /// and no lookup is necessary to be performed here
        /// </remarks>
        public void PrepareLoadAttributes()
        {
            _record1typename = _xloadAttributes.Element("record1typename").Value;
            _record2typename = _xloadAttributes.Element("record2typename").Value;

            if (LoadAttributes == null)
            {
                LoadAttributes = new Dictionary<string, Guid>();
            }
            else
            {
                LoadAttributes.Clear();
            }
            //get role ids from node elements
            if (_xloadAttributes.Element("record1roleid") != null)
            {
                string categoryId = defaultRoleCategoryId;
                if (_xloadAttributes.Element("record1rolecategoryid") != null)
                    categoryId = _xloadAttributes.Element("record1rolecategoryid").Value;

                LoadAttributes.Add("Record1Roleid", ConnectionRoles.ConnectionRolesDictionary[categoryId + "." + _xloadAttributes.Element("Record1RoleId").Value]);
            }

            if (_xloadAttributes.Element("record2roleid") != null)
            {   /*
                string categoryId = defaultRoleCategoryId;
                if (_xloadAttributes.Element("record2rolecategoryid") != null)
                    categoryId = _xloadAttributes.Element("record2rolecategoryid").Value;

                LoadAttributes.Add("Record2RoleId", ConnectionRoles.ConnectionRolesDictionary[categoryId + "." + _xloadAttributes.Element("Record2RoleId").Value]);
                 * */
                //record2roleid is provided as Guid in the input XElement so no need to look up any dictionary
                LoadAttributes.Add("Record2RoleId", new Guid(_xloadAttributes.Element("record2roleid").Value));
            }

            //if one role is missing, populate it with the matching role
            if (LoadAttributes.Keys.Contains("Record2RoleId") && !(LoadAttributes.Keys.Contains("Record1RoleId")))
                LoadAttributes.Add("Record1RoleId", loadAttributes["Record2RoleId"]);
            if (LoadAttributes.Keys.Contains("Record1RoleId") && !(LoadAttributes.Keys.Contains("Record2RoleId")))
                LoadAttributes.Add("Record2RoleId", loadAttributes["Record1RoleId"]);

            //get record ids, assumed to be found in the node attributes
            if (_xloadAttributes.Attribute("record1id") != null)
                LoadAttributes.Add("Record1Id", new Guid(_xloadAttributes.Attribute("record1id").Value));
            if (_xloadAttributes.Attribute("record2id") != null)
                LoadAttributes.Add("Record2Id", new Guid(_xloadAttributes.Attribute("record2id").Value));

        }
        
        public bool CreateConnectionRecord(out Guid connectionId)
        {
            bool created = false;
            Connection newConnection = InitializeConnection();
            try
            {
                connectionId = service.Create(newConnection);
                if (connectionId != Guid.Empty)
                    created = true;
            }
            catch
            {
                throw;
            }

            return created;
       }

        private Connection InitializeConnection()
        {
            Connection conn = new Connection();

            if (LoadAttributes.ContainsKey("Record1Id"))
                conn.Record1Id = new EntityReference(GetEntityLogicalName(_record1typename), LoadAttributes["Record1Id"]);
            if (LoadAttributes.ContainsKey("Record2Id"))
                conn.Record2Id = new EntityReference(GetEntityLogicalName(_record2typename), LoadAttributes["Record2Id"]);

            if (LoadAttributes.ContainsKey("Record2RoleId"))
                conn.Record2RoleId = new EntityReference(ConnectionRole.EntityLogicalName, LoadAttributes["Record2RoleId"]);
            if (LoadAttributes.ContainsKey("Record1RoleId"))
                conn.Record1RoleId = new EntityReference(ConnectionRole.EntityLogicalName, LoadAttributes["Record1RoleId"]);

 
            conn.ktc_Primary_Connection = IsPrimary;
 

            return conn;
        }

        public Connection FindConnection(Guid record1Id, Guid record2Id, Guid record2RoleId)
        {
            ColumnSet colset = new ColumnSet(new string[] { "connectionid", "record1id", "record1objecttypecode", "record1roleid", "record2id", "record2objecttypecode", "record2roleid", "relatedconnectionid", "ismaster" });
            QueryExpression query = new QueryExpression
            {
                EntityName = Connection.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "record1id",
                            Operator = ConditionOperator.Equal,
                            Values = {record1Id}
                        },
                        new ConditionExpression 
                        {
                            AttributeName = "record2id",
                            Operator = ConditionOperator.Equal,
                            Values = {record2Id}
                        },
                        new ConditionExpression
                        {
                            AttributeName = "record2roleid",
                            Operator = ConditionOperator.Equal,
                            Values = {record2RoleId}
                        },
                        new ConditionExpression
                        {
                            AttributeName = "ismaster",
                            Operator = ConditionOperator.Equal,
                            Values = {true}
                        }
                    }
                }
            };
            DataCollection<Entity> connections = service.RetrieveMultiple(query).Entities;
            if (connections != null && connections.Count > 0)
                _thisConnection = (Connection)connections[0];

            return _thisConnection;
        }

        public void UpdateRecord()
        {
            throw new Exception("Not implemented!");
        }

        public string GetEntityLogicalName(string name)
        {
            switch (name)
            {
                case "account":
                case "Account":
                    return Account.EntityLogicalName;
                case "contact":
                    return Contact.EntityLogicalName;
                case "ktc_project":
                case "project":
                case "Project":
                    return ktc_project.EntityLogicalName;
                default:
                    return name;
            }
        }

        public int GetEntityTypeCode(string name)
        {
            switch (name)
            {
                case "account":
                case "Account":
                    return Account.EntityTypeCode;
                case "contact":
                    return Contact.EntityTypeCode;
                case "project":
                case "Project":
                    return ktc_project.EntityTypeCode;
                default:
                    return -1;
            }
        }

        public Guid LoadEntity(XElement inEntity)
        {
            Guid connectionId = Guid.Empty;
            _xloadAttributes = inEntity;
            PrepareLoadAttributes();

            Connection connection = FindConnection(LoadAttributes["Record1Id"],
                                                    LoadAttributes["Record2Id"],
                                                    LoadAttributes["Record2RoleId"]);
            
            if (connection == null)
            {
                CreateConnectionRecord(out connectionId);
            }

            return connectionId;
        }
    }
}
