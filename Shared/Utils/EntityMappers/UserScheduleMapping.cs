using AutoMapper;
using TBD.API.DTOs;
using TBD.API.DTOs.Users;
using TBD.ScheduleModule.Models;

namespace TBD.Shared.Utils.EntityMappers;

public class UserScheduleMapping : Profile
{
    public UserScheduleMapping()
    {
        CreateMap<UserSchedule, Schedule>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.DaysWorkedJson, opt => opt.MapFrom(src => src.DaysWorkedJson))
            .ForMember(dest => dest.TotalHoursWorked, opt => opt.MapFrom(src => src.TotalHoursWorked))
            .ForMember(dest => dest.BasePay, opt => opt.MapFrom(src => src.BasePay))
            .ForMember(dest => dest.Overtime, opt => opt.Ignore()) // Example: Ignore or customize mappings
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null)); // Non-null check
    }
}
