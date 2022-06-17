using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public partial class OrganisationUnitsDto
    {
        public List<OrganisationUnit> OrganisationUnits { get; set; }
    }

    public partial class OrganisationUnit
    {
        public string code { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public string path { get; set; }
    }

    //public partial class Pager
    //{
    //    public long Page { get; set; }
    //    public long PageCount { get; set; }
    //    public long Total { get; set; }
    //    public long PageSize { get; set; }
    //}
}
