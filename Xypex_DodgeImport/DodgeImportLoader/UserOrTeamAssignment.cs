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

namespace DodgeImportLoader
{
    public class UserOrTeamAssignment
    {
        static EntityReference _projectAdminTeamId = null;
        static EntityReference _integrationUserId = null;
        const string _teamName = "Project Administration";
        const string _integrationUserName = "Xypex Integration";
        IOrganizationService _service;

        public UserOrTeamAssignment(OrganizationServiceProxy serviceProxy)
        {
            _service = (IOrganizationService)serviceProxy;
        }

        public static EntityReference ProjectAdminTeamId(IOrganizationService service)
        {
            if (_projectAdminTeamId != null)
                return _projectAdminTeamId;

            ColumnSet cols = new ColumnSet(new string[] {"name", "teamid"});
            QueryExpression qe = new QueryExpression
            {
                EntityName = Team.EntityLogicalName,
                ColumnSet = cols,
                Criteria = new FilterExpression()
            };
            qe.Criteria.Conditions.Add(new ConditionExpression("name", ConditionOperator.Equal, _teamName));

            DataCollection<Entity> teams = service.RetrieveMultiple(qe).Entities;
            if (teams != null && teams.Count > 0 )
            {
                _projectAdminTeamId = new EntityReference(Team.EntityLogicalName, ((Team)teams[0]).Id);
            }
            return _projectAdminTeamId;
        }

        public static EntityReference IntegrationUserId(IOrganizationService service)
        {
            if (_integrationUserId != null)
                return _integrationUserId;

            ColumnSet cols = new ColumnSet(new string[] { "fullname", "systemuserid" });
            QueryExpression qe = new QueryExpression
            {
                EntityName = SystemUser.EntityLogicalName,
                ColumnSet = cols,
                Criteria = new FilterExpression()
            };
            qe.Criteria.Conditions.Add(new ConditionExpression("fullname", ConditionOperator.Equal, _integrationUserName));

            DataCollection<Entity> users = service.RetrieveMultiple(qe).Entities;
            if (users != null && users.Count > 0)
            {
                _integrationUserId = new EntityReference(SystemUser.EntityLogicalName, ((SystemUser)users[0]).Id);
            }
            return _integrationUserId;
        }

        public bool AssignEntityToTeam(EntityReference assigneeTeam, EntityReference assignedRecord)
        {
            bool retval = false;
            AssignRequest arequest = null;

            try
            {
                arequest = new AssignRequest()
                {
                    Assignee = assigneeTeam,
                    Target = assignedRecord
                };

                _service.Execute(arequest);
                retval = true;
                return retval;
            }
            catch
            {
                throw;
            }
        }
    }
}
