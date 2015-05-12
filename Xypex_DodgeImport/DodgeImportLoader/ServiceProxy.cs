using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Samples;


namespace DodgeImportLoader
{
    public static class ServiceProxy
    {
        public static OrganizationServiceProxy serviceProxy;

        public static OrganizationServiceProxy GetXypexOrganizationServiceProxy()
        {
                // Obtain the target organization's web address and client logon credentials
                // from the user by using a helper class.
                ServerConnection serverConnect = new ServerConnection();
                ServerConnection.Configuration config = serverConnect.GetServerConfiguration();

                // Establish an authenticated connection to the Organization web service. 
                serviceProxy = new OrganizationServiceProxy(config.OrganizationUri, config.HomeRealmUri,
                                                            config.Credentials, config.DeviceCredentials);
                return serviceProxy;
        }
   }
}
