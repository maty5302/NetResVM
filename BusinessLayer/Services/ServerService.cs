using DataLayer;
using BusinessLayer;
using BusinessLayer.Models;
using BusinessLayer.MapperDT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleLogger;
using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public class ServerService
    {
        private readonly ServerTableDataGateway _gateway;
        private static ILogger _logger = FileLogger.Instance;

        public ServerService()
        {
            _gateway = new ServerTableDataGateway();
        }
        public List<ServerDTO>? GetAllServers()
        {
            try
            {
                var servers = new List<ServerDTO>();
                var table = _gateway.GetAllServers();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    servers.Add(ServerMapper.MapToDTO(row));
                }
                return servers;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }
        public ServerDTO? GetServerById(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return ServerMapper.MapToDTO(table.Rows[0]);
            }
            catch
            {
                _logger.LogError($"Server with id {id} not found.");
                return null;
            }
        }
        internal ServerModel? GetServerByIdInternal(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return ServerMapper.Map(table.Rows[0]);
            }
            catch
            {
                _logger.LogError($"Server with id {id} not found.");
                return null;
            }
        }

        public bool ServerExists(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return table.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }
        public bool InsertServer(ServerModel server)
        {
            try
            {
                var servers = GetAllServers();
                if (servers != null) {
                    foreach (var item in servers)
                    {
                        if (item.Name == server.Name)
                        {
                            _logger.LogWarning($"Server with name {server.Name} already exists.");
                            return false;
                        }
                    }
                }
                else
                {
                    _logger.LogError("Couldn't fetch servers from database.");
                    return false;
                }
            }
            catch
            {
                return false;
            }
            try
            {
                string ip = new Uri(server.IpAddress).Host;
                _gateway.InsertServer(server.ServerType, server.Name, ip, server.Username, server.Password);
                _logger.Log($"Server with name {server.Name} has been inserted.");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server with name {server.Name} couldn't be inserted. {e.Message}");
                return false;
            }
        }

        public bool UpdateServer(ServerModel server)
        {
            var serverToUpdate = GetServerByIdInternal(server.Id);
            if (serverToUpdate == null)
            {
                return false;
            }
            if (server.Password == null)
            {
                server.Password = serverToUpdate.Password;
            }
            try
            {
                _gateway.UpdateServer(server.Id, server.ServerType, server.Name, server.IpAddress, server.Username, server.Password);
                _logger.Log($"Server with id {server.Id} has been updated.");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server with id {server.Id} couldn't be updated. {e.Message}" );
                return false;
            }
        }

        public bool RemoveServer(int id)
        {
            var server = GetServerById(id);
            if (server == null)
            {
                return false;
            }
            try
            {
                _gateway.RemoveServer(id);
                _logger.Log($"Server with id {id} has been removed.");
                return true;
            }
            catch(Exception e)
            {
                _logger.LogError($"Server with id {id} couldn't be removed. {e.Message}");
                return false;
            }
        }
    }
}
