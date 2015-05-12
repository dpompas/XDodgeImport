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
    
	public class TransactionCurrencyLookups
    {
        private IOrganizationService service;
        Dictionary<string, object> _TransactionCurrencyISO_IdDictionary = null;

        public TransactionCurrencyLookups (OrganizationServiceProxy serviceProxy)
        {
            service = (IOrganizationService)serviceProxy;
        }

        public Dictionary<string, object> RetrieveAll()
        {
            Dictionary<string, object> isotoGuidCurrencies = new Dictionary<string, object>();
            ColumnSet colset = new ColumnSet(new string[] {"transactioncurrencyid", "isocurrencycode"});
            QueryExpression query = new QueryExpression
            {
                EntityName = TransactionCurrency.EntityLogicalName,
                ColumnSet = colset
            };
            DataCollection<Entity>  entities = service.RetrieveMultiple(query).Entities;
            foreach (Entity e in entities)
            {
                isotoGuidCurrencies.Add(((TransactionCurrency)e).ISOCurrencyCode, ((TransactionCurrency)e).Id);
            }

            _TransactionCurrencyISO_IdDictionary = isotoGuidCurrencies;
            return _TransactionCurrencyISO_IdDictionary;

        }

        public Guid TransactionCurrencyIdFromISOCode(string ISOCode)
        {
            if (_TransactionCurrencyISO_IdDictionary == null)
                _TransactionCurrencyISO_IdDictionary = RetrieveAll();

            if (_TransactionCurrencyISO_IdDictionary.ContainsKey(ISOCode))
                return (Guid) _TransactionCurrencyISO_IdDictionary[ISOCode];
            else
                return Guid.Empty;
        }
    }
}
