using Application.Common.CQS.Queries;
using Application.Common.Security;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class QueryContext : DataContext, IQueryContext, IBranchScopeBypassQuery
{
    public QueryContext(DbContextOptions<QueryContext> options, IOperatorContext operatorContext)
        : base(options, operatorContext)
    {
    }

    public new IQueryable<T> Set<T>() where T : class
    {
        return base.Set<T>();
    }

    public IQueryable<SubscriberProfile> SubscriberProfilesIgnoringBranchScope =>
        SubscriberProfile.IgnoreQueryFilters().Where(p => !p.IsDeleted);

    public IQueryable<MsisdnAsset> MsisdnAssetsIgnoringBranchScope =>
        MsisdnAsset.IgnoreQueryFilters().Where(m => !m.IsDeleted);

    public IQueryable<TelecomSubscription> TelecomSubscriptionsIgnoringBranchScope =>
        TelecomSubscription.IgnoreQueryFilters().Where(s => !s.IsDeleted);
}
