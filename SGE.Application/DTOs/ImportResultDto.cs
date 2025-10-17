namespace SGE.Application.DTOs;

public class ImportResultDto
{
    public int CreatedCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}
