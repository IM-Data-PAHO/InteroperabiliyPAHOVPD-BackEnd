using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public class HistoryCreateCommandDto
    {
        public string Programsid { get; set; }
        public IFormFile CsvFile { get; set; }
        public IFormFile CsvFile01 { get; set; }
        public string UserLogin { get; set; }
        public int startdate { get; set; }
        public int enddate { get; set; }
        public string token { get; set; }
        public string reponse { get; set; }
        public string separator { get; set; }
    }
}
