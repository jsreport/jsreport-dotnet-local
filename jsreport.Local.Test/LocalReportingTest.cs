using jsreport.Shared;
using jsreport.Types;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Shouldly;
using System.Threading.Tasks;


namespace jsreport.Local.Test
{
    [TestFixture]    
    public class LocalReportingTest
    {
        [Test]
        public async Task TestUtilityRender()
        {
            var rs = new LocalReporting().AsUtility().Create();

            var result = await TestRender(rs);

            new StreamReader(result.Content).ReadToEnd().ShouldBe("Hello world");            
        }

        [Test]
        public async Task TestWebServerRender()
        {
            var rs = new LocalReporting().AsWebServer().Create();
            await rs.StartAsync();

            var result = await TestRender(rs);

            new StreamReader(result.Content).ReadToEnd().ShouldBe("Hello world");
        }

        [Test]      
        [Ignore("Need to be fixed")]
        public void TestUtilityRenderSimultaneous()
        {
            var rs = new LocalReporting().AsUtility().Create();

            Parallel.ForEach(Enumerable.Range(0, 3), async (i) =>
            {
                var result = await TestRender(rs);

                new StreamReader(result.Content).ReadToEnd().ShouldBe("Hello world");
            });
        }

        private Task<Report> TestRender(IRenderService rs)
        {
            return rs.RenderAsync(new RenderRequest()
            {
                Template = new Template()
                {
                    Content = "Hello world",
                    Recipe = Recipe.Html,
                    Engine = Engine.Handlebars
                }
            });
        }
    }
}
