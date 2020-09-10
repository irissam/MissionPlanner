﻿using ExifLibrary;
using log4net;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MissionPlanner.ArduPilot
{
    public class LogMetaData
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static string[] vehicles = new[] { "Copter", "Plane", "Rover", "Tracker" };

        static string url = "https://autotest.ardupilot.org/LogMessages/{0}/LogMessages.xml";

        public static  Dictionary<string, Dictionary<string, string>> MetaData { get; } = new Dictionary<string, Dictionary<string, string>>();

        public static async Task GetMetaData()
        {
            List<Task> tlist = new List<Task>();

            vehicles.ForEach(a =>
            {
                try
                {
                    var newurl = String.Format(url, a);
                    var file = Path.Combine(Settings.GetDataDirectory(), "LogMessages" + a + ".xml");
                    if(File.Exists(file))
                        if (new FileInfo(file).LastWriteTime.AddDays(7) > DateTime.Now)
                            return;
                    var dltask = Download.getFilefromNetAsync(newurl, file);
                    tlist.Add(dltask);
                }
                catch (Exception ex) { log.Error(ex); }
            });

            await Task.WhenAll(tlist);
        }

        public static void ParseMetaData()
        {
            vehicles.ForEach(a =>
            {
                var file = Path.Combine(Settings.GetDataDirectory(), "LogMessages" + a + ".xml");

                if (!File.Exists(file))
                {
                    return;
                }

                var xml = XDocument.Load(file);

                xml.Root.Descendants("logformat").ForEach<XElement>(b =>
                {
                    if (b == null)
                        return;

                    var type = b.Attribute("name");
                    var typedesc = b.Descendants("description").FirstOrDefault();

                    if (!MetaData.ContainsKey(type.Value))
                        MetaData[type.Value] = new Dictionary<string, string>();

                    MetaData[type.Value]["description"] = typedesc.Value;

                    b.Descendants("fields").Descendants("field").ForEach(c => 
                    {
                        var name = c.Attribute("name");
                        var desc = c.Descendants("description").FirstOrDefault();

                        MetaData[type.Value][name.Value] = desc.Value;
                    });
                });
            });
        }
    }
}
