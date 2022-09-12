using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.Queries.DTOs
{
    public class UserSettingDto
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string surname { get; set; }
        public string firstName { get; set; }
        public string email { get; set; }   
        public Response settings { get; set; }
        public UserRoles userCredentials { get; set; }


    }

    public partial class Response
    {    
        public string keyUiLocale { get; set; }        
    }

    public partial class UserRoles
    {
        public List<UserRole> userRoles { get; set; }
    }
    public partial class UserRole
    {
        public string id { get; set; }
        public string name { get; set; }
    }



}
