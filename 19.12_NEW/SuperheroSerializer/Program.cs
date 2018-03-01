using System;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SuperheroSerializer
{
    class MyCrypto
    {
        public byte[] EncryptXor(byte[] bytes, byte key)
        {
            for(int i = 0; i<bytes.Length;++i)
            {
                bytes[i] ^= key;
            }
            return bytes;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Superhero Danya = new Superhero("qwerty");

            XmlSerializer serializer = new XmlSerializer(typeof(Superhero));
            
            //XmlSerialization
            using (var fin = new FileStream("serialized.xml", FileMode.Create))
            {
                serializer.Serialize(fin, Danya);
                Console.WriteLine("Check serialized.xml");
                Console.ReadKey();
            }

            //Binary custom serialization with encrypting
            using (var fileStream = File.OpenWrite("bin_serialized"))
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, Danya);

                memoryStream.Seek(0, SeekOrigin.Begin);

                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                var crypto = new MyCrypto();
                var encryptedBytes = crypto.EncryptXor(bytes, 5);
                fileStream.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }
    }
}
