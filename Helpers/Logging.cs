using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DSTBuilder.Helpers
{
    public class Logging
    {
        private string _xmlDocument = @"C:\_Queue\Messages.xml";

        public void CreateLogFile()
        {
            if (File.Exists(_xmlDocument))
            { File.Delete(_xmlDocument); }

            XmlDocument xmlDocument = new XmlDocument();
            XmlDeclaration declaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDocument.AppendChild(declaration);

            XmlElement rootElement = xmlDocument.CreateElement("BuildMessages");
            xmlDocument.AppendChild(rootElement);

            XmlElement queueElement = xmlDocument.CreateElement("Message");
            xmlDocument.ChildNodes.Item(1).AppendChild(queueElement);

            AddXMLAttribute(xmlDocument, queueElement, "Value", "BuildMessage");
            AddXMLAttribute(xmlDocument, queueElement, "Health", "HealthStatus");

            bool isFileCreated = false;

            // try to create the file, if it exists it will keep trying to create the file until it can.
            while (!isFileCreated)
            {
                if (!File.Exists(_xmlDocument))
                {
                    try
                    {
                        xmlDocument.Save(_xmlDocument);
                        isFileCreated = true;
                    }
                    catch { }
                }
            }
        }

        public void SetBuildStatus(string buildStatus, bool health)
        {
            if (File.Exists(_xmlDocument))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(_xmlDocument);

                XmlNode xNode = xDoc.SelectSingleNode("BuildMessages/Message");

                if (xNode != null)
                {
                    string status = xNode.Attributes["Value"].Value;
                    xNode.Attributes["Value"].Value = buildStatus;

                    string _health = xNode.Attributes["Health"].Value;
                    xNode.Attributes["Health"].Value = health.ToString();
                    xDoc.Save(_xmlDocument);
                }
            }
        }

        private void AddXMLAttribute(XmlDocument xmlDocument, XmlNode parentNode, string attributeName, string attributeValue)
        {
            XmlAttribute xmlAttribute = xmlDocument.CreateAttribute(attributeName);
            xmlAttribute.Value = attributeValue;
            parentNode.Attributes.Append(xmlAttribute);
        }

        public List<string> GetBuildStatus()
        {
            if (File.Exists(_xmlDocument))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(_xmlDocument);
                List<string> status = new List<string>();
                XmlNode xNode = xDoc.SelectSingleNode("BuildMessages/Message");

                if (xNode != null)
                {
                    status.Add(xNode.Attributes["Value"].Value);
                    status.Add(xNode.Attributes["Health"].Value);
                }

                return status;
            }
            else
            {
                return null;
            }

        }
    }
}