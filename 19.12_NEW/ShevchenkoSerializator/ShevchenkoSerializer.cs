using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.Remoting.Messaging;

namespace ShevchenkoSerializator
{
    class XmlSerializer : IDisposable
    {
        Type type;
        MemberInfo[] members; //Поля и свойства класса типа type
        Dictionary<MemberInfo, Dictionary<XmlAttributes, Attribute>> attrDict; //Атрибуты полей и свойств
        ManualResetEvent fsClosed = new ManualResetEvent(false);

        public XmlSerializer(Type type)//См. сообщения исключений, чтобы понять, в каких случаях они вылетают
        {
            #region Проверки
            if (type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                   null, Type.EmptyTypes, null) == null)

                throw new XmlSerializerException("A type which is serializing should have parameterless ctor" +
                    "in order to be deserialized");

            if (type.GetCustomAttribute(typeof(XmlSerializable)) == null)
                throw new XmlSerializerException("Serializing type requires [XmlSerializable] attribute");
            #endregion

            #region Заполнение полей members и attrDict
            this.type = type;
            members = type.GetFields().Cast<MemberInfo>() //Получаем информацию о полях и свойствах
                .Concat(type.GetProperties()).ToArray();
            attrDict = new Dictionary<MemberInfo, Dictionary<XmlAttributes, Attribute>>();
            foreach(var member in members)
            {
                attrDict.Add(member, new Dictionary<XmlAttributes, Attribute>());

                //Тип поля или свойства
                Type fieldType = (member as FieldInfo)?.FieldType ?? (member as PropertyInfo).PropertyType;
                var serializableAttr = fieldType.GetCustomAttribute(typeof(XmlSerializable));
                if (serializableAttr != null)
                    attrDict[member].Add(XmlAttributes.XmlSerializable, serializableAttr);

                var attrs = member.GetCustomAttributes();
                //Перебираем атрибуты, заполняем словарь
                foreach (Attribute attr in attrs)
                {
                    if (attr is XmlIgnoreAttribute)
                        attrDict[member].Add(XmlAttributes.XmlIgnore, attr);
                    else if (attr is XmlElementAttribute)
                        attrDict[member].Add(XmlAttributes.XmlElement, attr);
                    else if (attr is XmlAttributeAttribute)
                        attrDict[member].Add(XmlAttributes.XmlAttribute, attr);
                }
            }
#endregion
        }

        //Сериализация
        public XElement GetSerialization(XElement mainTag, object obj)
        {
            
            foreach(MemberInfo member in members)
            {
                if (attrDict[member].ContainsKey(XmlAttributes.XmlIgnore))
                    continue;

                string tagName = attrDict[member].ContainsKey(XmlAttributes.XmlElement)
                    ? (attrDict[member][XmlAttributes.XmlElement] as XmlElementAttribute).Name
                    :member.Name;
                bool isAttribute = attrDict[member].ContainsKey(XmlAttributes.XmlAttribute);
                bool isSerializable = attrDict[member].ContainsKey(XmlAttributes.XmlSerializable);
                //значение и тип поля или свойства
                object value = (member as FieldInfo)?.GetValue(obj) ?? (member as PropertyInfo).GetValue(obj);
                Type curType = (member as FieldInfo)?.FieldType ?? (member as PropertyInfo).PropertyType;

                XElement current = new XElement(tagName);

                if (isAttribute)
                {
                    if (isSerializable)
                        //Нельзя сделать объект пользовательского класса аттрибутом
                        throw new XmlSerializerException("Can't make an attribute out of a serializing object");

                    mainTag.SetAttributeValue(tagName, value);
                    continue;
                }
 
                //Если поле или свойство пользовательского типа и имеет XmlSerializable аттрибут, 
                //рекурсивно вызываем такой же метод на новом экземпляре сериализатора
                if (isSerializable)
                {
                    XElement innerTag = value != null ? new XmlSerializer(curType).GetSerialization(current, value)
                        : null;
                    if (innerTag != null)
                        current = innerTag;
                    else
                        current.Value = "null";
                    mainTag.Add(current);
                }
                else
                {
                    current.Value = value.ToString();
                    mainTag.Add(current);
                }
            }
            return mainTag;
        }

        public void Serialize(FileStream stream, object obj)
        {
            Thread.Sleep(3000);//Задержка для тестировки асинхронных методов
            if (obj.GetType() != type)
                throw new XmlSerializerException("Wrong type of serializing object. Can only serialize" +
                    "type you declared in ctor");

            XElement mainTag = new XElement(type.Name);

            XDocument xDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XComment("This was made by XmlSerializer class"),
                GetSerialization(mainTag, obj)
                );

