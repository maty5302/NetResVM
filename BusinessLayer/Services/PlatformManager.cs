using BusinessLayer.Enum;
using BusinessLayer.Interface;

namespace BusinessLayer.Services
{
    public class PlatformManager : IPlatformManager
    {
        private readonly IEnumerable<IVirtualizationAdapter> _adapters;

        /// <summary>
        /// Initializes a new instance of the PlatformManager class with the specified virtualization adapters.
        /// </summary>
        /// <param name="adapters">The collection of virtualization adapters to be managed. Cannot be null.</param>
        public PlatformManager(IEnumerable<IVirtualizationAdapter> adapters)
        { 
            _adapters = adapters;
        }

        /// <summary>
        /// Retrieves the virtualization adapter associated with the specified platform type.
        /// </summary>
        /// <param name="platformType">The platform type for which to retrieve the corresponding virtualization adapter.</param>
        /// <returns>An implementation of IVirtualizationAdapter that matches the specified platform type.</returns>
        /// <exception cref="Exception">Thrown if the specified platform type is unknown or not supported.</exception>
        public IVirtualizationAdapter GetAdapter(PlatformType platformType)
        {
            // Attempt to find the adapter that matches the specified platform type
            var targetAdapter = _adapters.FirstOrDefault(a => a.PlatformName == platformType);

            if (targetAdapter == null)
            {
                throw new Exception($"Unknown or unsupported platform: {platformType}");
            }

            return targetAdapter;
        }
    }
}
