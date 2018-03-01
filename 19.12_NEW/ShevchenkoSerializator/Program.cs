using System;
using System.IO;
using System.Threading;

namespace ShevchenkoSerializator
{
    [XmlSerializable]
    class C                                             //Агрегируемый агрегируемым
    {
        public string hello;
    }
    [XmlSerializable]
    class B                                             //Агрегируемый класс
    {
        public int asdas;
        public C CProperty { get; set; }

        public B()
        {
            CProperty = new C();
        }
    }
    [XmlSerializable]
    class MyClass                                       //Основной класс
    {
        [XmlAttribute]
        public int a;

        [XmlIgnore]
        public string b;
        
        [XmlElement("AgregationClass")]
        public B BProperty { get; set; }
        public double D { get; set; }

        public MyClass()
        {
            BProperty = new B();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //Нужно подобавлять значения в рантайме. Если установить их по умолчанию во время инициализации полей в классе
            //не будет видно работы десериализатора. Потому что он и так, инвокнув конструктор MyClass(), получит правильные
            //данные по всем полям
            var c = new C();
            c.hello = "worlds";
            var b = new B();
            b.asdas = 17;
            b.CProperty = c;
            var myClass = new MyClass() { D = 3.14};
            myClass.a = 6;
            myClass.b = "avs";
            myClass.BProperty = b;

            XmlSerializer serializer = new XmlSerializer(myClass.GetType());
            IAsyncResult asyncResult = null;
            var fs = new FileStream("serialized.xml", FileMode.Create);
            asyncResult = serializer.BeginSerialize(fs, myClass);

            Console.WriteLine("Основной поток продолжает работать");

            serializer.EndSerialize(asyncResult);
            Console.WriteLine("См. файл serialized.xml");


            MyClass my;
            fs = new FileStream("serialized.xml", FileMode.Open);
            asyncResult = serializer.BeginDeserialize(fs);
            my = (MyClass)serializer.EndDeserialize(asyncResult);

            Console.ReadKey();//Посмотрите в отладчике переменную "my"
        }
    }
}
