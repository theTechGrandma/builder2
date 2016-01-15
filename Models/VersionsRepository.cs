using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DSTBuilder.Models
{
    public class VersionsRepository : IVersionsRepository
    {
        private string _xmlDatabase = @"C:\ProductConfigurations.xml";
        public IEnumerable<Versions> GetVersions(string product)
        {
            List<Versions> majorMinorVersions = new List<Versions>();
            var xml = XElement.Load(_xmlDatabase);
            IEnumerable<XElement> versions = xml.Descendants("Product").Where(e => e.Attribute("Name").Value == product);

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

        
    }
}