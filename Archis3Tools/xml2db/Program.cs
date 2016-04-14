using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace xml2db
{
    // --xml=Z:\wouterb\Downloads\attachments\ARCHIS3_Winterswijk_2015_2016.xml --out=C:\tmp\winterswijk.txt

    class Program
    {
        static Dictionary<string, string> cmdArgs = new Dictionary<string,string>();

        static void Main(string[] args)
        {
            string key;
            string value;
            foreach (string arg in args)
            {
                key = arg.Split('=')[0].TrimStart('-');
                value = arg.Substring(arg.IndexOf('=') + 1);
                cmdArgs.Add(key,value);
            }
            //Dictionary<string, string> xPaths;
            //DataTable dt = GetTable(cmdArgs["map"], out xPaths);
            Console.WriteLine("Archis3 Xml Converter");
            Console.WriteLine("Input file: " + cmdArgs["xml"]);
            Console.WriteLine("Error messages");
            int count = WriteData(cmdArgs["xml"], cmdArgs["out"]);
            Console.WriteLine("Done, data rows exported: " + count);
            Console.WriteLine("Output file: " + cmdArgs["out"]);
            Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xpathColumnMap"></param>
        /// <param name="xPaths"></param>
        /// <returns></returns>
        static DataTable GetTable(string xpathColumnMap, out Dictionary<string, string> xPaths)
        {
            DataTable d = new DataTable();
            xPaths = new Dictionary<string, string>();
            string line;
            using (StreamReader sr = new StreamReader(xpathColumnMap))
            {
                while (sr.Peek() >= 0)
                {
                    line = sr.ReadLine();
                    Console.WriteLine(String.Format("{0}<={1}", line.Split('\t')[1], line.Split('\t')[0]));
                    d.Columns.Add(line.Split('\t')[1]);
                    xPaths.Add(line.Split('\t')[0], line.Split('\t')[1]);
                }
            }
            return d;
        }

        static int WriteData(string xmlPath, string outFile)
        {
            // The basics
            string _id;
            string startdatum;
            string einddatum_veldwerk;
            string type_onderzoek;
            string zaakidentificatie;
            string bedrijfsnaam;
            string output;
            int rowCount = 0;

            // Index based lookup of zaakobjecten, file integrity
            int count;

            // Ze file
            StreamWriter sw = new StreamWriter(outFile);
            sw.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}", "zaak_id"
                          , "object_volgnr"
                          , "startdatum"
                          , "einddatum_veldwerk"
                          , "type_onderzoek"
                          , "zaakidentificatie"
                          , "bedrijfsnaam"
                          , "object_type"
                          , "plaats_naam"
                          , "gemeente_naam"
                          , "toponiem"
                          , "geometrieid"
                          , "geometrie"
                          )
                        );

            // Ze document.
            XDocument xDoc = XDocument.Load(xmlPath);
            IEnumerable<XElement> list = xDoc.Root.XPathSelectElements("./item");
            foreach (XElement el in list)
            {
                _id = el.XPathSelectElement("./_id").Value;
                startdatum = el.XPathSelectElement("./fields/startdatum").Value;
                einddatum_veldwerk = el.XPathSelectElement("./fields/einddatum_veldwerk").Value;
                type_onderzoek = el.XPathSelectElement("./fields/type_onderzoek").Value;
                zaakidentificatie = el.XPathSelectElement("./fields/zaakidentificatie").Value;
                bedrijfsnaam = el.XPathSelectElement("./fields/zaakbetrokkenen/bedrijfsnaam").Value;
                // Go through the zaakobjecten
                IEnumerable<XElement> zobjects = el.XPathSelectElements("./fields/zaakobjecten");
                foreach (XElement fe in zobjects) // multiple zaak objecten, only distinguishable by order (index)
                {
                    output = "";
                    count = fe.XPathSelectElements("object_type").Count();
                    if (count == fe.XPathSelectElements("plaats_naam").Count()
                        && count == fe.XPathSelectElements("gemeente_naam").Count()
                        && count == fe.XPathSelectElements("toponiem").Count()
                        && count == fe.XPathSelectElements("geometrie").Count()
                        )
                    {
                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                output = (string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}", _id
                                  , i
                                  , startdatum
                                  , einddatum_veldwerk
                                  , type_onderzoek
                                  , zaakidentificatie
                                  , bedrijfsnaam
                                  , fe.XPathSelectElements("object_type").ToList()[i].Value
                                  , fe.XPathSelectElements("plaats_naam").ToList()[i].Value
                                  , fe.XPathSelectElements("gemeente_naam").ToList()[i].Value
                                  , fe.XPathSelectElements("toponiem").ToList()[i].Value
                                  , fe.XPathSelectElements("geometrie/geometrieid").ToList()[i].Value
                                  , fe.XPathSelectElements("geometrie/geometrie").ToList()[i].Value
                                  )
                                );
                                // verify row
                                if (output.ToCharArray().Count(c => c == '|') != 12)
                                    Console.WriteLine(String.Format("Invalid row, id: {0}", _id));
                                else
                                {
                                    sw.WriteLine(output);
                                    rowCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(String.Format("Missing Xml element in \"zaak id\": {0,-10} {1}", _id, ex.Message.Replace("\r\n", " ")));
                            }
                        }
                    }
                    else
                        Console.WriteLine(String.Format("Zaakobjecten elements do not match, skipped: {0} ({1})", _id, count));
                }
            }
            sw.Close();
            return rowCount;
        }
    }
}
