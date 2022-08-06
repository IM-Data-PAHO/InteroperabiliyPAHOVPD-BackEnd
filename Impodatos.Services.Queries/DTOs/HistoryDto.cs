using System;
using System.Collections.Generic;

namespace Impodatos.Services.Queries.DTOs
{
    public class dryrunDto
    {
        public int Uploads { get; set; }
        public int Deleted { get; set; }
        public List<validateDto> Response { get; set; }
        public string State { get; set; }
        public List<sumaryerrorDto> Sumary { get; set; }
    }
    public class sumaryerrorDto
    {
        public int date { get; set; }
        public int mandatory { get; set; }
        public int compulsory { get; set; }
        public int option { get; set; }
    }
    public class validateDto
    {
        public int indexpreload { get; set; }
        public string id { get; set; }
        public string detail { get; set; }
        public int ln { get; set; }
        public int cl { get; set; }
        public string ms { get; set; }
        public string errortype { get; set; }
        public string value { get; set; }
    }
    public class historyDto
    {
        public int Id { get; set; }
        public int Uploads { get; set; }
        public int Deleted { get; set; }
        public string Programsid { get; set; }
        public string JsonSet { get; set; }
        public string JsonResponse { get; set; }
        public bool State { get; set; }
        public string UserLogin { get; set; }
        public DateTime Fecha { get; set; }
        public byte [] File { get; set; }
        public string namefile { get; set; }
        public string country { get; set; }
        public string namefile1 { get; set; }
        public byte[] file1 { get; set; }
    }
    //public class TrackedhistoryDto
    //{

    //    public int Id { get; set; }
    //    public string trackedEntityInstance { get; set; }
    //    public string mxKJ869xJOd { get; set; }
    //    public string kR6TpjXjMP7 { get; set; }
    //    public bool State { get; set; }
    //    public string UserLogin { get; set; }
    //    public DateTime Fecha { get; set; }
    //}
    //public class EnrollmentsResp
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

    //public class EventsResp
    //{
    //    public string responseType { get; set; }
    //    public string status { get; set; }
    //    public int imported { get; set; }
    //    public int updated { get; set; }
    //    public int deleted { get; set; }
    //    public int ignored { get; set; }
    //    public List<ImportSummaryResp> importSummaries { get; set; }
    //    public int total { get; set; }
    //}
    //public class ImportCountResp
    //{
    //    public int imported { get; set; }
    //    public int updated { get; set; }
    //    public int ignored { get; set; }
    //    public int deleted { get; set; }
    //}
    //public class ImportSummaryResp
    //{
    //    public string responseType { get; set; }
    //    public string status { get; set; }
    //    public ImportCount importCount { get; set; }
    //    public List<object> conflicts { get; set; }
    //    public string reference { get; set; }
    //    public EnrollmentsResp enrollments { get; set; }
    //    public EventsResp events { get; set; }
    //}
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
}
