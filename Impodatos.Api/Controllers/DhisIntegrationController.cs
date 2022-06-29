using FluentValidation;
using Impodatos.Services.EventHandlers.Commands;
using Impodatos.Services.Queries;
using Impodatos.Services.Queries.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Impodatos.Api.Controllers
{

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
        //[HttpGet]
        //[Route("startDryRun/{token}")]
        //public async Task<DhisProgramDto> StartDryRun(string token)
        //{
        //    return await _dhisQueryService.StartDryRunAsync(token);
        //}

        [HttpGet]
        [Route("getAllProgram/{token}")]
        public async Task<DhisProgramDto> GetAllProgram(string token)
        {         
            return await _dhisQueryService.GetAllProgramAsync(token);
        }

        [HttpGet]
        [Route("getAllOrganisationUnits/{token}")]
        public async Task<OrganisationUnitsDto> GetAllOrganisationUnits(string token)
        {
            return await _dhisQueryService.GetAllOrganisation(token);
        }

        [HttpGet]
        [Route("getUidGenerated/{quantity}/{token}")]
        public async Task<UidGeneratedDto> GetUidGenerated(string quantity, string token)
        {
            return await _dhisQueryService.GetUidGenerated(quantity, token);
        }
        [HttpGet]
        [Route("getTracket/{caseid}/{ou}/{token}")]
        public async Task<AddTrackedDto> GetTracket(string caseid,string ou,string token)
        {
            return await _dhisQueryService.GetTracket(caseid,ou,token);
        }
        [HttpGet]
        [Route("getEnrollment/{caseid}/{ou}/{token}")]
        public async Task<AddEnrollmentDto> GetEnrollment(string tracked, string ou, string token)
        {
            return await _dhisQueryService.GetEnrollment(tracked, ou, token);
        }
        [HttpGet]
        [Route("getSequential/{quantity}/{token}")]
        public async Task<List<SequentialDto>> GetSequential(string quantity, string token)
        {
            return await _dhisQueryService.GetSequential(quantity, token);
        }
        [HttpPost]
        [Route("startDryRun")]
        public async Task<dryrunDto> StartDryRun([FromForm] HistoryCreateCommandDto request)
        {
            //var validation = _historyValidator.Validate(request);
            //if (validation.IsValid)
            //{
            //    return await _dhisQueryService.StartDryRunAsync(request);
            //}
            //return (new dryrunDto
            //{
            //    Response = "Error en datos",
            //    State = "400",
            //    Uploads = 0,
            //    Deleted =0
            //});
            return await _dhisQueryService.StartDryRunAsync(request);
        }
        [HttpPost]
        [Route("addTracket/{token}")]
        public async Task<AddTracketResultDto> AddTracket(AddTrackedDto request, string token)
        {
            return await _dhisQueryService.AddTracked(request, token);
        }
        [HttpPost]
        [Route("enrollment/{token}")]
        public async Task<AddEnrollmentResultDto> Enrollment(AddEnrollmentDto request, string token)
        {
            return await _dhisQueryService.AddEnrollment(request, token);
        }
        [HttpPost]
        [Route("addEvent/{token}")]
        public async Task<dynamic> AddEvent(AddEventsDto request, string token)
        {
            return await _dhisQueryService.AddEvent(request, token);
        }
    }
}
