using jsreport.Shared;
using jsreport.Types;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Shouldly;
using System.Threading.Tasks;
using jsreport.Binary;
using Newtonsoft.Json.Serialization;

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
            Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "jsreportCopyAlways"));
               _rs = new LocalReporting()
                .UseBinary(JsReportBinary.GetBinary())
                .RunInDirectory(Path.Combine(Directory.GetCurrentDirectory(), "jsreportCopyAlways"))                
                .Configure((cfg) => cfg.FileSystemStore())
                .AsWebServer()                
                .Create();
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


        [Test]
        public async Task TestWebServerRenderWithName()
        {
            var result = await _rs.RenderAsync(new RenderRequest()
            {
                Template = new Template()
                {
                   Name = "test"
                }
            });

            result.Meta.ContentType.ShouldBe("application/pdf");
        }
    }

        [TestFixture]
        [SingleThreaded]
        public class LocalWebServerReportingWithCustomDataContractResolberTest
        {
            private ILocalWebServerReportingService _rs;

            [SetUp]
            public async Task SetUp()
            {
                _rs = new LocalReporting().UseContractResolverForDataProperty(new CamelCasePropertyNamesContractResolver()).UseBinary(JsReportBinary.GetBinary()).AsWebServer().Create();
                await _rs.StartAsync();
            }

            [TearDown]
            public async Task TearDown()
            {
                await _rs.KillAsync();
            }

            [Test]
            public async Task TestDataSerializeWithCamelCase()
            {
                var result = await _rs.RenderAsync(new RenderRequest()
                {
                    Template = new Template()
                    {
                        Content = "{{helloWorld}}",
                        Recipe = Recipe.Html,
                        Engine = Engine.Handlebars
                    },
                    Data = new
                    {
                        HelloWorld = "foo"    
                    }
                });

                new StreamReader(result.Content).ReadToEnd().ShouldBe("foo");
            }
        }
}
