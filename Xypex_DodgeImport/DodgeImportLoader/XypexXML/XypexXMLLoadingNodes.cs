using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

///Class file to hold definition of typed XElement objects parsed by DodgeXMLParser and used for loading
namespace DodgeImportLoader.XypexXML
{
    /// <summary>
    /// 
    /// </summary>
    public class AccountXElement : XElement
    {
        public AccountXElement(XName XAccount)
            : base(XAccount)
        {
        }

        public AccountXElement(XElement parent)
            : base(parent)
        {
        }

       // public AccountXElement(
        //To do: define .xsd
    }

    public class ContactXElement : XElement
    {
         public ContactXElement(XName XContact)
            : base(XContact)
        {
        }

       //To do: define .xsd
    }

    public class ConnectionXElement : XElement
    {
        public ConnectionXElement(XName XConnection)
            : base(XConnection)
        {
        }

        //To do: define .xsd
    }

    public class ProjectXElement : XElement
    {
        public ProjectXElement(XName XProject)
            : base(XProject)
        {
        }

        //To do: define .xsd
    }

}
