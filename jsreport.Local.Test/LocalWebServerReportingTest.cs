using jsreport.Shared;
using jsreport.Types;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Shouldly;
using System.Threading.Tasks;
using jsreport.Binary;

namespace jsreport.Local.Test
{
    [TestFixture] 
    [SingleThreaded]
    public class LocalWebServerReportingTest
    {
        private ILocalWebServerReportingService _rs;

        [SetUp]
        public async Task SetUp()
        {
            _rs = new LocalReporting().UseBinary(JsReportBinary.GetStream()).AsWebServer().Create();
            await _rs.StartAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _rs.KillAsync();
        }        

        [Test]
        public async Task TestWebServerRender()
        {
            var result = await _rs.RenderAsync(new RenderRequest()
            {
                Template = new Template()
                {
                    Content = "Hello world",
                    Recipe = Recipe.Html,
                    Engine = Engine.Handlebars
                }
            });

            new StreamReader(result.Content).ReadToEnd().ShouldBe("Hello world");
        }
    }
}
