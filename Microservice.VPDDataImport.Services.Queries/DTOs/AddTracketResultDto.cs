using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.Queries.DTOs
{
        public partial class AddTracketResultDto
        {
            public string HttpStatus { get; set; }
            public long HttpStatusCode { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
            public Response Response { get; set; }
        }

        public partial class ImportSummary
        {
            public string ResponseType { get; set; }
            public string Status { get; set; }
            public ImportOptions ImportOptions { get; set; }
            public ImportCount ImportCount { get; set; }
            public List<Conflict> Conflicts { get; set; }
            public string Reference { get; set; }
            public Response Enrollments { get; set; }
        }

        public partial class Response
        {
            public string ResponseType { get; set; }
            public string Status { get; set; }
            public long Imported { get; set; }
            public long Updated { get; set; }
            public long Deleted { get; set; }
            public long Ignored { get; set; }
            public ImportOptions ImportOptions { get; set; }
            public List<ImportSummary> ImportSummaries { get; set; }
            public long Total { get; set; }
            public string relativeNotifierEndpoint { get; set; }
        }

        public partial class RelativeNotifierEndpoint
    {
        public string uid { get; set; }
        public string level { get; set; }
        public string category { get; set; }
        public string time { get; set; }
        public string message { get; set; }
        public bool completed { get; set; }
        public string id { get; set; }
    }


        public partial class Conflict
        {
            public string Object { get; set; }
            public string Value { get; set; }
        }

        public partial class ImportCount
        {
            public long Imported { get; set; }
            public long Updated { get; set; }
            public long Ignored { get; set; }
            public long Deleted { get; set; }
        }

        public partial class ImportOptions
        {
            public IdSchemes IdSchemes { get; set; }
            public bool DryRun { get; set; }
            public bool Async { get; set; }
            public string ImportStrategy { get; set; }
            public string MergeMode { get; set; }
            public string ReportMode { get; set; }
            public bool SkipExistingCheck { get; set; }
            public bool Sharing { get; set; }
            public bool SkipNotifications { get; set; }
            public bool SkipAudit { get; set; }
            public bool DatasetAllowsPeriods { get; set; }
            public bool StrictPeriods { get; set; }
            public bool StrictDataElements { get; set; }
            public bool StrictCategoryOptionCombos { get; set; }
            public bool StrictAttributeOptionCombos { get; set; }
            public bool StrictOrganisationUnits { get; set; }
            public bool RequireCategoryOptionCombo { get; set; }
            public bool RequireAttributeOptionCombo { get; set; }
            public bool SkipPatternValidation { get; set; }
            public bool IgnoreEmptyCollection { get; set; }
            public bool Force { get; set; }
            public bool FirstRowIsHeader { get; set; }
            public bool SkipLastUpdated { get; set; }
            public bool MergeDataValues { get; set; }
            public bool SkipCache { get; set; }
        }

        public partial class IdSchemes
        {
        }
   

}
