using Microsoft.EntityFrameworkCore;
using Impodatos.Persistence.Database;
using Impodatos.Services.Queries.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Impodatos.Domain;
using AutoMapper;
using System.Linq;

namespace Impodatos.Services.Queries
{
    public interface IhistoryQueryService 
    {
        Task<IEnumerable<historyDto>> GetAllAsync();
        Task<IEnumerable<historyDto>> GethistoryUserAsync(string correo);
        //Task<AddTracketResultDto> RawImport(RawImport request);

        //Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token);
        //Task<IEnumerable<historyDto>> GetAllAsync01();
        //Task<IEnumerable<historyDto>> GethistoryUserAsync01(string correo);
    }
    public  class historyQueryService : IhistoryQueryService
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
        public async Task<IEnumerable<historyDto>> GethistoryUserAsync(string correo)
        {
            var result = await (from c in _context.history
                                where c.userlogin == correo
                                select new history
                                {
                                    id = c.id,
                                    uploads = c.uploads,
                                    deleted = c.deleted,
                                    programsid = c.programsid,
                                    jsonset = c.jsonset,
                                    jsonresponse = c.jsonresponse,
                                    state = c.state,
                                    userlogin = c.userlogin,
                                    fecha = c.fecha,
                                    file = c.file

                                }).ToListAsync();

            return _mapper.Map<IEnumerable<historyDto>>(result);
        }

        //public async Task<AddTracketResultDto> RawImport(RawImport request)
        //{
        //    var content = JsonConvert.SerializeObject(request);
        //    var result = await RequestHttp.CallMethod("dhis", "", content, "");
        //    return JsonConvert.DeserializeObject<AddTracketResultDto>(result);
        //}
      
        //public async Task<IEnumerable<historyDto>> GetAllAsync01()
        //{
        //    var result = await _context.history.ToListAsync();

        //    return _mapper.Map<IEnumerable<historyDto>>(result);
        //}
        //public async Task<IEnumerable<historyDto>> GethistoryUserAsync01(string correo)
        //{
        //    var result = await (from c in _context.history
        //                        where c.userlogin == correo
        //                        select new history
        //                        {
        //                            id = c.id,
        //                            uploads = c.uploads,
        //                            deleted = c.deleted,
        //                            programsid = c.programsid,
        //                            jsonset = c.jsonset,
        //                            jsonresponse = c.jsonresponse,
        //                            state = c.state,
        //                            userlogin = c.userlogin,
        //                            fecha = c.fecha,
        //                            file = c.file

        //                        }).ToListAsync();

        //    return _mapper.Map<IEnumerable<historyDto>>(result);
        //}
    }
}
