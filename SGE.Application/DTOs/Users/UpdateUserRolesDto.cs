namespace SGE.Application.DTOs.Users;

/// <summary>
/// Data transfer object for updating user roles.
/// </summary>
public class UpdateUserRolesDto
{
    /// <summary>
    /// Gets or sets the list of role names to assign to the user.
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string>();
}

