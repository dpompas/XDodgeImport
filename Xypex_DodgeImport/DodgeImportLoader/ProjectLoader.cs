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
    public class ProjectLoader
    {
        IOrganizationService service;
        XElement _xloadAttributes;
        ktc_project _project;
        UserOrTeamAssignment _assigner;


        public ProjectLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
        }
    
        public ProjectLoader(OrganizationServiceProxy serviceProxy, UserOrTeamAssignment assigner, XElement projectAttributes)
        {
            service = (IOrganizationService)serviceProxy;
            _assigner = assigner;
            _xloadAttributes = projectAttributes;
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

        public Guid LoadEntity(XElement inEntity)
        {
            Guid projectId = Guid.Empty;

            LoadAttributes = inEntity;

            ktc_project crmProj = PrimaryMatch(LoadAttributes.Element("ktc_External_Data_Source_Match_ID").Value);
            if (crmProj == null)
            {
                Dictionary<string, string> secMatchAttribs = GetSecondaryMatchAttributes(inEntity);
                crmProj = SecondaryMatch(secMatchAttribs);
            }

            if (crmProj == null)
            {
                CreateRecord(out projectId);
            }
            else
            {
                projectId = crmProj.Id;
                UpdateRecord();
            }

            return projectId;
        }

        public ktc_project PrimaryMatch(string primaryIdentifier)
        {
            ColumnSet cols = new ColumnSet(new string[] { "ktc_projectid", "ktc_project_name", "owninguser", "owningteam" });
            
            //identify and return record for which ktc_External_Data_Source_Match_ID has the value provided in primaryIdentifier
            QueryExpression findByKtcMatchId = new QueryExpression 
            {
                EntityName = ktc_project.EntityLogicalName,
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
                DataCollection<Entity> projects = service.RetrieveMultiple(findByKtcMatchId).Entities;
                if (projects != null && projects.Count > 0)
                {
                    _project = (ktc_project)projects[0];
                    return _project;
                }
                else
                    return null;
            }
            catch {
                throw;
            }
        }

        public ktc_project SecondaryMatch(Dictionary<string, string> matchingAttributes)
        {
            ColumnSet colset = new ColumnSet(new string[] { "ktc_projectid", "ktc_project_name", "ktc_external_data_source_id", "owninguser", "owningteam" });
            FilterExpression fe = new FilterExpression();
            foreach (string attrib in matchingAttributes.Keys)
            {
                if (matchingAttributes[attrib] != String.Empty)
                    fe.Conditions.Add(new ConditionExpression(attrib, ConditionOperator.Equal, matchingAttributes[attrib]));
            }
            fe.Conditions.Add(new ConditionExpression("ktc_external_data_source_name", ConditionOperator.NotEqual, "McGraw-Hill Construction Dodge"));
            QueryExpression qe = new QueryExpression(ktc_project.EntityLogicalName);
            qe.ColumnSet = colset;
            qe.Criteria = fe;

            try
            {
                DataCollection<Entity> projects = service.RetrieveMultiple(qe).Entities;
                if (projects != null && projects.Count > 0)
                {
                    _project = (ktc_project)projects[0];
                    return _project;
                }
                else
                    return null;
            }
            catch
            {
                throw;
            }

        }

        public bool CreateRecord(out Guid projectId)
        {
            bool created = false;
            ktc_project newProject = InitializeProject(null);
            try
            {
                projectId = service.Create(newProject);
                if (projectId != Guid.Empty)
                    created = true;

                created = _assigner.AssignEntityToTeam(UserOrTeamAssignment.ProjectAdminTeamId(service),
                                    new EntityReference(ktc_project.EntityLogicalName, projectId));
            }
            catch
            {
                throw;
            }

            return created;
       }

        /// <summary>
        /// Update the Project record found by a previous match with the values from LoadAttributes
        /// </summary>
        /// <returns></returns>
        public bool UpdateRecord()
        {
            bool updated = false;
            InitializeProject(_project);
            try
            {
                service.Update(_project);
                updated = true;

                if ((_project.OwningTeam == null ||
                    _project.OwningTeam.Id != UserOrTeamAssignment.ProjectAdminTeamId(service).Id) &&
                    (_project.OwningUser != null && 
                    _project.OwningUser.Id == UserOrTeamAssignment.IntegrationUserId(service).Id) 
                     )
                {
                    updated = _assigner.AssignEntityToTeam(UserOrTeamAssignment.ProjectAdminTeamId(service),
                                    new EntityReference(ktc_project.EntityLogicalName, _project.Id));
                }
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

        private ktc_project InitializeProject(ktc_project thisProject)
        {
            if (thisProject == null)
            {
                thisProject = new ktc_project();
            }
            //ktc identifying fields
            //set the source date to the time the project is initialized for now
            DateTime sourceDate;
            try
            {
                if (LoadAttributes.Element("ktc_External_Data_Source_Date") != null
                    && LoadAttributes.Element("ktc_External_Data_Source_Date").Value.Length == 8)
                {
                    String srcDateVal = LoadAttributes.Element("ktc_External_Data_Source_Date").Value;
                    sourceDate = DodgeConvert.ToDate(srcDateVal);
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

            thisProject.ktc_External_Data_Source_Date = sourceDate;
           
            if (LoadAttributes.Element("ktc_External_Data_Source_ID") != null)
                thisProject.ktc_External_Data_Source_ID = LoadAttributes.Element("ktc_External_Data_Source_ID").Value;
            if (LoadAttributes.Element("ktc_External_Data_Source_Name") != null)
                thisProject.ktc_External_Data_Source_Name = LoadAttributes.Element("ktc_External_Data_Source_Name").Value;

            if (LoadAttributes.Element("ktc_External_Data_Source_PrjProfile_URL") != null)
                thisProject.ktc_External_Data_Source_PrjProfile_URL = LoadAttributes.Element("ktc_External_Data_Source_PrjProfile_URL").Value;
            
            if (LoadAttributes.Element("ktc_Project_Name") != null)
                thisProject.ktc_project_name = LoadAttributes.Element("ktc_Project_Name").Value;
            if (LoadAttributes.Element("ktc_Project_address1_line1") != null)
                thisProject.ktc_Project_address1_line1 = LoadAttributes.Element("ktc_Project_address1_line1").Value;
            if (LoadAttributes.Element("ktc_Project_address1_line2") != null)
                thisProject.ktc_Project_address1_line2 = LoadAttributes.Element("ktc_Project_address1_line2").Value;
            if (LoadAttributes.Element("ktc_Project_address1_line3") != null)
                thisProject.ktc_Project_address1_line3 = LoadAttributes.Element("ktc_Project_address1_line3").Value;
            if (LoadAttributes.Element("ktc_Project_address1_county") != null)
                thisProject.ktc_Project_address1_county = LoadAttributes.Element("ktc_Project_address1_county").Value;
            if (LoadAttributes.Element("ktc_Project_address1_city") != null)
                thisProject.ktc_Project_address1_city = LoadAttributes.Element("ktc_Project_address1_city").Value;
            if (LoadAttributes.Element("ktc_Project_address1_stateorprovince") != null)
                thisProject.ktc_Project_address1_stateorprovince = LoadAttributes.Element("ktc_Project_address1_stateorprovince").Value;
            if (LoadAttributes.Element("ktc_Project_address1_Postalcode") != null)
                thisProject.ktc_Project_address1_Postalcode = LoadAttributes.Element("ktc_Project_address1_Postalcode").Value;
            if (LoadAttributes.Element("ktc_Project_address1_country") != null)
                thisProject.ktc_Project_address1_country = LoadAttributes.Element("ktc_Project_address1_country").Value;
            
            //Transaction_Currency_Id - field deleted
            if (LoadAttributes.Element("TransactionCurrencyId") != null)
                thisProject.TransactionCurrencyId = new EntityReference(TransactionCurrency.EntityLogicalName,
                                        new Guid(LoadAttributes.Element("TransactionCurrencyId").Value));
            
            if (LoadAttributes.Element("ktc_Project_Estimated_Valuation_High") != null)
                thisProject.ktc_project_estimated_valuation_high = new Money(Convert.ToDecimal((LoadAttributes.Element("ktc_Project_Estimated_Valuation_High").Value)));

            if (LoadAttributes.Element("ktc_Bid_Date") != null)
                thisProject.ktc_Bid_Date = DodgeConvert.ToDate(LoadAttributes.Element("ktc_Bid_Date").Value);

            if (LoadAttributes.Element("ktc_Target_Start_Date") != null)
                thisProject.ktc_Target_Start_Date = DodgeConvert.ToDate(LoadAttributes.Element("ktc_Target_Start_Date").Value);
            // TO DO- figure out what to do if optionset enum does not exist
            if (LoadAttributes.Element("ktc_Project_Owner_Class") != null)
            {
                int ownerClassOptionSetValue = DodgeConvert.GetKtcProjectOwnerClassEnum(LoadAttributes.Element("ktc_Project_Owner_Class").Value);
                if (ownerClassOptionSetValue != DodgeConvert.enumNotDefined)
                    thisProject.ktc_project_owner_class = new OptionSetValue(ownerClassOptionSetValue);
            }
            if (LoadAttributes.Element("ktc_Project_Stage_Primary") != null)
                thisProject.ktc_Project_Stage_Primary = new EntityReference(ktc_project_stage.EntityLogicalName, new Guid(LoadAttributes.Element("ktc_Project_Stage_Primary").Value));

            // Project_Notes
            if (LoadAttributes.Element("ktc_Project_Result_General_Comments") != null)
                thisProject.ktc_Project_Result_General_Comments = LoadAttributes.Element("ktc_Project_Result_General_Comments").Value;

            
            if (LoadAttributes.Element("ktc_External_Data_Source_Project_Bidder_List_URL") != null)
                            thisProject.ktc_External_Data_Source_Project_Bidder_List = LoadAttributes.Element("ktc_External_Data_Source_Project_Bidder_List_URL").Value;
            
            return thisProject;
        }

        public Dictionary<string, string> GetSecondaryMatchAttributes(XElement attribElems)
        {

            Dictionary<string, string> matchAttribs = new Dictionary<string, string>();

            var matchKeys = new HashSet<string> {"ktc_Project_address1_line1", "ktc_Project_address1_city",
                "ktc_Project_address1_stateorprovince", "ktc_Project_address1_country"};

            IEnumerable<XElement> matchElems = from e in attribElems.Elements()
                                               where matchKeys.Contains(e.Name.LocalName)
                                               select e;
                        
            matchAttribs = matchElems.ToDictionary(p => (p.Name.LocalName).ToLower(), p => p.Value);

            return matchAttribs;
        }

    }
}
