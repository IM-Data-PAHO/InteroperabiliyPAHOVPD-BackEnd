using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.Common.Security
{

    public class IdSchemes
    {
    }

    public class ImportCount
    {
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Ignored { get; set; }
        public int Deleted { get; set; }
    }

    public class ImportOptions
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

    public class ImportSummary
    {
        public string ResponseType { get; set; }
        public string Status { get; set; }
        public object ImportOptions { get; set; }
        public ImportCount ImportCount { get; set; }
        public List<object> Conflicts { get; set; }
        public string Reference { get; set; }
        public object Enrollments { get; set; }
    }

    public class Response
    {
        public string ResponseType { get; set; }
        public string Status { get; set; }
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        public int Ignored { get; set; }
        public ImportOptions ImportOptions { get; set; }
        public List<ImportSummary> ImportSummaries { get; set; }
        public int Total { get; set; }
    }

    public class ResponseDto
    {
        public string HttpStatus { get; set; }
        public int HttpStatusCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public Response Response { get; set; }
    }
}

