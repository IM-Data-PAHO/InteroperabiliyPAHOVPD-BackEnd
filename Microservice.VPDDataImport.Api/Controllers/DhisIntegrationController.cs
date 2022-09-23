using FluentValidation;
using Microservice.VPDDataImport.Services.EventHandlers.Commands;
using Microservice.VPDDataImport.Services.Queries;
using Microservice.VPDDataImport.Services.Queries.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Api.Controllers
{
    /// <summary>
    /// Endpoint principal del controlador
    /// </summary>
    [ApiController]
    [Route("apipaho/[controller]")]
    public class DhisIntegrationController : ControllerBase
    {
        private readonly IDhisQueryService _dhisQueryService;
        private readonly IMediator _mediator;
        public DhisIntegrationController(IDhisQueryService dhisQueryService, IMediator mediator)
        {
            _dhisQueryService = dhisQueryService;
            _mediator = mediator;
        }

        /// <summary>
        /// Endpoint que retorna todos los programas mapeados
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo DhisProgramDto </returns>
        [HttpGet]
        [Route("getAllProgram/{token}")]
        public async Task<DhisProgramDto> GetAllProgram(string token)
        {         
            return await _dhisQueryService.GetAllProgramAsync(token);
        }

        /// <summary>
        /// Endpoint que retorna todas las unidades organizativas
        /// </summary>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo </returns>
        [HttpGet]
        [Route("getAllOrganisationUnits/{token}")]
        public async Task<OrganisationUnitsDto> GetAllOrganisationUnits(string token)
        {
            return await _dhisQueryService.GetAllOrganisation(token);
        }

        /// <summary>
        /// Endpoint que genera un uid para crear un nuevo tracked
        /// </summary>
        /// <param name="quantity">cantidad</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo </returns>
        [HttpGet]
        [Route("getUidGenerated/{quantity}/{token}")]
        public async Task<UidGeneratedDto> GetUidGenerated(string quantity, string token)
        {
            return await _dhisQueryService.GetUidGenerated(quantity, token);
        }

        /// <summary>
        /// Endpoint que consulta el tracked
        /// </summary>
        /// <param name="caseid">Número de identificación de los casos</param>
        /// <param name="ou">Unidad organizativa</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo AddTrackedDto</returns>
        [HttpGet]
        [Route("getTracked/{caseid}/{ou}/{token}/{atribute}")]
        public async Task<AddTrackedDto> GetTracket(string caseid,string ou,string token, string atribute)
        {
            return await _dhisQueryService.GetTracked(caseid,ou,token,  atribute);
        }

        /// <summary>
        /// Endpoint que consulta los enrollments
        /// </summary>
        /// <param name="caseid">Número de identificación de los casos</param>
        /// <param name="ou">Unidad organizativa</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo AddEnrollmentsClearDto </returns>
        [HttpGet]
        [Route("getEnrollment/{caseid}/{ou}/{token}")]
        public async Task<AddEnrollmentsClearDto> GetEnrollment(string tracked, string ou, string token)
        {
            return await _dhisQueryService.GetEnrollment(tracked, ou, token);
        }

        /// <summary>
        /// Endpoint para obtener las secuencias
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="token"></param>
        /// <returns>Retorna un dto de tipo lista de SequentialDto</returns>
        [HttpGet]
        [Route("getSequential/{quantity}/{token}")]
        public async Task<List<SequentialDto>> GetSequential(string quantity, string token)
        {
            return await _dhisQueryService.GetSequential(quantity, token);
        }

        /// <summary>
        /// Endpoint para ejecutar la Pre-validación
        /// </summary>
        /// <param name="request">Clase con todos los atributos propios de la importación</param>
        /// <returns>Retorna un dto de tipo dryrunDto</returns>
        [HttpPost]
        [Route("startDryRun")]
        public async Task<dryrunDto> StartDryRun([FromForm] HistoryCreateCommandDto request)
        {

            //objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(request.Programsid)).FirstOrDefault();
            //var validation = _historyValidator.Validate(request);

            return await _dhisQueryService.StartDryRunAsync(request);
            //return (new dryrunDto
            //{
            //    Response = "Error en datos",
            //    State = "400",
            //    Uploads = 10,
            //    Deleted = 5
            //});
            //return await _dhisQueryService.StartDryRunAsync(request);
        }

        /// <summary>
        /// Endpoint para agregar los trackeds, de forma individual ó masiva
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns>Retorna un dto de tipo AddTracketResultDto</returns>
        [HttpPost]
        [Route("addTracked/{token}")]
        public async Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token)
        {
            return await _dhisQueryService.AddTracked(request, token);
        }

        /// <summary>
        /// Endpoint para agregar los enrollments, de forma individual ó masiva
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo AddEnrollmentResultDto</returns>
        [HttpPost]
        [Route("enrollment/{token}")]
        public async Task<AddEnrollmentResultDto> Enrollment(AddEnrollmentDto request, string token)
        {
            return await _dhisQueryService.AddEnrollment(request, token);
        }

        /// <summary>
        /// Endpoint para agregar los events, de forma individual ó masiva
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un dto de tipo dynamic (AddEventResultDto)</returns>
        [HttpPost]
        [Route("addEvent/{token}")]
        public async Task<dynamic> AddEvent(AddEventsDto request, string token)
        {
            return await _dhisQueryService.AddEvent(request, token);
        }
    }
}
