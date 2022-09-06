using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries.DTOs
{
    public class AddEventsDto
    {
        public List<AddEventDto> events { get; set; }
    }
    public partial class AddEventDto
    {   public string event_ { get; set; }
        public string trackedEntityInstance { get; set; }
        public string program { get; set; }
        public string orgUnit { get; set; }
        public string programStage { get; set; }
        public string eventDate { get; set; }
        public string status { get; set; }
        public string storedBy { get; set; }
        public Coordinate coordinate { get; set; }
        public List<DataValue> dataValues { get; set; }
    }
    public class AddEventsClearDto
    {
        public List<AddEventClearDto> events { get; set; }
    }
    public partial class AddEventClearDto
    {
        public string event_ { get; set; }
    }
    public class Coordinate
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class DataValue
    {
        public string dataElement { get; set; }
        public string value { get; set; }
    }
    public partial class AddEventResultDto
    {
        public string HttpStatus { get; set; }
        public long HttpStatusCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public Response Response { get; set; }
    }

}
