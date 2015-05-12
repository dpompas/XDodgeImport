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
    class ProjectTypeLoader
    {
        IOrganizationService service;
        UserOrTeamAssignment _assigner;
        Guid _ktc_project_id;
        DataCollection<Entity> _CRM_ktc_project_types;
        Dictionary<Guid, ktc_project_type_assigned> _input_project_types;


        public ProjectTypeLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
        }

        public ProjectTypeLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner, IEnumerable<XElement> projectTypesElems, Guid projectId)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            _ktc_project_id = projectId;
            InitializeInputProjectTypes(projectTypesElems, projectId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputElements"></param>
        /// <param name="projId"></param>
        /// <returns></returns>
        public Dictionary<Guid, ktc_project_type_assigned> InitializeInputProjectTypes(IEnumerable<XElement> inputElements, Guid projId)
        {
            _input_project_types = new Dictionary<Guid, ktc_project_type_assigned>();
            
            foreach (XElement e in inputElements)
            {
                ktc_project_type_assigned pta = new ktc_project_type_assigned();
                pta.ktc_project_id = new EntityReference(ktc_project_type_assigned.EntityLogicalName, projId);
                if (e.Element("ktc_Project_Type_Id") == null || e.Element("ktc_Project_Type_Id").Value == String.Empty) 
                    continue;
                Guid proj_type_id;
                try
                {
                    proj_type_id = new Guid(e.Element("ktc_Project_Type_Id").Value);
                }
                catch (Exception exc)
                {
                    XypexLogger.HandleException(exc);
                    continue;
                }
                pta.ktc_project_type_id = new EntityReference(ktc_project_type.EntityLogicalName, proj_type_id);
                if (e.Element("ktc_Primary_Project_Type") != null)
                    pta.ktc_Primary_Indicator = e.Element("ktc_Primary_Project_Type").Value == "Y" ? true : false;
           
                _input_project_types.Add(proj_type_id, pta);
            }
            return _input_project_types;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public DataCollection<Entity> RetrieveProjectTypesAssignedForProject(Guid projectId)
        {
            ColumnSet colset = new ColumnSet(new string[] {"ktc_project_type_assignedid",
                                                            "ktc_project_id",
                                                            "ktc_project_type_id",
                                                            "ktc_primary_indicator"});
            QueryExpression query = new QueryExpression
            {
                EntityName = ktc_project_type_assigned.EntityLogicalName,
                ColumnSet = colset,
                Criteria = new FilterExpression
                {
                    Conditions = 
                    {
                        new ConditionExpression
                        {
                            AttributeName = "ktc_project_id",
                            Operator = ConditionOperator.Equal,
                            Values = {projectId}
                        }
                    }

                }
            };

            _CRM_ktc_project_types = service.RetrieveMultiple(query).Entities;
            return _CRM_ktc_project_types;
        }

        public void LoadEntities(IEnumerable<XElement> projectTypesElems, Guid projectId)
        {
            InitializeInputProjectTypes(projectTypesElems, projectId);
            RetrieveProjectTypesAssignedForProject(projectId);

            if (_CRM_ktc_project_types != null && _CRM_ktc_project_types.Count > 0)
            {
                UpdateProjectTypes();
            }
            else
            {
                AddProjectTypes();
            }
        }

        public void AddProjectTypes()
        {
            try
            {
                foreach (ktc_project_type_assigned kpta in _input_project_types.Values)
                {
                    Guid project_type_assigned_id = service.Create(kpta);
                }
            }
            catch
            {
                throw;
            }
        }

        public void UpdateProjectTypes()
        {
            try
            {
                //first loop through existing project types in CRM and delete obsolete records or update primary indicator
                foreach (Entity e in _CRM_ktc_project_types)
                {
                    ktc_project_type_assigned crmpta = (ktc_project_type_assigned)e;

                    if (_input_project_types.ContainsKey(crmpta.ktc_project_type_id.Id))
                    {
                        if (crmpta.ktc_Primary_Indicator != _input_project_types[crmpta.ktc_project_type_id.Id].ktc_Primary_Indicator)
                        {
                            crmpta.ktc_Primary_Indicator = _input_project_types[crmpta.ktc_project_type_id.Id].ktc_Primary_Indicator;
                            service.Update(crmpta);
                        }
                        //remove from input dictionary as it's processed
                        _input_project_types.Remove(crmpta.ktc_project_type_id.Id);
                    }
                    else
                    {
                        //delete no longer relevant type
                        service.Delete(ktc_project_type_assigned.EntityLogicalName, crmpta.Id);
                    }
                }
                //then add the new types, if anything left in the dictionary
                {
                    if (_input_project_types.Count > 0)
                    {
                        foreach (ktc_project_type_assigned kpta in _input_project_types.Values)
                        {
                            Guid project_type_assigned_id = service.Create(kpta);
                        }
                    }

                }
            }
            catch
            {
                throw;
            }
        }
    }
}
