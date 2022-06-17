using System;
using System.Collections.Generic;

namespace Impodatos.Services.Queries.DTOs
{
    public partial class OAuth2Token
    {

        public string access_token { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }


    }


    public partial class DhisProgramDto
    {
        public List<object> Datasets { get; set; }
        public List<Program> Programs { get; set; }

        public static implicit operator List<object>(DhisProgramDto v)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Program
    {
        public string Programid { get; set; }
        public string Programname { get; set; }
        public string Enrollmentdatecolumm { get; set; }
        public string Incidentdatecolumm { get; set; }
        public string Orgunitcolumm { get; set; }
        public string Orgunitid { get; set; }
        public string Status { get; set; }
        public string Trackedentitytype { get; set; }
        public string caseNum { get; set; }
        public List<Attribute> Attribute { get; set; }
        public List<ProgramStage> programStages { get; set; }
    }
    public class ProgramStage
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<ProgramStageDataElement> programStageDataElements { get; set; }
    }
    public class DataElement
    {
        public string column { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    }

    public class ProgramStageDataElement
    {
        public DataElement dataElement { get; set; }
    }
    public class Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Column { get; set; }
    }
}
