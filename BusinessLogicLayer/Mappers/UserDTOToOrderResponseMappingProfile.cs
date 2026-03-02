using AutoMapper;
using BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Mappers;

public class UserDTOToOrderResponseMappingProfile : Profile
{
    public UserDTOToOrderResponseMappingProfile()
    {
        CreateMap<UserDTO, OrderResponse>()
            .ForMember(dest => dest.UserPersonName, opt => opt.MapFrom(src => src.PersonName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
    }
}
