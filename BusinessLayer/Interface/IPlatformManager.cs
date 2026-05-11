using BusinessLayer.Enum;

namespace BusinessLayer.Interface;

/// <summary>
/// Defines a contract for retrieving virtualization adapters for specific platform types.
/// </summary>
/// <remarks>Implementations of this interface provide access to platform-specific virtualization functionality by
/// returning the appropriate adapter for a given platform type. This interface is typically used to abstract platform
/// differences in virtualization scenarios.</remarks>
public interface IPlatformManager
{
    /// <summary>
    /// Retrieves a virtualization adapter instance for the specified platform type.
    /// </summary>
    /// <remarks>Use this method to obtain an adapter that provides virtualization operations for the given
    /// platform. The returned adapter may vary in capabilities depending on the platform type specified.</remarks>
    /// <param name="platformType">The platform type for which to obtain the virtualization adapter.</param>
    /// <returns>An instance of <see cref="IVirtualizationAdapter"/> that supports the specified platform type.</returns>
    IVirtualizationAdapter GetAdapter(PlatformType platformType);
}