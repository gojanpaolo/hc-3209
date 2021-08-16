using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using WebApplication;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var services = new ServiceCollection();
            var schema = await Startup.AddGraphQL(services).BuildSchemaAsync();
            var x = schema.Print();
        }
    }
}
