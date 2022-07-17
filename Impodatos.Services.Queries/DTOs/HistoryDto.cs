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
    }
    public class validateDto
    {
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
    }
    public class TrackedhistoryDto
    {

        public int Id { get; set; }
        public string trackedEntityInstance { get; set; }
        public string mxKJ869xJOd { get; set; }
        public string kR6TpjXjMP7 { get; set; }
        public bool State { get; set; }
        public string UserLogin { get; set; }
        public DateTime Fecha { get; set; }
    }
}
