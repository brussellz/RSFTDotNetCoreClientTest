using System;
using System.Linq;
using FeatureFlagsService;

namespace RSFT.Core
{
    public class FeatureFlagService
    {
        private readonly Func<IFeatureRepository> _featureFlagFactoryFunc;

        private DateTime _lastUpdateTime;
        
        public string Tenant { get; private set; }

        public string FeaturePrefix { get; private set; }

        public int CacheTime { get; private set; }

        public Feature[] Features { get; private set; }

        private readonly object _lockObject = new object();

        public FeatureFlagService(Func<IFeatureRepository> featureFlagFactoryFunc, 
            string tenant, string featurePrefix, int cacheTime)
        {
            _featureFlagFactoryFunc = featureFlagFactoryFunc;
            Tenant = tenant;
            FeaturePrefix = featurePrefix;
            CacheTime = cacheTime;
            _lastUpdateTime = DateTime.MinValue;
        }

        public static System.ServiceModel.Channels.Binding GetBindingForEndpoint()
        {
            System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
            result.MaxBufferSize = int.MaxValue;
            result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
            result.MaxReceivedMessageSize = int.MaxValue;
            result.AllowCookies = true;
            return result;
        }

        private void RefreshCache()
        {
            if (DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds <= CacheTime && Features != null)
            {
                return;
            }

            var service = _featureFlagFactoryFunc.Invoke();
            var result = service.GetFeatureSettingsWithOptionsAsync(new GetFeatureSettingsRequest
            {
                FeatureNamePrefix = FeaturePrefix
            }).Result;

            lock (_lockObject)
            {
                Features = result;
                _lastUpdateTime = DateTime.Now;
            }
        }

        public void Initialize()
        {
            RefreshCache();
        }

        public bool IsAvailable(string featureName)
        {
            // Refresh our cache if needed
            RefreshCache();

            var result = Features
                .FirstOrDefault(f => f.Name.Equals(featureName))
                ?.SupportedTenants
                .Any(t => t.Equals(Tenant));

            return result.GetValueOrDefault(false);
        }
    }
}
