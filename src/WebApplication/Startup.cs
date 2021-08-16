using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace WebApplication
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddPooledDbContextFactory<MedicalDbContext>(_ => _.UseSqlServer("server=(localdb)\\mssqllocaldb;database=test"));
            //services.AddDbContext<MedicalDbContext>(_ => _.UseSqlServer("server=(localdb)\\mssqllocaldb;database=test"));

            AddGraphQL(services);
        }

        public static IRequestExecutorBuilder AddGraphQL(IServiceCollection services) =>
            services
                .AddGraphQLServer()
                .AddFiltering()
                .AddSorting<CustomSortConvention>()
                .AddQueryType<Query>()
                .AddType<Chilli>()
                .AddType<Cream>()
                .AddUnionType<Food>()
                .AddType<FooSortInputType>();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //using var ctx = new MedicalDbContext(
            //    new DbContextOptionsBuilder<MedicalDbContext>()
            //    .UseSqlServer("server=(localdb)\\mssqllocaldb;database=test")
            //    .Options);
            //ctx.Database.EnsureDeleted();
            //ctx.Database.EnsureCreated();

            //ctx.Foo.Add(new Foo());
            //ctx.SaveChanges();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(_ => _.MapGraphQL());
        }
    }

    public class Query
    {
        [UseSorting]
        public IQueryable<Foo> GetFoo() => new[] { new Foo() }.AsQueryable();

        [UseFiltering]
        public IQueryable<Food> GetFood() => new Food[] { new Chilli { Id = 5 }, new Cream { Id = 9 } }.AsQueryable();
    }

    public abstract class Food
    {
        public int Id { get; set; }

        public string Color { get; set; }

        public string Taste { get; set; }

        public string Type => GetType().Name;
    }

    public class Chilli : Food
    {
        public int Schoville { get; set; }
    }

    public class Cream : Food
    {
    }

    public class Foo
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("foos")
                .UseDbContext<MedicalDbContext>()
                .Resolver((ctx, ct) =>
                {
                    return ((MedicalDbContext)ctx.LocalContextData[typeof(MedicalDbContext).FullName]).Foo;
                });
        }
    }

    public class FooSortInputType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(_ => _.Name);
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

    public class FooBarSortHandler : QueryableDefaultSortFieldHandler
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            typeDefinition is SortInputTypeDefinition { Name: { Value: "FooBarSortInput" } }
            || (fieldDefinition is SortFieldDefinition sortFieldDefinition
                && sortFieldDefinition.Type is ExtendedTypeReference extendedTypeReference
                && extendedTypeReference.Type.Type == typeof(FooBarSortInputType));

        public override bool TryHandleEnter(QueryableSortContext context, ISortField field, ObjectFieldNode node, [NotNullWhen(true)] out ISyntaxVisitorAction action)
        {
            if (field.Name == "id")
            {
                // in our actual code, this has some complex logic that ultimately returns a bool and we sort by `true/1` and `false/0`
                Expression<Func<Foo, bool>> expression = _ => true;
                var lastFieldSelector = (QueryableFieldSelector)context.GetInstance();
                var nextSelector = Expression.Invoke(expression, lastFieldSelector.Selector);
                context.PushInstance(lastFieldSelector.WithSelector(nextSelector));
                action = SyntaxVisitor.Break;
                return true;
            }

            action = SyntaxVisitor.Continue;
            return true;
        }
    }

    public class MedicalDbContext : DbContext
    {
        public MedicalDbContext(DbContextOptions<MedicalDbContext> options) : base(options) { }

        public DbSet<Foo> Foo { get; set; }
    }
}
