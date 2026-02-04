using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CustomerPortal.Shared.Egnyte;
using NUnit.Framework;

namespace CustomerPortal.Tests.Egnyte
{
    [TestFixture, Ignore("Very flaky, especially with rate limiting")]
    public class EgnyteServiceTests
    {
        private EgnyteService _egnyteService;
        private AppSettings _appSettings;

        [SetUp]
        public void Setup()
        {
            _appSettings = new AppSettings
            {
                EgnyteDomain = "savantatest",
                EgnyteClientId = "rtuyjyt7queuc3ybefsgawd9",
                EgnyteUsername = "integration.tech.team",
                EgnytePassword = "b$fDErH75MR8[\"V/(@/s(-",
                EgnyteRootFolder = "/Shared/Savanta/Service Assets/Customer Portal/"
            };

            _egnyteService = new EgnyteService(_appSettings.EgnyteDomain, _appSettings.EgnyteClientId, _appSettings.EgnyteUsername, _appSettings.EgnytePassword, _appSettings.EgnyteAccessToken);
        }

        [Test]
        public void Test_that_Egnyte_API_can_instantiate_safely_without_blowing_any_caps()
        {
            Assert.DoesNotThrow(() =>
            {
                var exceptions = new ConcurrentQueue<Exception>();

                Parallel.For(0, 1000, i =>
                {
                    try
                    {
                        var client = _egnyteService.GetClient().Result;
                    }
                    catch (Exception e)
                    {
                        exceptions.Enqueue(e);
                    }
                });

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            });
        }
        
        [Test]
        public void Test_that_Egnyte_API_can_retry_to_get_around_API_throttling()
        {
            Assert.DoesNotThrow(() =>
            {
                var exceptions = new ConcurrentQueue<Exception>();

                Parallel.For(0, 5, i =>
                {
                    try
                    {
                        var result = _egnyteService.ExecuteEgnyteCall(egnyteClient =>
                            egnyteClient.Files.ListFileOrFolder(_appSettings.EgnyteRootFolder)).Result;

                        Console.WriteLine(result.AsFolder.Name);
                    }
                    catch (Exception e)
                    {
                        exceptions.Enqueue(e);
                    }
                });

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            });
        }

        [Test]
        public void Test_that_can_access_test_folder()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                var result = await _egnyteService.ExecuteEgnyteCall(egnyteClient =>
                    egnyteClient.Files.ListFileOrFolder(_appSettings.EgnyteRootFolder));

                Console.WriteLine(result.AsFolder.Name);
            });

        }
    }
}
