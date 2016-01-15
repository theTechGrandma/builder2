using DSTBuilder.Models;
using System.Collections.Generic;
using System.Web.Http;

namespace DSTBuilder.Controllers
{
    public class XmlController : ApiController
    {
        static readonly IXmlRepository XmlRepository = new XmlRepository();
        
        [ActionName("DefaultAction")]
        public IEnumerable<Server> GetServers(string product, string release)
        {
            return XmlRepository.GetServers(product, release);
        }

         [Route("xml/{product}/server")]
        public IEnumerable<Server> GetServer(string product, string release, string serverName)
        {
            return XmlRepository.GetServer(product, release, serverName);
        }

        [Route("xml/{product}/release")]
        public IEnumerable<Versions> GetRelease(string product)
        {
            return XmlRepository.GetRelease(product);
        }

        [Route("xml/{product}/version")]
        public IEnumerable<Versions> GetVersion(string product, string release)
        {
            return XmlRepository.GetVersion(product, release);
        }

        [Route("xml/{product}/path")]
        public IEnumerable<Path> GetPath(string product, string release)
        {
            return XmlRepository.GetPaths(product, release);
        }

        [ActionName("DefaultAction")]
        public IEnumerable<Product> GetProducts()
        {
            return XmlRepository.GetProducts();
        }

        [Route("xml/{product}/services")]
        public IEnumerable<Services> GetServices(string product, string release)
        {
            return XmlRepository.GetServices(product, release);
        }

        [Route("xml/{product}/locations")]
        public IEnumerable<Location> GetLocations(string product, string release, string serverName)
        {
            return XmlRepository.GetLocations(product, release, serverName);
        }

        [Route("xml/{product}/xmlConfig")]
        public bool ChangeXMLConfigs(string fileToChange, string replaceNodeWith, string nodeToFind, string nodeToFindValue, string xmlNode)
        {
            return XmlRepository.ChangeXMLConfigs(fileToChange, replaceNodeWith, nodeToFind, nodeToFindValue, xmlNode);
        }

        [Route("xml/{product}/getEmailGroup")]
        public string GetEmailGroup(string product)
        {
            return XmlRepository.GetEmailGroup(product);
        }

        [Route("xml/{product}/getOracleEmailGroup")]
        public string GetOracleEmailGroup(string product)
        {
            return XmlRepository.GetOracleEmailGroup(product);
        }

        [Route("xml/{product}/setProductVersion")]
        public bool SetProductVersion(string product, string release, string version)
        {
            return XmlRepository.SetProductVersion(product, release, version);
        }
    }

        
}
