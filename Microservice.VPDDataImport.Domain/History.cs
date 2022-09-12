using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Domain
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
        public string namefile { get; set; }
        public string country { get; set; }
        public string namefile1 { get; set; }
        public byte[] file1 { get; set; }                   
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ConflictDhis
    {
        public string @object { get; set; }
        public string value { get; set; }
    }

    public class EnrollmentsDhis
    {
        public string responseType { get; set; }
        public string status { get; set; }
        public int imported { get; set; }
        public int updated { get; set; }
        public int deleted { get; set; }
        public int ignored { get; set; }
        public List<ImportSummaryDhis> importSummaries { get; set; }
        public int total { get; set; }
    }

    public class Events
    {
        public string responseType { get; set; }
        public string status { get; set; }
        public int imported { get; set; }
        public int updated { get; set; }
        public int deleted { get; set; }
        public int ignored { get; set; }
        public List<ImportSummaryDhis> importSummaries { get; set; }
        public int total { get; set; }
    }

    public class ImportCount
    {
        public int imported { get; set; }
        public int updated { get; set; }
        public int ignored { get; set; }
        public int deleted { get; set; }
    }

    public class ImportSummaryDhis
    {
        public string responseType { get; set; }
        public string status { get; set; }
        public ImportCount importCount { get; set; }
        public List<ConflictDhis> conflicts { get; set; }
        public string reference { get; set; }
        public EnrollmentsDhis enrollments { get; set; }
        public Events events { get; set; }
    }

    public class Root
    {
        public string responseType { get; set; }
        public string status { get; set; }
        public int imported { get; set; }
        public int updated { get; set; }
        public int deleted { get; set; }
        public int ignored { get; set; }
        public List<ImportSummaryDhis> importSummaries { get; set; }
        public int total { get; set; }     
        
    }




}
