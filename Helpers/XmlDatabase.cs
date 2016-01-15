using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DSTBuilder.Models;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace DSTBuilder.Helpers
{
    public class XmlDatabase
    {
        private string _xmlDatabase = @"C:\ProductConfigurations.xml";


        public string _queue = @"C:\_Queue";
        //private Email _pmEmail = new Email();

        #region Selects
        public IEnumerable<Versions> GetVersions(string product)
        {
            List<Versions> majorMinorVersions = new List<Versions>();
            var xml = XElement.Load(_xmlDatabase);
            IEnumerable<XElement> versions = xml.Descendants("Product").Where(e => e.Attribute("Name").Value == "DST");

            foreach (XElement element in versions.Descendants("Version"))
            {
                majorMinorVersions.Add(new Versions
                {
                    Version = ((string)element.Attribute("Version")),
                    MajorMinor = ((string)element.Attribute("MajorMinor")),
                    CoreVersion = ((string)element.Attribute("CoreVersion"))
                });
            }

            return majorMinorVersions;

        }
        public IEnumerable<Product> GetProducts()
        {
            List<Product> products = new List<Product>();
            XElement root = XElement.Load(_xmlDatabase);
            IEnumerable<XElement> product =
                from el in root.Descendants("Product")
                select el;
            foreach (XElement el in product)
            {
                products.Add(new Product { Name = ((string)el.Attribute("Name")) });
            };

            return products;
        }

        public IEnumerable<Server> GetServers(string product, string majorMinor)
        {

            List<Server> servers = new List<Server>();
            var xml = XElement.Load(_xmlDatabase);
            IEnumerable<XElement> server = from el in xml.Descendants("Product").Where(e => e.Attribute("Name").Value == "DST")
            select el;

            foreach (XElement element in server.Descendants("Server"))
            {
                foreach (XElement result in element.Descendants())
                {
                    foreach (var result2 in result.Descendants("Version").Where(e => e.Attribute("MajorMinor").Value != "4.0").ToList())
                        result2.Remove();
                    foreach (var final in result.Descendants("Server"))
                    {
                        servers.Add(new Server
                        {
                            Name = ((string)element.Attribute("Name")),
                            IP = ((string)element.Attribute("IP")),
                            ServerURL = ((string)element.Attribute("ServerURL"))
                        });
                    }
                }

                return servers;

            }
            return null;
        }
        #endregion Selects

        #region Add/Delete
        public void AddToQueue(string product, string productVersion, string majorMinor, string startTime, bool skipGetSource)
        {
            // Declare XML
            XmlDocument xmlDocument = new XmlDocument();
            XmlDeclaration declaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDocument.AppendChild(declaration);

            // Create <Configuration> Node
            XmlElement rootElement = xmlDocument.CreateElement("Configuration");
            xmlDocument.AppendChild(rootElement);

            // Create <Queue> Node
            XmlElement queueElement = xmlDocument.CreateElement("Queue");
            xmlDocument.ChildNodes.Item(1).AppendChild(queueElement);

            // Create <Product> Node
            XmlNode productNode = xmlDocument.CreateNode(XmlNodeType.Element, "Product", null);
            xmlDocument.ChildNodes.Item(1).ChildNodes.Item(0).AppendChild(productNode);

            // Create all the attributes for the queue
            AddXMLAttribute(xmlDocument, productNode, "Name", product);
            AddXMLAttribute(xmlDocument, productNode, "Version", productVersion);
            AddXMLAttribute(xmlDocument, productNode, "MajorMinor", majorMinor);
            AddXMLAttribute(xmlDocument, productNode, "StartTime", startTime);
            AddXMLAttribute(xmlDocument, productNode, "IsCancel", bool.TrueString);
            AddXMLAttribute(xmlDocument, productNode, "SkipGetSource", skipGetSource.ToString());

            bool isFileCreated = false;

            // try to create the file, if it exists it will keep trying to create the file until it can.
            while (!isFileCreated)
            {
                string dateTime = string.Format(@"{0:MM-dd-yyyy_hh-mm-ss-tt}", DateTime.Now);
                string queueFile = _queue + dateTime + ".txt";

                if (!File.Exists(queueFile))
                {
                    try
                    {
                        xmlDocument.Save(queueFile);
                        isFileCreated = true;
                    }
                    catch { }
                }

                System.Threading.Thread.Sleep(1000);
            }
        }

        public bool DeleteQueueItem(string product, string productVersion, string startTime)
        {
            try
            {
                List<string> productList = new List<string>();

                foreach (string file in Directory.GetFiles(_queue, "*.txt"))
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                //TODO
                //AddLog("Could not delete queue item!" + Environment.NewLine + e.Message.ToString());
                //Email(_pmEmail.GetEmail(product, true).ToString(), "Could not delete queue item!", LogReader().ToString());
                return false;
            }
        }

        private void AddXMLAttribute(XmlDocument xmlDocument, XmlNode parentNode, string attributeName, string attributeValue)
        {
            XmlAttribute xmlAttribute = xmlDocument.CreateAttribute(attributeName);
            xmlAttribute.Value = attributeValue;
            parentNode.Attributes.Append(xmlAttribute);
        }
        #endregion Add/Delete
    }
}