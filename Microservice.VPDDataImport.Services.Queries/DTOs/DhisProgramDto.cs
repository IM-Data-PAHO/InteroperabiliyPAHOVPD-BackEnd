using System;
using System.Collections.Generic;

namespace Microservice.VPDDataImport.Services.Queries.DTOs
{
    public partial class OAuth2Token
    {

        public string access_token { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }


    }

    public class infodhis
    {
        public string info { get; set; }
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
        public string caseidcolumm { get; set; }
        public string firstlastnamecolumm { get; set; }
        public string secondlastnamecolumm { get; set; }
        public string firstnamecolumm { get; set; }
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
        public string valueType { get; set; }
        public OptionSet optionSet { get; set; }
    }
    public class ProgramStageDataElement
    {
        public string compulsory { get; set; }
        public DataElement dataElement { get; set; }
    }
    public class Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Column { get; set; }
        public OptionSet optionSet { get; set; }
        public string mandatory { get; set; }
        public string valueType { get; set; }
    }
    public class Option
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class OptionSet
    {
        public List<Option> options { get; set; }
    }

    public class ResultTaskDto
    {
        public List<ResultTask> resultTasks { get; set; }
    }

    public partial class ResultTask
    {
        public string uid { get; set; }
        public string level { get; set; }
        public string category { get; set; }
        public string time { get; set; }
        public string message { get; set; }
        public bool completed { get; set; }
        public string id { get; set; }     
    }


    public class AttributeReference
    {
        public DateTime lastUpdated { get; set; }
        public string storedBy { get; set; }
        public string displayName { get; set; }
        public DateTime created { get; set; }
        public string valueType { get; set; }
        public string attribute { get; set; }
        public string value { get; set; }
    }

    public class TrackedreferenceResponse
    {
        public DateTime created { get; set; }
        public string orgUnit { get; set; }
        public DateTime createdAtClient { get; set; }
        public string trackedEntityInstance { get; set; }
        public DateTime lastUpdated { get; set; }
        public string trackedEntityType { get; set; }
        public DateTime lastUpdatedAtClient { get; set; }
        public string storedBy { get; set; }
        public bool deleted { get; set; }
        public string featureType { get; set; }
        public List<object> programOwners { get; set; }
        public List<object> enrollments { get; set; }
        public List<object> relationships { get; set; }
        public List<AttributeReference> attributes { get; set; }
    }

}

