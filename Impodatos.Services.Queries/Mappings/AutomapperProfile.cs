using AutoMapper;
using Impodatos.Domain;
using Impodatos.Services.Queries.DTOs;

namespace Impodatos.Services.Queries.Mappings
{
    public class AutomapperProfile : Profile
    {     
        public AutomapperProfile()
        {
            CreateMap<history, historyDto>();
            CreateMap<historyDto, history>();
        }
    }
}
