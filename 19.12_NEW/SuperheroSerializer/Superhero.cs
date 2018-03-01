using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SuperheroSerializer
{
    public class Superhero : ISerializable
    {
        static int count = 0;
        int id;
        public Gun gun;

        [XmlArray("Quests")]
        [XmlArrayItem("Quest")]
        public List<Quest> quests;

        string billPassword;

        public Superhero()
        {
            id = count++;
            gun = new Gun();
            Quest startQuest = new Quest { Message = "Talk to dekan", Reward = 100 };
            quests = new List<Quest>();
            quests.Add(startQuest);
        }
        public Superhero(string pass) : this()
        {
            billPassword = pass;
        }
        private Superhero(SerializationInfo propertyBag, StreamingContext context) : this()
        {
            gun = (Gun)propertyBag.GetValue("gun", gun.GetType());
            quests = (List<Quest>)propertyBag.GetValue("quests", quests.GetType());
        }
        public void GetObjectData(SerializationInfo propertyBag, StreamingContext context)
        {
            propertyBag.AddValue("gun", gun);
            propertyBag.AddValue("quests", quests);
        }
    }
}
