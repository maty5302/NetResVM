using BusinessLayer.Enum;

namespace BusinessLayer.Interface;

public interface IPlatformManager
{
    IVirtualizationAdapter GetAdapter(PlatformType platformType);
}