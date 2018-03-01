using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShevchenkoSerializator
{
    public enum XmlAttributes
    {
        XmlIgnore,
        XmlAttribute,
        XmlSerializable,
        XmlElement
    }
    /// <summary>
    /// Игнорировать элемент во время сериализации. Только для открытых свойств/полей.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class XmlIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Сделать элемент атрибутом родительского тэга. Только для открытых 
    /// свойств/полей типов, не декорированных [XmlSerializable].
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class XmlAttributeAttribute : Attribute
    {
    }

    /// <summary>
    /// Пометить класс как сериализуемый. Только для классов.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class XmlSerializable : Attribute
    {
    }

    /// <summary>
    /// Изменить имя элемента. Новое имя указывается в конструкторе. Только для открытых свойств/полей.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class XmlElementAttribute : Attribute
    {
        private readonly string name;

        public string Name
        {
            get { return name; }
        }

        public XmlElementAttribute(string name)
        {
            this.name = name;
        }
    }

}
