﻿/// <summary>
/// The autogenerated game object model.
/// </summary>
namespace bg3_modders_multitool.Models
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public class AutoGenGameObject
    {
        public AutoGenGameObject(string file)
        {
            if (File.Exists(file)&&false)
            {
                using (var fileStream = new StreamReader(file))
                using (var reader = new XmlTextReader(fileStream))
                {
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.IsStartElement() && reader.GetAttribute("id") == "GameObjects")
                        {
                            var xml = (XElement)XNode.ReadFrom(reader);
                            var attributes = xml.Elements().Where(x => x.Name == "attribute");

                            foreach (XElement attribute in attributes)
                            {
                                var id = attribute.Attribute("id").Value;
                                var handle = attribute.Attribute("handle")?.Value;
                                var value = handle ?? attribute.Attribute("value").Value;
                                var type = attribute.Attribute("type").Value;
                                if (string.IsNullOrEmpty(handle))
                                {
                                    //gameObject.LoadProperty(id, type, value);
                                }
                                else
                                {
                                    //gameObject.LoadProperty($"{id}Handle", type, value);
                                    //var translationText = TranslationLookup.FirstOrDefault(tl => tl.Key.Equals(value)).Value?.Value;
                                    //gameObject.LoadProperty(id, type, translationText);
                                }
                            }

                            //if (string.IsNullOrEmpty(gameObject.Name.Value))
                            //    gameObject.Name.Value = gameObject.DisplayName?.Value;
                            //if (string.IsNullOrEmpty(gameObject.Name.Value))
                            //    gameObject.Name.Value = gameObject.Stats?.Value;

                            //lock (GameObjects)
                            //{
                            //    GameObjects.Add(gameObject);
                            //    reader.Skip();
                            //}
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
        }

        #region Autogen Properties

        #endregion
    }
}
