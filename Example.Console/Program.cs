using System;
using System.ServiceModel;
using FeatureFlagsService;

namespace Example.Console
{
    class Program
    {
        public static RSFT.Core.FeatureFlagService FeatureFlagService { get; set; }

        private static int CacheTime = 15;

        static void Main(string[] args)
        {
            System.Console.WriteLine("Initializing FeatureFlag Service.");

            FeatureFlagService =
                new RSFT.Core.FeatureFlagService(
                    () => new FeatureRepositoryClient(RSFT.Core.FeatureFlagService.GetBindingForEndpoint(), 
                        new EndpointAddress("http://featureflags.boss.ebsco.com/Service/CentralizedFeatureRepository.svc")), 
                    "PROD", "EPOP", CacheTime);

            FeatureFlagService.Initialize();

            System.Console.WriteLine("Testing some flags...");
            WriteValuesOut();
            System.Console.WriteLine($"Sleeping cache time of {CacheTime}");
            System.Threading.Thread.Sleep(new TimeSpan(0,0,0,CacheTime));
            WriteValuesOut();

            System.Console.ReadKey();
        }

        static void WriteValuesOut()
        {
            System.Console.WriteLine("---------------------------");

            foreach (var item in FeatureFlagService.Features)
            {
                System.Console.WriteLine($"{item.Name} PROD Value: {FeatureFlagService.IsAvailable(item.Name)}");
            }

            System.Console.WriteLine("---------------------------");
        }
    }
}
