using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace BusinessLayer.Interface;

public interface IVirtualizationAdapter
{
    PlatformType PlatformName { get; }
    
    Task<(bool Valid, string Message)> AuthenticateAsync(int id);
    //Task<HttpRequestMessage> LogoutAsync();

    Task<(List<LabDTO>? Labs, string Message)> GetLabsAsync(int serverId);


    Task<(LabDTO? Lab, string Message)> GetLabInfoAsync(int serverId, string labId);
    
    Task<bool> ImportLab(int serverId, byte[] fileContent, string? filename = null);
    
    Task<bool> ImportLab(int serverId, IFormFile file);
    
    Task<(byte[]? FileContent, string ContentType, string FileName, string Message)> DownloadLab(int serverId, string? labId, LabDTO? lab = null);
    
    Task<(bool value, string message)> DeleteLab(int serverId, string labId);
    
    Task<(bool value, string message)> StartLabAsync(int serverId, string labId);
    
    Task<(bool value, string message)> StopLabAsync(int serverId, string labId);

    Task<(List<NodeDTO>? Nodes, string Message)> GetAllNodes(int serverId, string labId);

}