using AutoMapper;
using Impodatos.Domain;
using Impodatos.Services.Queries.DTOs;

namespace ImpodatosCommon
{
    public class AutomapperProfile : Profile
    {     
        public AutomapperProfile()
        {
            CreateMap<History, HistoryDto>();
            CreateMap<HistoryDto, History>();
   
        }
    }
}
