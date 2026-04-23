using System.Data;
namespace DataLayer.Interface;

public interface IServerTableDataGateway
{
    DataTable GetAllServers();
    DataTable GetServerById(int id);
    void InsertServer(string serverType, string name, string ipAddress, string username, string password);
    void UpdateServer(int id, string serverType, string name, string ipAddress, string username, string password);
    void RemoveServer(int id);
}