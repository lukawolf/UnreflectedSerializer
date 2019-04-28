using System;
using System.Collections.Generic;
using System.IO;

namespace UnreflectedSerializer
{
    public class DescriptionField
    {
        public string TagName { get; }
        public Func<object, string> ValueGetter { get; }

        public DescriptionField(Func<object, string> valueGetter)
        {
            ValueGetter = valueGetter;
        }

        public DescriptionField(string tagName, Func<object, string> valueGetter)
        {
            TagName = tagName;
            ValueGetter = valueGetter;
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
            Console.ReadLine();
        }

        static RootDescriptor<Person> GetPersonDescriptor()
        {
            var rootDesc = new RootDescriptor<Person>("Person");
            rootDesc.RegisterDescriptionField(new DescriptionField("FirstName", (person) => ((Person)person).FirstName));
            rootDesc.RegisterDescriptionField(new DescriptionField("LastName", (person) => ((Person)person).LastName));
            rootDesc.RegisterDescriptionField(new DescriptionField((person) => GetAddressDescriptor().SerializeToString(((Person)person).HomeAddress, "HomeAddress")));
            rootDesc.RegisterDescriptionField(new DescriptionField((person) => GetAddressDescriptor().SerializeToString(((Person)person).WorkAddress, "WorkAddress")));
            rootDesc.RegisterDescriptionField(new DescriptionField((person) => GetCountryDescriptor().SerializeToString(((Person)person).CitizenOf, "CitizenOf")));
            rootDesc.RegisterDescriptionField(new DescriptionField((person) => GetPhoneNumberDescriptor().SerializeToString(((Person)person).MobilePhone, "MobilePhone")));
            return rootDesc;
        }

        static RootDescriptor<Address> GetAddressDescriptor()
        {
            var rootDesc = new RootDescriptor<Address>("Address");
            rootDesc.RegisterDescriptionField(new DescriptionField("Street", (address) => ((Address)address).Street));
            rootDesc.RegisterDescriptionField(new DescriptionField("City", (address) => ((Address)address).City));
            return rootDesc;
        }

        static RootDescriptor<Country> GetCountryDescriptor()
        {
            var rootDesc = new RootDescriptor<Country>("Country");
            rootDesc.RegisterDescriptionField(new DescriptionField("Name", (country) => ((Country)country).Name));
            rootDesc.RegisterDescriptionField(new DescriptionField("AreaCode", (country) => ((Country)country).AreaCode.ToString()));
            return rootDesc;
        }

        static RootDescriptor<PhoneNumber> GetPhoneNumberDescriptor()
        {
            var rootDesc = new RootDescriptor<PhoneNumber>("PhoneNumber");
            rootDesc.RegisterDescriptionField(new DescriptionField((phoneNumber) => GetCountryDescriptor().SerializeToString(((PhoneNumber)phoneNumber).Country, "Country")));
            rootDesc.RegisterDescriptionField(new DescriptionField("Number", (phoneNumber) => ((PhoneNumber)phoneNumber).Number.ToString()));
            return rootDesc;
        }
    }
}
