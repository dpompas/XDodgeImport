using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security;
using DodgeImportLoader;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using XypexCRM;
using DodgeXMLParser;

namespace Xypex_DodgeImportConsoleApp
{
    class ProgramStart
    {
        static void Main(string[] args)
        {
            ProgramStart consoleApp = new ProgramStart();

            Console.WriteLine("Hello. We can start....");
            Console.WriteLine();

            try
            {
               // OrganizationServiceProxy serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
                //serviceProxy.EnableProxyTypes();

                //consoleApp.WhoAmI(serviceProxy);

                //Account acct = consoleApp.FindAccount(serviceProxy);
                //Console.WriteLine(acct.ktc_External_Data_Source_Match_ID + " AccountId: " + acct.AccountId.ToString() +
                //    " Name: " + acct.Name);
                consoleApp.RunParser();
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault> e) { XypexLogger.HandleException(e); }
            catch (TimeoutException e) { XypexLogger.HandleException(e); }
            catch (SecurityTokenValidationException e) { XypexLogger.HandleException(e); }
            catch (ExpiredSecurityTokenException e) { XypexLogger.HandleException(e); }
            catch (MessageSecurityException e) { XypexLogger.HandleException(e); }
            catch (SecurityNegotiationException e) { XypexLogger.HandleException(e); }
            catch (SecurityAccessDeniedException e) { XypexLogger.HandleException(e); }
            catch (Exception e) { XypexLogger.HandleException(e); }

            Console.WriteLine("press ENTER to end...");
            Console.ReadLine();
        }

        private void WhoAmI(OrganizationServiceProxy proxy)
        {
            // Here we will use the interface instead of the proxy object.
            IOrganizationService service = (IOrganizationService)proxy;

            // Display information about the logged on user.
            Guid userid = ((WhoAmIResponse)service.Execute(new WhoAmIRequest())).UserId;
            SystemUser systemUser = (SystemUser)service.Retrieve("systemuser", userid,
                new ColumnSet(new string[] { "firstname", "lastname" }));
            Console.WriteLine("Logged on user is {0} {1}.", systemUser.FirstName, systemUser.LastName);

            // Retrieve the version of Microsoft Dynamics CRM.
            RetrieveVersionRequest versionRequest = new RetrieveVersionRequest();
            RetrieveVersionResponse versionResponse =
                (RetrieveVersionResponse)service.Execute(versionRequest);
            Console.WriteLine("Microsoft Dynamics CRM version {0}.", versionResponse.Version);
        }

        public void Run(OrganizationServiceProxy svcProxy)
        {
            //do something
            Dictionary<string,string> accountAttributes = new Dictionary<string,string>();
            accountAttributes.Add("name", "Sixth Coffee");
            accountAttributes.Add("address1_line1", "5 Hana Road");
            accountAttributes.Add("address1_city", "Hana");
            UserOrTeamAssignment ua = new UserOrTeamAssignment(svcProxy);
                                    
            AccountLoader acctLoader = new AccountLoader(svcProxy, ua, accountAttributes);
            try
            {
                Guid accountId;
                acctLoader.CreateRecord(out accountId);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void RunParser()
        {
            DodgeFileParser fileParser = new DodgeFileParser();
            //fileParser.ParseFile("C:\\Users\\Diana\\Documents\\Xypex\\Dodge XML\\916640966540205_Xypex_on_plans_01232015.xml");
            try
            {
                List<string> files = fileParser.CheckFileLocation();
                foreach (string fileName in files)
                {
                    fileParser.ParseFile(fileName);
                }
            }
            catch (Exception e) { XypexLogger.HandleException(e); }
        }

        private Account FindAccount(OrganizationServiceProxy serviceProxy)
        {
            UserOrTeamAssignment ua = new UserOrTeamAssignment(serviceProxy);
            AccountLoader testAccount = new AccountLoader(serviceProxy, ua);
            Account foundAcct = testAccount.PrimaryMatch("McGraw-Hill Constructi201300423713");
            return foundAcct;
        }
    }
}
