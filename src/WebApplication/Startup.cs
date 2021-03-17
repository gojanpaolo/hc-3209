using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

public record Foo(int FooId);

public record BarService(int BarFooId = 2);

public class Startup
{
    public void ConfigureServices(IServiceCollection _) => _
        .AddHttpContextAccessor()
        .AddScoped<BarService>()
        .AddGraphQLServer()
        .AddFiltering(_ => _.AddDefaults().AddProviderExtension(new QueryableFilterProviderExtension(_ => _
            .AddFieldHandler<FooBarFilterFieldHandler>())))
        .AddQueryType<Query>();

    public void Configure(IApplicationBuilder app) => app.UseRouting().UseEndpoints(_ => _.MapGraphQL());
}

public class Query
{
    [UseFiltering(typeof(FooFilterInputType))]
    public Foo[] GetFoos() => new[] { new Foo(1), new Foo(2), new Foo(3) };
}

public class FooFilterInputType : FilterInputType<Foo>
{
    protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor) => descriptor.Field("bar").Type<BooleanOperationFilterInputType>();
}

public class FooBarFilterFieldHandler : FilterFieldHandler<QueryableFilterContext, Expression>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FooBarFilterFieldHandler(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public override bool CanHandle(ITypeCompletionContext context, IFilterInputTypeDefinition typeDefinition, IFilterFieldDefinition fieldDefinition) => fieldDefinition is FilterFieldDefinition { Name: { Value: "bar" } };

    public override bool TryHandleEnter(QueryableFilterContext context, IFilterField field, ObjectFieldNode node, out ISyntaxVisitorAction action)
    {
        var barService = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<BarService>();
        var fooId = Expression.Property(context.GetInstance(), nameof(Foo.FooId));
        context.PushInstance(FilterExpressionBuilder.Equals(fooId, barService.BarFooId));
        action = SyntaxVisitor.Continue;
        return true;
    }
}
