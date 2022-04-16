using AutoMapper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models;
using Xentegra.Models.DTO;

namespace Xentegra.Extensions
{
    public class AutoMapperProfile: Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<DemoRequestDTO, DemoRequest>();
            CreateMap<DemoRequest, DemoRequestDTO>();

            CreateMap<Technology, TechnologyDTO>();
            CreateMap<TechnologyDTO, Technology>();
        }
    }
}
