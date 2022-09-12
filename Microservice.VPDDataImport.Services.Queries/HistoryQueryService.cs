using Microsoft.EntityFrameworkCore;
using Microservice.VPDDataImport.Persistence.Database;
using Microservice.VPDDataImport.Services.Queries.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microservice.VPDDataImport.Domain;
using AutoMapper;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Data;
using Microservice.VPDDataImport.Services.Common.Security;

namespace Microservice.VPDDataImport.Services.Queries
{
    public interface IhistoryQueryService
    {
        Task<IEnumerable<historyDto>> GetAllAsync();
        Task<IEnumerable<historyDto>> GethistoryUserAsync(string correo, string token);
        Task<DhisProgramDto> GetAllProgramAsync(string token);
        Task<TrackedreferenceResponse> GetTrackedreferenceAsync(string token, string reference);

        //Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token);
        //Task<IEnumerable<historyDto>> GetAllAsync01();
        //Task<IEnumerable<historyDto>> GethistoryUserAsync01(string correo);
    }

    public class historyQueryService : IhistoryQueryService
    {
        private readonly ApplicationDbContext _context;

        private readonly IMapper _mapper;
        public historyQueryService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<historyDto>> GetAllAsync()
        {
            var result = await _context.history.ToListAsync();

            return _mapper.Map<IEnumerable<historyDto>>(result);
        }

        /// <summary>
        /// Métodoo que retorna el historial de las importaciones
        /// </summary>
        /// <param name="correo">email del usuario logeado</param>
        /// <param name="token">Token de atenticación</param>
        /// <returns></returns>
        public async Task<IEnumerable<historyDto>> GethistoryUserAsync(string correo, string token)
        {

            List<history> result = new List<history>();
            var userSetting = await GetUserSetting(token);
            if (userSetting.userCredentials.userRoles[0].name == "Superuser")
            {
                result = await (from c in _context.history orderby c.fecha descending select c).ToListAsync();
            }
            else
            {
                result = await (from c in _context.history where c.userlogin.Equals(correo) orderby c.fecha descending select c).ToListAsync();
              }

            if (result.Count > 0)
            {
                Program objprogram = new Program();
                var ExternalImportDataApp = await GetAllProgramAsync(token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programname.Equals(result[0].programsid.Trim())).FirstOrDefault();
                int idresult = 0;
                foreach (history dto in result)
                {
                    List<infodhis> error = new List<infodhis>();
                    history objdto = new history();
                    objdto = result[idresult];
                    if (objdto.id == 88)
                    {
                    }
                    var json = objdto.jsonset;
                    string ResponseError = "";
                    var jsonpars = json.Remove(0, 1);
                    int lg = json.Length - 2;
                    jsonpars = jsonpars.Remove(lg, 1);
                    jsonpars = jsonpars.Replace("\\", "");
                    jsonpars = "[" + jsonpars + "]";
                    jsonpars = jsonpars.Replace("[\"", "[");
                    jsonpars = jsonpars.Replace("\"]", "]");
                    jsonpars = jsonpars.Replace("}\",\"{", "},{"); 
                    try
                    {

                        var dhisResponse = JsonConvert.DeserializeObject<List<Root>>(jsonpars);
                        foreach (Root item in dhisResponse)
                            foreach (ImportSummaryDhis itemsum in item.importSummaries) //f8NbfmhXzl3
                            {
                                if (itemsum.conflicts.Count > 0)
                                {
                                    var st = itemsum.conflicts[0];
                                    foreach (ConflictDhis conflict in itemsum.conflicts)
                                    {                                    
                                        var Case_ID = await GetTrackedreferenceAsync(token, itemsum.reference);                                                  
                                        ResponseError = "\nCase_Id " + Case_ID.attributes[0].value + " : "  + conflict.value + ";" + ResponseError;
                                               
                                    }
                                }
                            
                                foreach (ImportSummaryDhis itemed in itemsum.enrollments.importSummaries)
                                    foreach (ImportSummaryDhis itemev in itemed.events.importSummaries)
                                    {
                                        
                                        
                                        if (itemev.conflicts.Count > 0)
                                        {
                                            foreach (ConflictDhis conflict in itemev.conflicts)
                                            {
                                                foreach (ProgramStage ps in objprogram.programStages)
                                                    foreach (ProgramStageDataElement dtele in ps.programStageDataElements)
                                                        if (dtele.dataElement.id == conflict.@object)
                                                        {
                                                            var Case_ID = await GetTrackedreferenceAsync(token, itemsum.reference);
                                                            //infodhis inf = new infodhis();
                                                            //inf.info = "Case_Id " + Case_ID.attributes[0].value + " : " + dtele.dataElement.name + "," + conflict.value;
                                                            //error.Add(inf);
                                                            ResponseError = "\nCase_Id " + Case_ID.attributes[0].value + " : " + dtele.dataElement.name + "," + conflict.value + ";" + ResponseError;
                                                        }
                                            }


                                        }
                                    }
                            }

                    }
                    catch (Exception e)
                    {
                        //infodhis inf = new infodhis();
                        //inf.info = "Error al serializar respuesta";
                        //error.Add(inf);
                        ResponseError = "";
                    }
                    result[idresult].jsonset = ResponseError;//JsonConvert.SerializeObject(error); 

                    idresult++;
                }
            }


            return _mapper.Map<IEnumerable<historyDto>>(result);
        }

        /// <summary>
        /// Método que retorna todos los programas mapeados
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo DhisProgramDto</returns>
        public async Task<DhisProgramDto> GetAllProgramAsync(string token)
        {
            var result = await RequestHttp.CallMethod("dhis", "program", token);
            return JsonConvert.DeserializeObject<DhisProgramDto>(result);
        }

        /// <summary>
        /// Método que retorna las configuraciones de usuario (correo, idioma, rol, etc)
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo UserSettingDto</returns>
        public async Task<UserSettingDto> GetUserSetting(string token)
        {
            var result = await RequestHttp.CallMethodGetUserSetting("dhis", token);
            return JsonConvert.DeserializeObject<UserSettingDto>(result);
        }

        /// <summary>
        /// Método que retorna cual es el atributo con error dentro del summary
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <param name="reference"></param>
        /// <returns>Retorna un dto de tipo TrackedreferenceResponse</returns>
        public async Task<TrackedreferenceResponse> GetTrackedreferenceAsync(string token, string reference)
        {
            var result = await RequestHttp.CallMethod("dhis", "trackedreference", token, reference);
            return JsonConvert.DeserializeObject<TrackedreferenceResponse>(result);
        }

      
    }
}
