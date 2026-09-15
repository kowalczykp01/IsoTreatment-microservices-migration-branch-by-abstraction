using AutoMapper;
using IsoTreatmentProcessSupportAPI.Entities;
using IsoTreatmentProcessSupportAPI.Models;

namespace IsoTreatmentProcessSupportAPI
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateEntryDto, Entry>();

            CreateMap<Entry, EntryDto>();

            CreateMap<User, UserDto>();
        }
    }
}
