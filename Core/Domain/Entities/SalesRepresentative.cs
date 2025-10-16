using Domain.Common;

namespace Domain.Entities;

public class SalesRepresentative : BaseEntity
{
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Description { get; set; }
    public string? SalesTeamId { get; set; }
    public SalesTeam? SalesTeam { get; set; }

}
