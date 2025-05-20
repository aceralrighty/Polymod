using AutoMapper;
using TBD.Models.Entities;

namespace TBD.Models.DTOs.UserScheduleMappingProfile;

public class UserScheduleMapping : Profile
{
    public UserScheduleMapping()
    {
        CreateMap<UserSchedule, Schedule>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        ;
    }
}