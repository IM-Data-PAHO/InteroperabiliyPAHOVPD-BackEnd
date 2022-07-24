using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Domain
{
    public class history
    {
        public int id { get; set; }
        public int uploads { get; set; }
        public int deleted { get; set; }
        public string programsid { get; set; }
        public string jsonset { get; set; }
        public string jsonresponse { get; set; }
        public bool state { get; set; }
        public string userlogin { get; set; }
        public DateTime fecha { get; set; }
        public byte[] file { get; set; }
    }
    //public class DehisResponse
    //{
    //    public string responseType { get; set; }
    //    public string status { get; set; }
    //    public int imported { get; set; }
    //    public int updated { get; set; }
    //    public int deleted { get; set; }
    //    public int ignored { get; set; }
    //    public List<ImportSummary> importSummaries { get; set; }
    //    public int total { get; set; }
    //}
    //public partial class ImportSummary
    //{
    //    public string ResponseType { get; set; }
    //    public string Status { get; set; }
    //    public ImportOptions ImportOptions { get; set; }
    //    public ImportCount ImportCount { get; set; }
    //    public List<Conflict> Conflicts { get; set; }
    //    public string Reference { get; set; }
    //    public Response Enrollments { get; set; }
    //}
    //public partial class Conflict
    //{
    //    public string Object { get; set; }
    //    public string Value { get; set; }
    //}
    //public partial class Response
    //{
    //    public string ResponseType { get; set; }
    //    public string Status { get; set; }
    //    public long Imported { get; set; }
    //    public long Updated { get; set; }
    //    public long Deleted { get; set; }
    //    public long Ignored { get; set; }
    //    public ImportOptions ImportOptions { get; set; }
    //    public List<ImportSummary> ImportSummaries { get; set; }
    //    public long Total { get; set; }
    //    public string relativeNotifierEndpoint { get; set; }
    //}
    //public partial class ImportCount
    //{
    //    public long Imported { get; set; }
    //    public long Updated { get; set; }
    //    public long Ignored { get; set; }
    //    public long Deleted { get; set; }
    //}
    //public partial class IdSchemes
    //{
    //}
    //public partial class ImportOptions
    //{
    //    public IdSchemes IdSchemes { get; set; }
    //    public bool DryRun { get; set; }
    //    public bool Async { get; set; }
    //    public string ImportStrategy { get; set; }
    //    public string MergeMode { get; set; }
    //    public string ReportMode { get; set; }
    //    public bool SkipExistingCheck { get; set; }
    //    public bool Sharing { get; set; }
    //    public bool SkipNotifications { get; set; }
    //    public bool SkipAudit { get; set; }
    //    public bool DatasetAllowsPeriods { get; set; }
    //    public bool StrictPeriods { get; set; }
    //    public bool StrictDataElements { get; set; }
    //    public bool StrictCategoryOptionCombos { get; set; }
    //    public bool StrictAttributeOptionCombos { get; set; }
    //    public bool StrictOrganisationUnits { get; set; }
    //    public bool RequireCategoryOptionCombo { get; set; }
    //    public bool RequireAttributeOptionCombo { get; set; }
    //    public bool SkipPatternValidation { get; set; }
    //    public bool IgnoreEmptyCollection { get; set; }
    //    public bool Force { get; set; }
    //    public bool FirstRowIsHeader { get; set; }
    //    public bool SkipLastUpdated { get; set; }
    //    public bool MergeDataValues { get; set; }
    //    public bool SkipCache { get; set; }
    //}


}