            xDoc.Save(stream);
        }

        //Десериализация
        private MemberInfo FindMember(XName xmlName)
        {
            MemberInfo current = null;
            foreach (var fp in members)
            {
                string fpName = attrDict[fp].ContainsKey(XmlAttributes.XmlElement)
                    ? (attrDict[fp][XmlAttributes.XmlElement] as XmlElementAttribute).Name
                    : fp.Name;
                if (fpName == xmlName.LocalName)
                {
                    current = fp;
                    break;
                }
            }
            if (current == null)
                throw new XmlSerializerException("Uncorrect xml file to deserialize. File contains some extra data");
            return current;
        }

        public object Deserialize(XElement root)
        {
            var ctor = type.GetConstructor(new Type[0]);

            object res = ctor.Invoke(null);

            //Вытягиваем аттрибуты
            if(root.HasAttributes)
            {
                foreach(var attr in root.Attributes())
                {
                    MemberInfo current = FindMember(attr.Name);

                    Type curType = (current as FieldInfo)?.FieldType ?? (current as PropertyInfo).PropertyType;

                    object resValue;
                    string aValue = attr.Value;
                    if (curType == typeof(int))
                        resValue = Int32.Parse(aValue);
                    else if (curType == typeof(double))
                        resValue = Double.Parse(aValue);
                    else if (curType == typeof(long))
                        resValue = long.Parse(aValue);
                    else if (curType == typeof(char))
                        resValue = char.Parse(aValue);
                    else if (curType == typeof(bool))
                        resValue = bool.Parse(aValue);
                    else
                        resValue = aValue;

                    if (current is FieldInfo)
                        (current as FieldInfo).SetValue(res, resValue);
                    else if (current is PropertyInfo)
                        (current as PropertyInfo).SetValue(res, resValue);
                }
            }
            
            //Вытягиваем всё остальное
            foreach (var tag in root.Descendants())
            {
                if (tag.Parent != root)//Костыль, потому что root.Descendants() возвращает всех детей, даже детей детей.
                                       //Поэтому в детей детей заходит дважды, и рекурсивно, и просто перебором здесь
                    continue;
                MemberInfo current = FindMember(tag.Name);

                Type curType = (current as FieldInfo)?.FieldType ?? (current as PropertyInfo).PropertyType;

                if (tag.HasElements)
                {
                    XmlSerializer serializer = new XmlSerializer(curType);
                    if (current is FieldInfo)
                        (current as FieldInfo).SetValue(res, serializer.Deserialize(tag));
                    else if (current is PropertyInfo)
                        (current as PropertyInfo).SetValue(res, serializer.Deserialize(tag));
                    continue;
                }
                object resValue;
                string aValue = tag.Value;
                if (curType == typeof(int))
                    resValue = Int32.Parse(aValue);
                else if (curType == typeof(double))
                    resValue = Double.Parse(aValue);
                else if (curType == typeof(long))
                    resValue = long.Parse(aValue);
                else if (curType == typeof(char))
                    resValue = char.Parse(aValue);
                else if (curType == typeof(bool))
                    resValue = bool.Parse(aValue);
                else
                    resValue = aValue;

                if (current is FieldInfo)
                    (current as FieldInfo).SetValue(res, resValue);
                else if (current is PropertyInfo)
                    (current as PropertyInfo).SetValue(res, resValue);
            }

            return res;
        }

        public object Deserialize(FileStream stream)
        {
            //Thread.Sleep(2000);//Задержка для тестировки асинхронных методов
            XDocument xDoc = XDocument.Load(stream);

            return Deserialize(xDoc.Root);
        }

        public void Dispose()
        {
            Console.WriteLine("Serializer {0} disposed", this.GetHashCode());
        }

        ~XmlSerializer()
        {
            Console.WriteLine("Serializer {0} disposed", this.GetHashCode());
        }

        public IAsyncResult BeginSerialize(FileStream stream, object obj)
        {
            var asyncSerialize = new Action<FileStream, object>(Serialize);
            IAsyncResult result = asyncSerialize.BeginInvoke(stream, obj, null, stream);
            return result;
        }
        /// <summary>
        /// Ожидает завершения BeginSerialize(FileStream stream, object obj) и закрывает поток stream
        /// </summary>
        /// <param name="result"></param>
        public void EndSerialize(IAsyncResult result)
        {
            result.AsyncWaitHandle.WaitOne();
            FileStream fs = result.AsyncState as FileStream;
            fs.Close();
        }

        public IAsyncResult BeginDeserialize(FileStream stream)
        {
            Func<FileStream, object> delegat = Deserialize;
            IAsyncResult result = delegat.BeginInvoke(stream, null, stream);
            return result;
        }
        /// <summary>
        /// Ожидает завершения BeginDeserialize(FileStream stream) и закрывает поток stream
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public object EndDeserialize(IAsyncResult result)
        {
            result.AsyncWaitHandle.WaitOne();
            FileStream fs = result.AsyncState as FileStream;
            fs.Close();
            AsyncResult res = result as AsyncResult;
            return (res.AsyncDelegate as Func<FileStream, object>).EndInvoke(result);
        }
    }
}
