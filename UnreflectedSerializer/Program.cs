using System;
using System.Collections.Generic;
using System.IO;

namespace UnreflectedSerializer
{
    public abstract class DescriptionField
    {
        public string TagName { get; protected set; }
        public Func<object, string> ValueGetter { get; protected set; }
    }

    public class StringDescriptionField : DescriptionField
    {
        public StringDescriptionField(string tagName, Func<object, string> valueGetter)
        {
            TagName = tagName;
            ValueGetter = valueGetter;
        }
    }

    public class IntDescriptionField : DescriptionField
    {
        public IntDescriptionField(string tagName, Func<object, int> valueGetter)
        {
            TagName = tagName;
            ValueGetter = (obj) => valueGetter(obj).ToString();
        }
    }

    public class RootDescriptionField<T> : DescriptionField
    {
        public RootDescriptionField(string tagName, RootDescriptor<T> descriptor, Func<object, T> valueGetter)
        {
            ValueGetter = (obj) => descriptor.SerializeToString(valueGetter(obj), tagName);
        }
    }

    public class RootDescriptor<T>
    {
        public string TagName { get; private set; }
        private List<DescriptionField> fields = new List<DescriptionField>();

        public RootDescriptor(string TagName){
            this.TagName = TagName;
        }

        public void RegisterDescriptionField(DescriptionField field)
        {
            fields.Add(field);
        }

        private string OpenTag(string tagName)
        {
            return "<" + tagName + ">";
        }

        private string CloseTag(string tagName)
        {
            return "</" + tagName + ">";
        }

        private string Sanitize(string value)
        {
            return value.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public void Serialize(TextWriter writer, T instance)
        {
            writer.WriteLine(OpenTag(TagName));
            foreach (var field in fields)
            {
                if (field.TagName == null)
                    writer.Write(field.ValueGetter(instance));
                else
                    writer.WriteLine(OpenTag(field.TagName) + Sanitize(field.ValueGetter(instance)) + CloseTag(field.TagName));
            }
            writer.WriteLine(CloseTag(TagName));
        }

        public string SerializeToString(T instance, string tagName = null)
        {
            var writer = new StringWriter();

            var myTagName = TagName;
            if (tagName != null)
                TagName = tagName;

            Serialize(writer, instance);
   
            if (tagName != null)
                TagName = myTagName;

            return writer.ToString();
        }
    }

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    class Country
    {
        public string Name { get; set; }
        public int AreaCode { get; set; }
    }

    class PhoneNumber
    {
        public Country Country { get; set; }
        public int Number { get; set; }
    }

    class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address HomeAddress { get; set; }
        public Address WorkAddress { get; set; }
        public Country CitizenOf { get; set; }
        public PhoneNumber MobilePhone { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            RootDescriptor<Person> rootDesc = GetPersonDescriptor();

            var czechRepublic = new Country { Name = "Czech Republic", AreaCode = 420 };
            var person = new Person
            {
                FirstName = "Pavel",
                LastName = "Jezek",
                HomeAddress = new Address { Street = "Patkova", City = "Prague" },
                WorkAddress = new Address { Street = "Malostranske namesti", City = "Prague" },
                CitizenOf = czechRepublic,
                MobilePhone = new PhoneNumber { Country = czechRepublic, Number = 123456789 }
            };

            rootDesc.Serialize(Console.Out, person);
        }

        static RootDescriptor<Person> GetPersonDescriptor()
        {
            var rootDesc = new RootDescriptor<Person>("Person");
            rootDesc.RegisterDescriptionField(new StringDescriptionField("FirstName", (person) => ((Person)person).FirstName));
            rootDesc.RegisterDescriptionField(new StringDescriptionField("LastName", (person) => ((Person)person).LastName));
            rootDesc.RegisterDescriptionField(new RootDescriptionField<Address>("HomeAddress", GetAddressDescriptor(), (person) => ((Person)person).HomeAddress));
            rootDesc.RegisterDescriptionField(new RootDescriptionField<Address>("WorkAddress", GetAddressDescriptor(), (person) => ((Person)person).WorkAddress));
            rootDesc.RegisterDescriptionField(new RootDescriptionField<Country>("CitizenOf", GetCountryDescriptor(), (person) => ((Person)person).CitizenOf));
            rootDesc.RegisterDescriptionField(new RootDescriptionField<PhoneNumber>("MobilePhone", GetPhoneNumberDescriptor(), (person) => ((Person)person).MobilePhone));
            return rootDesc;
        }

        static RootDescriptor<Address> GetAddressDescriptor()
        {
            var rootDesc = new RootDescriptor<Address>("Address");
            rootDesc.RegisterDescriptionField(new StringDescriptionField("Street", (address) => ((Address)address).Street));
            rootDesc.RegisterDescriptionField(new StringDescriptionField("City", (address) => ((Address)address).City));
            return rootDesc;
        }

        static RootDescriptor<Country> GetCountryDescriptor()
        {
            var rootDesc = new RootDescriptor<Country>("Country");
            rootDesc.RegisterDescriptionField(new StringDescriptionField("Name", (country) => ((Country)country).Name));
            rootDesc.RegisterDescriptionField(new IntDescriptionField("AreaCode", (country) => ((Country)country).AreaCode));
            return rootDesc;
        }

        static RootDescriptor<PhoneNumber> GetPhoneNumberDescriptor()
        {
            var rootDesc = new RootDescriptor<PhoneNumber>("PhoneNumber");
            rootDesc.RegisterDescriptionField(new RootDescriptionField<Country>("Country", GetCountryDescriptor(), (phoneNumber) => ((PhoneNumber)phoneNumber).Country));
            rootDesc.RegisterDescriptionField(new IntDescriptionField("Number", (phoneNumber) => ((PhoneNumber)phoneNumber).Number));
            return rootDesc;
        }
    }
}
