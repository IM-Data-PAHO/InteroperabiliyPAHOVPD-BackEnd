using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Microservice.VPDDataImport.Services.EventHandlers.Commands
{
    public class historyCreateCommand : INotification
    {
        public string Programsid { get; set; }
        public IFormFile  CsvFile { get; set; }
        public IFormFile CsvFile01{ get; set; }
        public string UserLogin { get; set; }
        public int startdate{ get; set; }
        public int enddate { get; set; }
        public string token { get; set; }
        public string reponse { get; set; }
        public string separator { get; set; }
    }
}
