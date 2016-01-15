using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public interface IXmlRepository
    {
        IEnumerable<Server> GetServers(string product, string release);
        IEnumerable<Server> GetServer(string product, string release, string serverName);
        IEnumerable<Versions> GetRelease(string product);
        IEnumerable<Versions> GetVersion(string product, string version);
        IEnumerable<Product> GetProducts();
        IEnumerable<Services> GetServices(string product, string release);
        IEnumerable<Location> GetLocations(string product, string release, string serverName);
        IEnumerable<Path> GetPaths(string product, string release);
        bool ChangeXMLConfigs(string fileToChange, string replaceNodeWith, string nodeToFind, string nodeToFindValue, string xmlNode);
        string GetEmailGroup(string product);
        string GetOracleEmailGroup(string product);
        bool SetProductVersion(string product, string release, string version);
    }
}