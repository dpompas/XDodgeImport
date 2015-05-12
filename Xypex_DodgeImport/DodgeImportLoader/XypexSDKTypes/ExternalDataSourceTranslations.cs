using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using XypexCRM;

namespace DodgeImportLoader.XypexSDKTypes
{
    public class ExternalDataSourceTranslation
    {
        protected Dictionary<string, object/*Guid*/> _idLookupDictionary;
        protected Dictionary<string, object /*string*/> _codeLookupDictionary;
        protected IOrganizationService service;

        public ExternalDataSourceTranslation(OrganizationServiceProxy serviceProxy)
        {
            service = (IOrganizationService)serviceProxy;
        }


        public virtual Dictionary<string, object/*Guid*/> IdLookupDictionary
        {
            get
            {
                return _idLookupDictionary;
            }
            set
            {
                _idLookupDictionary = value;
            }
        }

        public virtual Dictionary<string, object/*string*/> codeLookupDictionary
        {
        
            get
            {
                return _codeLookupDictionary;
            }
            set
            {
                _codeLookupDictionary = value;
            }
        }

        public virtual Dictionary<string, object /*Guid*/> RetrieveIdDictionary()
        {
            RetrieveFromCRM();
            return IdLookupDictionary;
        }

        public virtual Dictionary<string, object/*string*/> RetrieveCodeDictionary()
        {
            RetrieveFromCRM();
            return codeLookupDictionary;
        }

        protected virtual void RetrieveFromCRM()
        {
        }
    }

    public class ProjectTypeTranslation : ExternalDataSourceTranslation
    {
        public ProjectTypeTranslation(OrganizationServiceProxy serviceProxy)
            : base(serviceProxy)
        {
        }

        protected override void RetrieveFromCRM()
        {
            ColumnSet colset = new ColumnSet(new string[] {"ktc_external_data_source_project_type", 
                                                            "ktc_project_type_id"});
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project_type_translation.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "ktc_external_data_source_name",
                            Operator = ConditionOperator.Equal,
                            Values = {"McGraw-Hill Construction Dodge"}
                        }
                    }
                }
            };

            _idLookupDictionary = new Dictionary<string, object/*Guid*/>();
            _codeLookupDictionary = null;

            DataCollection<Entity> records = service.RetrieveMultiple(query).Entities;
            ktc_project_type_translation record;
            foreach(Entity rec in records)
            {
                record = (ktc_project_type_translation) rec;
                string key = record.ktc_external_data_source_project_type;
                Guid value = record.ktc_project_type_id.Id;
                if (_idLookupDictionary.ContainsKey(key))
                    continue;
                _idLookupDictionary.Add(key, value);
            }


        }

    }

    public class ProjectStageTranslation : ExternalDataSourceTranslation
    {
        public ProjectStageTranslation(OrganizationServiceProxy serviceProxy)
            : base(serviceProxy)
        {
        }

        protected override void RetrieveFromCRM()
        {
            ColumnSet colset = new ColumnSet(new string[] {"ktc_external_data_source_project_stage", 
                                                            "ktc_project_stage_id"});
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project_stage_translation.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "ktc_external_data_source_name",
                            Operator = ConditionOperator.Equal,
                            Values = {"McGraw-Hill Construction Dodge"}
                        }
                    }
                }
            };

            _idLookupDictionary = new Dictionary<string, object /*Guid*/>();
            _codeLookupDictionary = null;

            DataCollection<Entity> records = service.RetrieveMultiple(query).Entities;
            ktc_project_stage_translation record;
            foreach (Entity rec in records)
            {
                record = (ktc_project_stage_translation)rec;
                string key = record.ktc_external_data_source_project_stage;
                Guid value = record.ktc_project_stage_id.Id;
                if (_idLookupDictionary.ContainsKey(key))
                    continue;
                _idLookupDictionary.Add(key, value);
            }
        }
    }

    public class ConnectionRoleTranslation : ExternalDataSourceTranslation
    {
        public ConnectionRoleTranslation(OrganizationServiceProxy serviceProxy)
            : base(serviceProxy)
        {
        }

        protected override void RetrieveFromCRM()
        {
            ColumnSet colset = new ColumnSet(new string[] {"ktc_external_data_source_contact_type", 
                                                            "ktc_connection_role_name"});
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_connection_role_translation.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "ktc_external_data_source_name",
                            Operator = ConditionOperator.Equal,
                            Values = {"McGraw-Hill Construction Dodge"}
                        }
                    }
                }
            };

            _idLookupDictionary = null;
            _codeLookupDictionary = new Dictionary<string, object /*string*/>();

            DataCollection<Entity> records = service.RetrieveMultiple(query).Entities;
            ktc_connection_role_translation record;
            foreach (Entity rec in records)
            {
                record = (ktc_connection_role_translation)rec;
                string key = record.ktc_external_data_source_contact_type;
                string value = record.ktc_connection_role_name;
                if (_codeLookupDictionary.ContainsKey(key))
                    continue;
                _codeLookupDictionary.Add(key, value);
            }
        }
    }
}
