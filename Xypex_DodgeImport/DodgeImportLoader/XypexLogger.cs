using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using XypexCRM;

namespace DodgeImportLoader
{
    #region EntityInformation
    public class EntityInformation
    {
        public string EntityName;
        public string EntityIdentificationName;
        public string ExternalDataSourceName;
        public string ExternalDataSourceId;
        public Guid EntityId;
        
        public EntityInformation (string logicalName, string entIdName, string externalDataSrcId)
        {
            EntityName = logicalName;
            EntityIdentificationName = entIdName;
            ExternalDataSourceId = externalDataSrcId;
            EntityId = Guid.Empty;
        }
        public EntityInformation(string logicalName, string entIdName, string externalDataSrcId, Guid entityId)
        {
            EntityName = logicalName;
            EntityIdentificationName = entIdName;
            ExternalDataSourceId = externalDataSrcId;
            EntityId = entityId;
        }
        public EntityInformation(string logicalName, string entIdName, string externalDataSrcId, string externalDataSrcName)
        {
            EntityName = logicalName;
            EntityIdentificationName = entIdName;
            ExternalDataSourceId = externalDataSrcId;
            ExternalDataSourceName = externalDataSrcName;
            EntityId = Guid.Empty;
        }
        public EntityInformation(string logicalName, string entIdName, string externalDataSrcId, string externalDataSrcName, Guid entityId)
        {
            EntityName = logicalName;
            EntityIdentificationName = entIdName;
            ExternalDataSourceId = externalDataSrcId;
            ExternalDataSourceName = externalDataSrcName;
            EntityId = entityId; 
        }
    }
    #endregion

    public class LogEntry
    {
        private DateTime logTime;
        public enum LogType
        {
            Information = 100000000,
            Warning,
            Error, 
            ProcessSummary
        }
        public enum LogLevel
        {
            Summary = 0,
            Detail = 1
        }
        public LogType LogEntryType;
        public LogLevel LogEntryLevel;
        public string FileName;
        public EntityInformation LogEntityInfo;
        public string ProcessName;
        public string ComponentName;
        public string LogOverviewText;
        public string LogDetailsText;

        public LogEntry(LogType logType, string overview, string description, string componName)
        {
            logTime = DateTime.Now;
            LogEntryType = logType;
            LogOverviewText = overview;
            LogDetailsText = description;
            ComponentName = componName;
        }

        public LogEntry(LogType logType, string fileName, EntityInformation entityInfo, string componName, string overview, string description)
        {
            logTime = DateTime.Now;
            LogEntryType = logType;
            FileName = fileName;
            LogEntityInfo = entityInfo;
            ComponentName = componName;
            LogOverviewText = overview;
            LogDetailsText = description;
        }

        public LogEntry(LogType logType, string fileName, string overview, string description, string componName)
        {
            LogEntryType = logType;
            FileName = fileName;
            LogOverviewText = overview;
            LogDetailsText = description;
            ComponentName = componName;
        }

    }

    public class LoggerException : Exception
    {
        public enum ExceptionType
        {
            CRMInaccessible = 0,
            EventLogInaccessible,
            Other
        }
        public ExceptionType LogExcType;

        public LoggerException(string message)
            : base(message)
        {
            LogExcType = ExceptionType.Other;
        }

        public LoggerException(ExceptionType type, string message) : base(message)
        {
            LogExcType = type;
        }
    }

    public class XypexLogger
    {
        public const string DodgeExtDataSrcName = "McGraw-Hill Construction Dodge";
        public const string DodgeImportProcessName = "Dodge Import";
        private static XypexLogger _loggerInstance;
        private LogEntry.LogLevel _systemLogLevelSetting;
        OrganizationServiceProxy _serviceProxy;

        private XypexLogger(LogEntry.LogLevel loggingLevel, OrganizationServiceProxy serviceProxy)
        {
            _systemLogLevelSetting = loggingLevel;
            _serviceProxy = serviceProxy;
        }

        public static XypexLogger CreateInstance()
        {
            OrganizationServiceProxy serviceProxy;
            try
            {
                serviceProxy = ServiceProxy.GetXypexOrganizationServiceProxy();
                serviceProxy.EnableProxyTypes();
            }
            catch
            {
                serviceProxy = null;
                //this will prompt logging to EventLog
            }

            LogEntry.LogLevel logLevel = XypexLogger.GetLogLevelSystemSettings();
            _loggerInstance = new XypexLogger(logLevel, serviceProxy);
            return _loggerInstance;
        }

        private static LogEntry.LogLevel GetLogLevelSystemSettings()
        {
            return LogEntry.LogLevel.Detail;
        }

