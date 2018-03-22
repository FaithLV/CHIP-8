using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DOTNET_CHIP_8
{
    public class ControlXMLParser
    {
        //returns all bound keyboard
        public static string[] Binds(string key, string gamehash = null)
        {
            string BindsXML = AppDomain.CurrentDomain.BaseDirectory + @"\data\KeyMappings.xml";

            if(gamehash != null)
            {
                string customControls = AppDomain.CurrentDomain.BaseDirectory + $"profiles\\{gamehash}\\KeyMappings.xml";
                if (File.Exists(customControls))
                {
                    BindsXML = customControls;
                }
            }
            
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
