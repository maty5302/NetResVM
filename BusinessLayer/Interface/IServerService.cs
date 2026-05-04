using BusinessLayer.DTOs;
using BusinessLayer.Models;
using BusinessLayer.Enum;

namespace BusinessLayer.Interface;

public interface IServerService
{
    List<ServerDTO>? GetAllServers();
    ServerDTO? GetServerById(int id);
    //ServerModel? GetServerByIdInternal(int id);
    (string Url, string Username, string Password)? GetServerCredentials(int id);
    PlatformType GetServerType(int id);
    bool ServerExists(int id);
    bool InsertServer(ServerModel server);
    bool UpdateServer(ServerModel server);
    bool RemoveServer(int id);
    Task<bool> IsServerOnlineAsync(string ipAddress);
}