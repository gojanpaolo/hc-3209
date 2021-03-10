using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApplication
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddGraphQLServer()
                .AddSorting<CustomSortConvention>()
                .AddQueryType<Query>()
                .AddType<FooSortInputType>()
                .AddType<FooBarSortInputType>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(_ => _.MapGraphQL());
        }
    }

    public class Query
    {
        public Foo GetFoo() => default;
    }

    public class Foo
    {
        public int Id { get; set; }
    }

    public class FooSortInputType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .Field("bar")
                .Type<FooBarSortInputType>();
        }
    }

    public class CustomSortConvention : SortConvention
    {
        protected override void Configure(ISortConventionDescriptor descriptor)
        {
            descriptor.AddDefaults();

            descriptor
                .AddProviderExtension(
                    new QueryableSortProviderExtension(_ => _
                        .AddFieldHandler(new FooBarSortHandler())));
        }
    }

    public class FooBarSortInputType : SortInputType
    {
        protected override void Configure(ISortInputTypeDescriptor descriptor)
        {
            descriptor.Name("FooBarSortInput");
            descriptor.Field("id").Type<NonNullType<StringType>>();
            descriptor.Field("sort").Type<DefaultSortEnumType>();
        }
    }

    public class FooBarSortHandler : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            typeDefinition is SortInputTypeDefinition { Name: { Value: "FooBarSortInput" } };
    }
}
