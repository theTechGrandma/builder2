using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace DSTBuilder.Models
{
    public class XmlRepository : IXmlRepository
    {        
        private readonly string _xmlDatabase = ConfigurationManager.AppSettings["xmlLocation"];
        //private readonly string _xmlDatabase = HttpContext.Current.Server.MapPath("~/App_Data/ProductConfig2.xml");
        private readonly BuildRepository _buildRepository = new BuildRepository();

        public string Product
        {
            get { return _buildRepository.Product; }
        }

        public IEnumerable<Versions> GetRelease(string product)
        {
            //product = Product;
            //List for dropdown for choosing version to build
            XElement xml = XElement.Load(_xmlDatabase);
            var versions = xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product);

            return versions.Descendants("Version").Select(element => new Versions
            {
                Version = ((string)element.Attribute("Version")),
                Release = ((string)element.Attribute("Release"))
            }).ToList();
        }

        public IEnumerable<Product> GetProducts()
        {
            var root = XElement.Load(_xmlDatabase);
            var product = from el in root.Descendants("Product")
                select el;
            var products = product.Select(el => new Product {Name = ((string) el.Attribute("Name"))}).ToList();
            return products;
        }

        public IEnumerable<Path> GetPaths(string product, string release)
        {
            var paths = new List<Path>();
            var xml = XElement.Load(_xmlDatabase);
            var buildProduct = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                                                 select el;

            foreach (var version in buildProduct.Descendants("Version").Where(e => e.Attribute("Release").Value == release).ToList())
            {
                paths.AddRange(version.Descendants("Path").Select(path => new Path
                {
                    Name = (string)path.Attribute("Name"),
                    Location = (string)path.Attribute("Location")
                }));
            }

            return paths;
        }

        public IEnumerable<Server> GetServers(string product, string release)
        {
            var servers = new List<Server>();
            var xml = XElement.Load(_xmlDatabase);
            var buildProduct = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                               select el;

            foreach (var version in buildProduct.Descendants("Version").Where(e => e.Attribute("Release").Value == release).ToList())
            {
                servers.AddRange(version.Descendants("Server").Select(server => new Server
                {
                    Name = (string)server.Attribute("Name")
                }));
            }

            return servers;
        }

        public IEnumerable<Server> GetServer(string product, string release, string serverName)
        {
            var servers = new List<Server>();
            var xml = XElement.Load(_xmlDatabase);
            var buildProduct = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                                                 select el;

            foreach (var version in buildProduct.Descendants("Version").Where(e => e.Attribute("Release").Value == release).ToList())
            {
                servers.AddRange(version.Descendants("Server").Where(e => e.Attribute("Name").Value == serverName).Select(server => new Server
                {
                    Name = (string) server.Attribute("Name")
                }));
            }

            return servers;
        }

        public IEnumerable<Versions> GetVersion(string product, string release)
        {
            var xml = XElement.Load(_xmlDatabase);
            var versions = xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product);
            return versions.Descendants("Version").Where(e => e.Attribute("Release").Value == release).Select(element => new Versions
            {
                Version = ((string) element.Attribute("Version")), 
                Release = ((string) element.Attribute("Release")), 
                LastBuildDateTime = ((string) element.Attribute("LastBuildDateTime")),
                SiteUrl = ((string)element.Attribute("SiteUrl")),
                SolutionFile = ((string)element.Attribute("SolutionFile"))
            }).ToList();
        }

        public IEnumerable<Location> GetLocations(string product, string release, string serverName)
        {
            var locations = new List<Location>();
            var xml = XElement.Load(_xmlDatabase);
            var buildProduct = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                                                 select el;

            foreach (var version in buildProduct.Descendants("Version").Where(e => e.Attribute("Release").Value == release).ToList())
            {
                foreach (var server in version.Descendants("Server").Where(e => e.Attribute("Name").Value == serverName))
                {
                    locations.AddRange(server.Descendants("Location").Select(location => new Location
                    {
                        Source = ((string) location.Attribute("Source")), 
                        SharePath = ((string) location.Attribute("SharePath")), 
                        Name = ((string) location.Attribute("Name"))
                    }));
                }
            }

            return locations;
        }

        public IEnumerable<Services> GetServices(string product, string release)
        {
            var services = new List<Services>();
            var xml = XElement.Load(_xmlDatabase);
            var buildProduct = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                                                 select el;

            foreach (var element in buildProduct.Descendants("Version").Where(e => e.Attribute("Release").Value == release).ToList())
            {
                services.AddRange(element.Descendants("Service").Select(service => new Services
                {
                    Name = ((string) service.Attribute("Name")), 
                    Server = ((string) service.Attribute("Server")), 
                    ServiceName = ((string) service.Attribute("ServiceName")),
                }));
            }

            return services;
        }

        public bool ChangeXMLConfigs(string fileToChange, string replaceNodeWith, string nodeToFind, string nodeToFindValue, string xmlNode)
        {
            if (File.Exists(fileToChange))
            {
                var xDoc = new XmlDocument();
                xDoc.Load(fileToChange);
                var xmlNodeList = xDoc.SelectNodes(nodeToFind);
                if (xmlNodeList != null)
                    foreach (XmlNode xNode in xmlNodeList.Cast<XmlNode>().Where(xNode => xNode.Attributes != null && xNode.Attributes[xmlNode].Value == nodeToFindValue))
                    {
                        xNode.Attributes["value"].Value = replaceNodeWith;
                    }

                xDoc.Save(fileToChange);
                return true;
            }
            else { return false; }
        }

        public string GetEmailGroup(string product)
        {
            var xdoc = XElement.Load(_xmlDatabase);
            return (string)(from el in xdoc.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                select el.Element("EmailGroup")).FirstOrDefault();
        }

        public string GetOracleEmailGroup(string product)
        {
            var xdoc = XElement.Load(_xmlDatabase);
            return (string)(from el in xdoc.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
                            select el.Element("OracleEmailGroup")).FirstOrDefault();
        }

        public bool SetProductVersion(string product, string release, string version)
        {
            var today = DateTime.Now.ToString(CultureInfo.CurrentCulture);

            if (!File.Exists(_xmlDatabase)) return true;
            var xDoc = new XmlDocument();
            xDoc.Load(_xmlDatabase);

            var xmlNodeList = xDoc.SelectNodes("Configuration/Products/Product");
            if (xmlNodeList != null)
                foreach (var buildversion in (from XmlNode productNode in xmlNodeList where productNode.Attributes != null && !string.IsNullOrEmpty(productNode.Attributes["Name"].Value) where productNode.Attributes["Name"].Value.Contains(product) from XmlNode buildversion in productNode.ChildNodes where buildversion.Name == "Version" where buildversion.Attributes != null && buildversion.Attributes["Release"].Value.StartsWith(release) select buildversion).Where(buildversion => buildversion.Attributes != null && buildversion.Attributes["Version"].Value != version))
                {
                    if (buildversion.Attributes == null) continue;
                    buildversion.Attributes["Version"].Value = version;
                    buildversion.Attributes["LastBuildDateTime"].Value = today;
                }
            xDoc.Save(_xmlDatabase);
            xDoc = null;
            return true;
            }

        //public string GetLastBuild(string product, string release)
        //{
        //    var xdoc = XElement.Load(_xmlDatabase);
        //    var _buildProduct = from el in xdoc.Descendants("Product").Where(e => e.Attribute("Name").Value == product)
        //                        from l in el.Descendants("Version").Where(e => e.Attribute("Release").Value == release)
        //                        select new {stuff = l.Descendants("LastBuildDateTime")};

        //    return _buildProduct.ToString();

        //}
        }
    }
