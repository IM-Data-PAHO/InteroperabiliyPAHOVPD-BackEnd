using Microservice.VPDDataImport.Services.Common.Security;
using Microservice.VPDDataImport.Services.Queries.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.Queries
{
    public interface ILoginQueryService
    {
        Task<LoginResponseDto> GetLogin(string user, string password);
        Task<UserSettingDto> GetUserSetting(string token);  

    }
    public class LoginQueryService : ILoginQueryService
    {
        /// <summary>
        /// Método para obtener el login
        /// </summary>
        /// <param name="user">Usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>Retorna un dto de tipo LoginResponseDto</returns>
        public async Task<LoginResponseDto> GetLogin(string user, string password)      
        {     
            var result = await RequestHttp.CallMethodLogin("dhis", "login" ,  user,  password);           

            return JsonConvert.DeserializeObject<LoginResponseDto>(JsonConvert.DeserializeObject<string>(result));
        }     

        /// <summary>
        /// Método que retorna las configuraciones del usuario (idioma, correo, rol, etc)
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns></returns>
        public async Task<UserSettingDto> GetUserSetting(string token)
        {
            var result = await RequestHttp.CallMethodGetUserSetting("dhis",  token);            
            return JsonConvert.DeserializeObject<UserSettingDto>(result);
        }
    }
}
