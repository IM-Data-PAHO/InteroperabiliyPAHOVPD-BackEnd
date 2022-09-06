using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public class ResponseDhisDto
    {
        public class ImportCount
        {
            public int imported { get; set; }
            public int updated { get; set; }
            public int ignored { get; set; }
            public int deleted { get; set; }
        }

        public class ImportOptions
        {
            public IdSchemes idSchemes { get; set; }
            public bool dryRun { get; set; }
            public bool async { get; set; }
            public string importStrategy { get; set; }
            public string mergeMode { get; set; }
            public string reportMode { get; set; }
            public bool skipExistingCheck { get; set; }
            public bool sharing { get; set; }
            public bool skipNotifications { get; set; }
            public bool skipAudit { get; set; }
            public bool datasetAllowsPeriods { get; set; }
            public bool strictPeriods { get; set; }
            public bool strictDataElements { get; set; }
            public bool strictCategoryOptionCombos { get; set; }
            public bool strictAttributeOptionCombos { get; set; }
            public bool strictOrganisationUnits { get; set; }
            public bool requireCategoryOptionCombo { get; set; }
            public bool requireAttributeOptionCombo { get; set; }
            public bool skipPatternValidation { get; set; }
            public bool ignoreEmptyCollection { get; set; }
            public bool force { get; set; }
            public bool firstRowIsHeader { get; set; }
            public bool skipLastUpdated { get; set; }
            public bool mergeDataValues { get; set; }
            public bool skipCache { get; set; }
        }

        public class ImportSummary
        {
            public string responseType { get; set; }
            public string status { get; set; }
            public string description { get; set; }
            public ImportCount importCount { get; set; }
            public List<object> conflicts { get; set; }
            public string reference { get; set; }
        }

        public class Response
        {
            public string responseType { get; set; }
            public string status { get; set; }
            public int imported { get; set; }
            public int updated { get; set; }
            public int deleted { get; set; }
            public int ignored { get; set; }
            public ImportOptions importOptions { get; set; }
            public List<ImportSummary> importSummaries { get; set; }
            public int total { get; set; }
            public string relativeNotifierEndpoint { get; set; }
        }

        public class ResponseDhis
        {
            public string httpStatus { get; set; }
            public int httpStatusCode { get; set; }
            public string status { get; set; }
            public string message { get; set; }
            public Response response { get; set; }
        }
    }
}
