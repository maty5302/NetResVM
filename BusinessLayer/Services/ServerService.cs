using DataLayer;
using BusinessLayer;
using BusinessLayer.Models;
using BusinessLayer.MapperDT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class ServerService
    {
        private readonly ServerTableDataGateway _gateway;

        public ServerService()
        {
            _gateway = new ServerTableDataGateway();
        }
        public List<ServerModel> GetAllServers()
        {
            var servers = new List<ServerModel>();
            var table = _gateway.GetAllServers();
            foreach (System.Data.DataRow row in table.Rows)
            {
                servers.Add(ServerMapper.Map(row));
            }
            return servers;
        }
        public ServerModel GetServerById(int id)
        {
            var table = _gateway.GetServerById(id);
            return ServerMapper.Map(table.Rows[0]);
        }
        public bool InsertServer(ServerModel server)
        {
            foreach(var item in GetAllServers())
            {
                if (item.Name == server.Name)
                {
                    return false;
                }
            }

            string ip = new Uri(server.IpAddress).Host;
            _gateway.InsertServer(server.ServerType, server.Name, ip, server.Username, server.Password);
            return true;
        }
    }
}
