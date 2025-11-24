using AutoMapper;
using SGE.Application.DTOs;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Exceptions;

namespace SGE.Application.Services;

/// <summary>
/// Provides services to manage department-related operations.
/// Implements the <see cref="IDepartmentService"/> interface and uses the
/// <see cref="IDepartmentRepository"/> for data access.
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;

    public DepartmentService(IDepartmentRepository departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieves all department records asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of department data transfer objects.</returns>
    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _departmentRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<DepartmentDto>>(list);
    }

    /// <summary>
    /// Retrieves a department record by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the department.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A department data transfer object if found; otherwise, null.</returns>
    public async Task<DepartmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dept = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        return dept == null ? null : _mapper.Map<DepartmentDto>(dept);
    }

    /// <summary>
    /// Creates a new department record asynchronously.
    /// </summary>
    /// <param name="dto">The data transfer object containing the details of the department to be created.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The created department data transfer object.</returns>
    /// <exception cref="DuplicateDepartmentNameException">Thrown if the department name already exists.</exception>
    /// <exception cref="DuplicateDepartmentCodeException">Thrown if the department code already exists.</exception>
    public async Task<DepartmentDto> CreateAsync(DepartmentCreateDto dto, CancellationToken cancellationToken = default)
    {
        var existingName = await _departmentRepository.GetByNameAsync(dto.Name, cancellationToken);
        if (existingName != null)
            throw new DuplicateDepartmentNameException(dto.Name);

        var existingCode = await _departmentRepository.GetByCodeAsync(dto.Code, cancellationToken);
        if (existingCode != null)
            throw new DuplicateDepartmentCodeException(dto.Code);

        var entity = _mapper.Map<Department>(dto);
        await _departmentRepository.AddAsync(entity, cancellationToken);
        return _mapper.Map<DepartmentDto>(entity);
    }

    /// <summary>
    /// Updates an existing department record asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the department to update.</param>
    /// <param name="dto">The data transfer object containing updated information for the department.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A boolean value indicating whether the update was successful.</returns>
    public async Task<bool> UpdateAsync(int id, DepartmentUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        _mapper.Map(dto, entity);
        await _departmentRepository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    /// <summary>
    /// Deletes a department record by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the department to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        await _departmentRepository.DeleteAsync(entity.Id, cancellationToken);
        return true;
    }
}
