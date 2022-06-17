using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public partial class AddTrackedDto
    {
        public List<TrackedEntityInstances> trackedEntityInstances { get; set; }
    }

    public partial class TrackedEntityInstances
    {
        public string trackedEntityInstance{ get; set; }
        public string trackedEntityType { get; set; }
        public string orgUnit { get; set; }
        public List<Attribut> attributes { get; set; }
    }

    public partial class Attribut
    {
        public string attribute { get; set; }
        public string value { get; set; }
    }
  

}
