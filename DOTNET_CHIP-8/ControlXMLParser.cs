using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DOTNET_CHIP_8
{
    public class ControlXMLParser
    {
        //returns all bound keyboard
        public static string[] Binds(string key)
        {
            string BindsXML = AppDomain.CurrentDomain.BaseDirectory + @"\data\KeyMappings.xml";
            List<string> _binds = new List<string>();
            using (XmlReader reader = XmlReader.Create(BindsXML))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "KeyMappings":
                            break;
                        case "Bind":
                            string value = reader["keyboard"];
                            if (value == key)
                            {
                                _binds.Add(reader["console"]);
                            }
                            break;
                    }
                }
            }
            return _binds.ToArray();
        }
    }
}
