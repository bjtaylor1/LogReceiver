using AutoMapper;
using System;

namespace LogReceiver
{
    public class Mapping
    {
        public static Lazy<Mapper> Mapper { get; } = new Lazy<Mapper>(() =>
        {
            return new Mapper(GetConfiguration());
        });

        public static MapperConfiguration GetConfiguration()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
        }
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LoggerNode, LoggerNodeModel>();
            CreateMap<LoggerNodeModel, LoggerNode>();
        }
    }
}
