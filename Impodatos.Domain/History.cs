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
}
