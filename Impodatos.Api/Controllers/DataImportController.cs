using FluentValidation;
using Impodatos.Services.EventHandlers.Commands;
using Impodatos.Services.Queries;
using Impodatos.Services.Queries.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Impodatos.Api.Controllers
{

    /// <summary>
    /// Endpoint principal del controlador, que redirecciona al ImportHistoryHandlerAsync.cs
    /// </summary>
    [ApiController]
    [Route("apipaho/[controller]")]
    public class DataImportController : ControllerBase
    {
        private readonly IhistoryQueryService _historyQueryService1;
        private readonly IMediator _mediator;
        private readonly IValidator<historyCreateCommand> _historyValidator;      
        public DataImportController(IhistoryQueryService historyQueryService, IMediator mediator, IValidator<historyCreateCommand> historyValidator)
        {
            _historyQueryService1 = historyQueryService;
            _mediator = mediator;
            _historyValidator = historyValidator;
        }

        /// <summary>
        /// Endpoint que consulta el historial de las importaciones
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("history")]
        public async Task<IEnumerable<historyDto>> GetAll(string user, string token)
        {
            return await _historyQueryService1.GethistoryUserAsync(user, token);
        }

        /// <summary>
        /// Endpoint de tipo POST, para el guardado de todo el proceso de la importación
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Add([FromForm] historyCreateCommand command)
        {
            var validation = _historyValidator.Validate(command);
            if (validation.IsValid)
            {
                await _mediator.Publish(command);
                return Ok(command.reponse.ToString()); ;
            }
            return BadRequest(validation.Errors);

        }

        //[HttpGet]
        //public async Task<IEnumerable<historyDto>> GetAll()
        //{
        //    return await _historyQueryService1.GetAllAsync();
        //}

        //[HttpGet]
        //public async Task<IEnumerable<historyDto>> GetAll01()
        //{
        //    return await _historyQueryService1.GetAllAsync01();
        //}
        //[HttpGet("user")]
        //public async Task<IEnumerable<historyDto>> GetAll01(string user)
        //{
        //    return await _historyQueryService1.GethistoryUserAsync01(user);
        //}

        //[HttpPut]
        //public async Task<IActionResult> Update(historyUpdateCommand command)
        //{
        //    await _mediator.Publish(command);
        //    return Ok();
        //    //var result = await _mediator.Publish(command);
        //}
        //[HttpPost]
        //[Route("rawimport")]      
        //public async Task<AddTracketResultDto> RawImport(RawImport request)
        //{

        //    return await _historyQueryService1.RawImport(request);          
        //}

    }
}
