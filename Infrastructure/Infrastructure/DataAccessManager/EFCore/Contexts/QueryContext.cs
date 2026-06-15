using Application.Common.CQS.Queries;
using Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class QueryContext : DataContext, IQueryContext
{
    public QueryContext(DbContextOptions<QueryContext> options, IOperatorContext operatorContext)
        : base(options, operatorContext)
    {
    }

    public new IQueryable<T> Set<T>() where T : class
    {
        return base.Set<T>();
    }
}
