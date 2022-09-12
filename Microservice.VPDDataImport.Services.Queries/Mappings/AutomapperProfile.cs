using AutoMapper;
using Microservice.VPDDataImport.Domain;
using Microservice.VPDDataImport.Services.Queries.DTOs;

namespace Microservice.VPDDataImport.Services.Queries.Mappings
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
