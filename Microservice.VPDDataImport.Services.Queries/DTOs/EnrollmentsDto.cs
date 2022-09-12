using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.Queries.DTOs
{
    public partial class AddEnrollmentDto
    {
        public List<Enrollment> enrollments { get; set; }
    }
    public partial class Enrollment
    {
        public string trackedEntityInstance { get; set; }
        public string program { get; set; }
        public string status { get; set; }
        public string orgUnit { get; set; }
        public string enrollmentDate { get; set; }
        public string incidentDate { get; set; }        
        public string enrollment { get; set; }
        public List<AddEventDto> Eev { get; set; }
    }
    public partial class AddEnrollmentResultDto
    {
        public string HttpStatus { get; set; }
        public long HttpStatusCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public Response Response { get; set; }
    }

    public class AddEnrollmentsClearDto
    {
        public List<AddEnrollmentClearDto> enrollments { get; set; }
    }
    public partial class AddEnrollmentClearDto
    {
        public string enrollment { get; set; }
    }
}