        public static XypexLogger GetInstance()
        {
            if (_loggerInstance == null)
                _loggerInstance = CreateInstance();
            return _loggerInstance; 
        }

        public OrganizationServiceProxy Proxy
        {
            get
            {
                return _serviceProxy;
            }
        }

        public static void WriteLogEntryToCRM(LogEntry entry)
        {
            if (GetInstance().Proxy == null)
                throw new LoggerException(LoggerException.ExceptionType.CRMInaccessible, "Could not get a service proxy to CRM.");

            try
            {
                ktc_externaldatasourceimportlog CRMlog = new ktc_externaldatasourceimportlog();
                CRMlog.ktc_Log_Date_Time = DateTime.Now;
                CRMlog.ktc_process_name = entry.ProcessName == string.Empty ? DodgeImportProcessName : entry.ProcessName;
                CRMlog.ktc_Log_Type = new OptionSetValue((int) entry.LogEntryType);
                CRMlog.ktc_component_name = entry.ComponentName;
                CRMlog.ktc_file_name = entry.FileName;
                CRMlog.ktc_entity_name = entry.LogEntityInfo.EntityName;
                PopulateCRMlogEntityReference(CRMlog, entry);
                CRMlog.ktc_external_data_source_id = entry.LogEntityInfo.ExternalDataSourceId;
                CRMlog.ktc_external_data_source_name = (entry.LogEntityInfo.ExternalDataSourceName == string.Empty) ? DodgeExtDataSrcName : 
                                                        entry.LogEntityInfo.ExternalDataSourceName;
                CRMlog.ktc_Event_Log_Overview = entry.LogOverviewText;
                if (entry.LogEntityInfo.EntityIdentificationName != string.Empty &&
                    !entry.LogOverviewText.Contains(entry.LogEntityInfo.EntityIdentificationName))
                {
                    CRMlog.ktc_Event_Log_Overview += " for " + entry.LogEntityInfo.EntityIdentificationName;
                }
                CRMlog.ktc_event_log_details = entry.LogDetailsText;

                Guid CRMLogId = GetInstance().Proxy.Create(CRMlog);
            }
            catch
            {
                throw new LoggerException(LoggerException.ExceptionType.CRMInaccessible, "Could not log entry to CRM.");
            }
        }

        private static ktc_externaldatasourceimportlog PopulateCRMlogEntityReference(ktc_externaldatasourceimportlog CRMlog, LogEntry entry)
        {
            switch (entry.LogEntityInfo.EntityName)
            {
                case "account":
                case "Account":
                    CRMlog.ktc_account_id = new EntityReference(Account.EntityLogicalName, entry.LogEntityInfo.EntityId);
                    break;
                case "contact":
                case "Contact":
                    CRMlog.ktc_contact_id = new EntityReference(Contact.EntityLogicalName, entry.LogEntityInfo.EntityId);
                    break;
                case "opportunity":
                case "Opportunity":
                    CRMlog.ktc_opportunity_id = new EntityReference(Opportunity.EntityLogicalName, entry.LogEntityInfo.EntityId);
                    break;
                case "ktc_project":
                    CRMlog.ktc_project_id = new EntityReference(ktc_project.EntityLogicalName, entry.LogEntityInfo.EntityId);
                    break;
                default:
                    break;

            }
            return CRMlog;
        }

        public static void WriteLogEntryToEventLog(LogEntry entry)
        {
            throw new Exception("not implemented");
        }

        public static void WriteLogEntry(LogEntry logEntry)
        {
            try 
            {
                WriteLogEntryToCRM(logEntry);
            }
            catch (LoggerException le)
            {
                switch (le.LogExcType)
                {
                    case LoggerException.ExceptionType.CRMInaccessible:
                        logEntry.LogDetailsText = le.Message + " " + logEntry.LogDetailsText;
                        break;
                    case LoggerException.ExceptionType.Other:
                        logEntry.LogDetailsText = le.Message + " Original log entry:" + logEntry.LogDetailsText;
                        break;
                    default:
                        break;
                }
                WriteLogEntryToEventLog(logEntry);
            }
                
        }
 
        public static void LogInformation(string fileName, EntityInformation entityInfo, string componName, string overview, string description)
        {
            WriteLogEntry(new LogEntry(LogEntry.LogType.Information, fileName, entityInfo, componName, overview, description));
        }

        public static void HandleException(Exception e)
        {
            // Display the details of the exception.
            Console.WriteLine("\n" + e.Message);
            Console.WriteLine(e.StackTrace);

            if (e.InnerException != null) HandleException(e.InnerException);
        }

    }
}
