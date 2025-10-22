using AutoMapper;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Mappings
{
    /// <summary>
    /// Profile do AutoMapper para Routine
    /// </summary>
    public class RoutineProfile : Profile
    {
        public RoutineProfile()
        {
            CreateMap<Routine, RoutineDto>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Actions, opt => opt.MapFrom(src => src.Actions ?? new List<string>()));
        }
    }
}
