using Impodatos.Services.Queries;
using Impodatos.Services.Queries.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Impodatos.Api.Controllers
{
    [ApiController]
    [Route("apipaho/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginQueryService _loginQueryService;

        public LoginController(ILoginQueryService loginQueryService)
        {
            _loginQueryService = loginQueryService;
      
        }
        [HttpPost]
        [Route("getUser")]
        public async Task<LoginResponseDto> getUser([FromBody] LoginRequestDto command)
        {
            return await _loginQueryService.GetLogin(command.username, command.password);

        }
    }
}
