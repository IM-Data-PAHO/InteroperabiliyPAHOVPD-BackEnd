using Microservice.VPDDataImport.Services.Queries;
using Microservice.VPDDataImport.Services.Queries.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Api.Controllers
{
    /// <summary>
    /// Endpoint principal del controlador
    /// </summary>
    [ApiController]
    [Route("apipaho/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginQueryService _loginQueryService;

        public LoginController(ILoginQueryService loginQueryService)
        {
            _loginQueryService = loginQueryService;
      
        }

        /// <summary>
        /// Endpoint para hacer el login
        /// </summary>
        /// <param name="command">Clase con todos los atributos propios de la importación</param>
        /// <returns>Retorna un dto de tipo LoginResponseDto</returns>
        [HttpPost]
        [Route("getUser")]
        public async Task<LoginResponseDto> getUser([FromBody] LoginRequestDto command)
        {
            return await _loginQueryService.GetLogin(command.username, command.password);

        }

        /// <summary>
        /// Endpoint que retorna algunas de las configuraciones del usuario, como idioma, correo, rol entre otros
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna dto de tipo UserSettingDto</returns>
        [HttpGet]
        [Route("getUserSetting/{token}")]
        public async Task<UserSettingDto> getUserSetting(string token)
        {
            return await _loginQueryService.GetUserSetting(token);
        }     

    }
}
