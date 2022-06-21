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
    [ApiController]
    [Route("apipaho/[controller]")]
    public class historyController : ControllerBase
    {
        private readonly IhistoryQueryService _historyQueryService1;
        private readonly IMediator _mediator;
        private readonly IValidator<historyCreateCommand> _historyValidator;
       public historyController(IhistoryQueryService historyQueryService, IMediator mediator, IValidator<historyCreateCommand> historyValidator)
        {
            _historyQueryService1 = historyQueryService;
            _mediator = mediator;
            _historyValidator = historyValidator;
        }
        [HttpGet]
        public async Task<IEnumerable<historyDto>> GetAll()
        {
            return await _historyQueryService1.GetAllAsync();
        }
        [HttpGet("user")]
        public async Task<IEnumerable<historyDto>> GetAll(string user)
        {
            return await _historyQueryService1.GethistoryUserAsync(user);
        }
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
        [HttpPost]
        public async Task<IActionResult> Add([FromForm]historyCreateCommand command)
        {
            var validation = _historyValidator.Validate(command);
            if (validation.IsValid)
            {
                await _mediator.Publish(command);
            return Ok();
        }
            return Ok(validation.Errors);

    }
        [HttpPut]
        public async Task<IActionResult> Update(historyUpdateCommand command)
        {
            await _mediator.Publish(command);
            return Ok();
        }


    }
}
