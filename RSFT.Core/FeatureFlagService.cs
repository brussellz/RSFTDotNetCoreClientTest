using System;
using System.Linq;
using FeatureFlagsService;

namespace RSFT.Core
{
    public class FeatureFlagService
    {
        private readonly Func<IFeatureRepository> _featureFlagFactoryFunc;

        private TimeSpan _lastUpdateTime;
		
		public string Tenant { get; private set; }

		public string FeaturePrefix { get; private set; }

		public int CacheTime { get; private set; }

		public Feature[] Features { get; private set; }

        private readonly object _lockObject = new object();

        public FeatureFlagService(Func<IFeatureRepository> featureFlagFactoryFunc, TimeSpan lastUpdateTime, string tenant, string featurePrefix, int cacheTime)
        {
            _featureFlagFactoryFunc = featureFlagFactoryFunc;
            _lastUpdateTime = lastUpdateTime;
            Tenant = tenant;
            FeaturePrefix = featurePrefix;
            CacheTime = cacheTime;
            _lastUpdateTime = TimeSpan.MinValue;
        }

        private void RefreshCache()
        {
            if (_lastUpdateTime.Seconds <= CacheTime && Features != null)
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
            }
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
