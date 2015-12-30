using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace BEncode
{
    class BEncode
    {
        BinaryReader reader;
        byte chr;
        public string outputfile;
        public string charset = Encoding.GetEncoding(Encoding.Default.CodePage).HeaderName;
        dynamic content = "";

        public BEncode()
        {
        }

        public BEncode(String filename)
        {
            load(filename);
        }

        #region encode
        public void save(string file)
        {
            File.WriteAllBytes(file, encode(content));
        }

        byte[] encode(Dictionary<Object, Object> dic)
        {
            var result = new List<byte> { (byte)'d' };
            foreach (dynamic item in dic)
            {
                result.AddRange(encode(item.Key));
                result.AddRange(encode(item.Value));
            }
            result.Add((byte)'e');
            return result.ToArray();
        }

        byte[] encode(List<Object> list)
        {
            var result = new List<byte> { (byte)'l' };
            foreach (dynamic item in list) result.AddRange(encode(item));
            result.Add((byte)'e');
            return result.ToArray();
        }

        byte[] encode(string str)
        {
            var result = Encoding.GetEncoding(charset).GetByteCount(str).ToString() + ":" + str;
            return Encoding.GetEncoding(charset).GetBytes(result);
        }

        byte[] encode(byte[] str)
        {
            var result = new List<byte>();
            result.AddRange(Encoding.Default.GetBytes(str.Length + ":"));
            result.AddRange(str);
            return result.ToArray();
        }

        byte[] encode(long n)
        {
            var result = "i" + n.ToString() + "e";
            return Encoding.Default.GetBytes(result);
        }
        #endregion
        #region decode
        public void load(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                reader = new BinaryReader(stream);
                chr = reader.ReadByte();
                switch (chr)
                {
                    case (byte)'d':
                        content = readDictionary();
                        break;
                    case (byte)'l':
                        content = readList();
                        break;
                    case (byte)'s':
                        content = readString();
                        break;
                    case (byte)'i':
                        content = readInt();
                        break;
                }
            }

            if (charset == Encoding.GetEncoding(Encoding.Default.CodePage).HeaderName)
            {
                try
                {
                    var new_charset = content["encoding"];
                    if (charset != new_charset)
                    {
                        charset = new_charset;
                        load(filename);
                    };
                }
                catch { }
            }

        }

        Dictionary<Object, Object> readDictionary()
        {
            var dic = new Dictionary<Object, Object>();
            var loop = true;
            var is_value = false;
            dynamic key = "";
            dynamic value = "";

            while (loop)
            {
                chr = reader.ReadByte();
                if (chr >= '0' && chr <= '9') value = readString();
                else if (chr == 'd') value = readDictionary();
                else if (chr == 'l') value = readList();
                else if (chr == 'i') value = readInt();
                else if (chr == 'e')
                {
                    if (is_value) throw new Exception("List key with no value");
                    loop = false;
                    continue;
                }

                if (is_value) dic.Add(key, value);
                else key = value;

                is_value = !is_value;
            }

            return dic;
        }

        List<Object> readList()
        {
            var list = new List<Object>();
            var loop = true;
            dynamic r = "";

            while (loop)
            {
                chr = reader.ReadByte();
                if (chr >= '0' && chr <= '9') r = readString();
                else if (chr == 'd') r = readDictionary();
                else if (chr == 'l') r = readList();
                else if (chr == 'i') r = readInt();
                else if (chr == 'e') loop = false;
                else throw new Exception("Wrong list");
                if (loop) list.Add(r);
            }

            return list;
        }

        Object readString()
        {
            string length = Encoding.Default.GetString(new[] { chr });
            byte[] str;
            var loop = true;

            while (loop)
            {
                chr = reader.ReadByte();
                if (chr >= '0' && chr <= '9') length += Encoding.Default.GetString(new[] { chr });
                else if (chr == ':') loop = false;
                else throw new Exception("Wrong string length");
            }

            str = reader.ReadBytes(Int32.Parse(length));
            for (var j = 0; j < str.Length; j++)
                if (str[j] < 32)
                {
                    return str;
                }

            return Encoding.GetEncoding(charset).GetString(str);
        }

        Int64 readInt()
        {
            string s = "";
            var loop = true;

            while (loop)
            {
                chr = reader.ReadByte();
                if ((chr >= '0' && chr <= '9') || (chr == '-' && s == "")) s += System.Text.Encoding.Default.GetString(new[] { chr });
                else if (chr == 'e') loop = false;
                else throw new Exception("Wrong integer");
            }

            return Int64.Parse(s);
        }
        #endregion
        #region getXML
        public byte[] getXML()
        {
            var doc = new XmlDocument();
            var xmldecl = doc.CreateXmlDeclaration("1.0", charset, null);
            doc.InsertBefore(xmldecl, doc.DocumentElement);
            XmlElement el = doc.CreateElement("bencode");
            doc.AppendChild(el);
            el.AppendChild(el.OwnerDocument.ImportNode(getXML(content), true));
            var hash = infohash();
            if (hash != null) el.SetAttribute("infohash", hash);
            return Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(charset), Encoding.UTF8.GetBytes(beautify(doc)));
        }

        XmlElement getXML(Dictionary<Object, Object> d)
        {
            var doc = new XmlDocument();
            var el = doc.CreateElement("dictionary");
            doc.AppendChild(el);
            foreach (dynamic item in d)
            {
                el.AppendChild(el.OwnerDocument.ImportNode(getXML(item.Key), true));
                el.AppendChild(el.OwnerDocument.ImportNode(getXML(item.Value), true));
            }
            return doc.DocumentElement;
        }

        XmlElement getXML(List<Object> l)
        {
            var doc = new XmlDocument();
            var el = (XmlElement)doc.CreateElement("list");
            doc.AppendChild(el);
            foreach (dynamic item in l) el.AppendChild(el.OwnerDocument.ImportNode(getXML(item), true));
            return doc.DocumentElement;
        }

        XmlElement getXML(string s)
        {
            var doc = new XmlDocument();
            var el = (XmlElement)doc.CreateElement("string");
            doc.AppendChild(el);
            el.InnerText = Encoding.UTF8.GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(charset), Encoding.UTF8.GetBytes(s)));
            el.InnerText = s;
            return doc.DocumentElement;
        }

        XmlElement getXML(byte[] b)
        {
            var doc = new XmlDocument();
            var el = (XmlElement)doc.CreateElement("binary");
            doc.AppendChild(el);
            el.InnerText = Convert.ToBase64String(b);
            return doc.DocumentElement;
        }

        XmlElement getXML(long n)
        {
            var doc = new XmlDocument();
            var el = (XmlElement)doc.CreateElement("int");
            doc.AppendChild(el);
            el.InnerText = n.ToString();
            return doc.DocumentElement;
        }
        #endregion
        #region loadXML
        public bool loadXML(string file)
        {
            var doc = new XmlDocument();
            try { doc.Load(file); }
            catch { return false; }
            if (doc.DocumentElement.Name != "bencode") return false;
            if (doc.DocumentElement.ChildNodes.Count != 1) return false;
            //  try {
            this.content = loadXML((XmlElement)doc.DocumentElement.FirstChild);
            //  }
            //  catch { return false; }
            return true;
        }

        // todo доделать загрузку из XML
        dynamic loadXML(XmlElement x)
        {
            dynamic new_content = null;
            switch (x.Name)
            {
                case "dictionary":
                    new_content = new Dictionary<Object, Object>();
                    if (x.ChildNodes.Count % 2 > 0) throw new Exception("Incorrect dictionary");
                    for (var i = 0; i < x.ChildNodes.Count; i += 2) new_content.Add(loadXML((XmlElement)x.ChildNodes[i]), loadXML((XmlElement)x.ChildNodes[i + 1]));
                    break;
                case "list":
                    new_content = new List<Object>();
                    for (var i = 0; i < x.ChildNodes.Count; i++) new_content.Add(loadXML((XmlElement)x.ChildNodes[i]));
                    break;
                case "string":
                    new_content = x.InnerText;
                    break;
                case "binary":
                    new_content = Encoding.Default.GetString(Convert.FromBase64String(x.InnerText));
                    break;
                case "int":
                    new_content = Int64.Parse(x.InnerText);
                    break;
                default:
                    throw new Exception("Incorrect XML tags");
            }
            return new_content;
        }
        #endregion

        string infohash()
        {
            string hash = null;
            var tool = new SHA1Managed();
            try { hash = Hash(encode(content["info"])); } catch { }
            return hash;
        }

        public string Hash(byte[] temp)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(temp);

                var sb = new StringBuilder();
                foreach (byte b in hash) sb.AppendFormat("{0:X2}", b);
                return sb.ToString();
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public string beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = Encoding.GetEncoding(charset),
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"" + charset + "\"?>");
        }
    }
}
