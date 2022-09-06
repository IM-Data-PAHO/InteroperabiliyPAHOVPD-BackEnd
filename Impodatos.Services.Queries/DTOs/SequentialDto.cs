using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public partial class SequentialDto
    {
        public string ownerObject { get; set; }
        public string ownerUid { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string created { get; set; }
        public string expiryDate { get; set; }
    }
}
