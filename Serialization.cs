using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using Client.Windows;

namespace Client.Net
{
    static class Serialization
    {
        public static byte[] getBytes(INPUT str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static INPUT fromBytes(ref byte[] arr)
        {
            INPUT str = new INPUT();

            int size = Marshal.SizeOf(str.GetType());
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (INPUT)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static byte[] GetClipboardBytes(ClipboardPacket p)
        {
            byte[] res = new byte[8*1024];
            int i = 0;
            res[i++] = (byte)p.type;
            for (int j = sizeof(long); j > 0 ; j--)
            {
                res[i++] = (byte) (p.totalLength >> (8*((sizeof(long)-j)) ));
            }
            byte[] s = Encoding.Unicode.GetBytes(p.name);
            for (int j = 0; j < 256; j++)
            {
                res[i++] = (j < s.Length) ? s[j] : (byte)0;
            }
            for (int j = sizeof(int); j > 0 ; j--)
            {
                res[i++] = (byte)(p.length >> (8 * ((sizeof(int) - j))));
            }
            for (int j = 0; j < p.length; j++)
            {
                res[i++] = p.data[j];
            }
            return res;
        }

        public static ClipboardPacket FromClipboardBytes(byte[] arr)
        {
            ClipboardPacket p = new ClipboardPacket();
            int i = 0;
            p.type = arr[i++];
            p.totalLength = BitConverter.ToInt64(arr, i);
            i += 8;
            p.name = Encoding.Unicode.GetString(arr, i, 256).TrimEnd('\0');  //remove all string terminator characters
            i += 256;
            p.length = BitConverter.ToInt32(arr, i);
            i += 4;
            p.data = new byte[p.length];
            for (int j = 0; j < p.length; j++, i++)
            {
                p.data[j] = arr[i];
            }
            return p;
        }

        public static string SerializeObject<T>(T obj)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode);
            xs.Serialize(xmlTextWriter, obj);
            memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
            return Encoding.Unicode.GetString(memoryStream.ToArray());
        }
        
        public static T DeserializeObject<T>(string xml)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(xml));
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode);
            return (T)xs.Deserialize(memoryStream);
        }
        
    }

}
