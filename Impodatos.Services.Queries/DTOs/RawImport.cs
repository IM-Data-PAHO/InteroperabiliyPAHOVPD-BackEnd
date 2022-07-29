using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public class RawImport
    {
        public string Programsid { get; set; }
        public dynamic Data { get; set; }
        public string UserLogin { get; set; }
        public int Startdate { get; set; }
        public int Enddate { get; set; }
        public string Token { get; set; }
    }

    
}
