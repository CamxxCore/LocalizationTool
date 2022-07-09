using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace LocalizationTool.Localization
{
    public abstract class ILocalizationFile : ISerializable
    {
        public abstract byte[] Serialize();

        public abstract int Deserialize(byte[] data);

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class LocalizationFile : ILocalizationFile
    {
        public LocalizationHeader Header = new LocalizationHeader();

        private Dictionary<int, string> _items;

        public Dictionary<int, string> Items
        {
            get
            {

                if (_items == null)
                {
                    _items = new Dictionary<int, string>();

                    int idx = 1;

                    _items.AddRange(Utils.Repeat(() =>
                    {
                        int key = idx++;
                        return new KeyValuePair<int, string>(key, "");
                    }, Header.Count));
                }

                return _items;
            }

            private set
            {
                _items = value;
            }
        }

        public LocalizationFile(int version, int languageId, Dictionary<int, string> items)
        {
            Items = items ?? throw new ArgumentNullException("items");

            Header.Count = items.Count;

            Header.LanguageID = languageId;

            Header.Version = version;
        }

        public LocalizationFile(int version, int languageId)
        {
            Header.Version = version;

            Header.LanguageID = languageId;
        }

        public LocalizationFile(int version)
        {
            Header.Version = version;
        }

        public LocalizationFile()
        { }

        public void AddItem(int key, string name)
        {
            Items[key] = name;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Header.Magic);
                    writer.Write(Header.Version);
                    writer.Write(Header.LanguageID);
                    writer.Write(Items.Count);

                    foreach (var item in Items)
                    {
                        writer.Write(item.Value);
                    }    
                }

                return stream.ToArray();
            }
        }

        public override int Deserialize(byte[] data)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                Header.Magic = reader.ReadInt32();

                if (Header.Magic != 0x10C)
                    return 0;

                Header.Version = reader.ReadInt32();
                Header.LanguageID = reader.ReadInt32();
                Header.Count = reader.ReadInt32();
              
                for (int i = 0; i < Header.Count; i++)
                {
                    AddItem(i + 1, reader.ReadString());
                }
            }

            return data.Length;
        }
    }

    public class LocalizationHeader
    {
        public int Magic = 0x10C;
        public int Version;
        public int LanguageID;
        public int Count;
    }
}
