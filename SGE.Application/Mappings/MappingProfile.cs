using AutoMapper;
using SGE.Application.DTOs;
using SGE.Application.DTOs.Employees;
using SGE.Core.Entities;

namespace SGE.Application.Mappings;

/// <summary>
/// AutoMapper profile to define mapping rules between DTOs and entities.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Department mappings
        CreateMap<Department, DepartmentDto>();
        CreateMap<DepartmentCreateDto, Department>();
        CreateMap<DepartmentUpdateDto, Department>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.DepartmentName, opt =>
                opt.MapFrom(src => src.Department.Name));
        CreateMap<EmployeeCreateDto, Employee>();
        CreateMap<EmployeeUpdateDto, Employee>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember)
                => srcMember != null)); // ignore nulls
    }
}