using Application.Common.CQS.Commands;
using Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;
public class CommandContext : DataContext, ICommandContext
{
    public CommandContext(DbContextOptions<CommandContext> options, IOperatorContext operatorContext)
        : base(options, operatorContext)
    {
    }
}
