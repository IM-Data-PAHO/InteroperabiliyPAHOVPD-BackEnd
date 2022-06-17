﻿using Impodatos.Services.Common.Security;
using Impodatos.Services.Queries.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.Queries
{
    public interface ILoginQueryService
    {
        Task<LoginResponseDto> GetLogin(string user, string password);

    }
    public class LoginQueryService : ILoginQueryService
    {
      
        public async Task<LoginResponseDto> GetLogin(string user, string password)      
        {     
            var result = await RequestHttp.CallMethodLogin("dhis", "login" ,  user,  password);           

            return JsonConvert.DeserializeObject<LoginResponseDto>(JsonConvert.DeserializeObject<string>(result));
        }
    }
}
