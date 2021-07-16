using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace angoukun
{
    public class Contact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PubKey { get; set; }

        public string FullName
        {
            get
            {
                return this.FirstName + " " + this.LastName;
            }
        }

        public string PubKeyLine
        {
            get
            {
                return this.PubKey.Replace("\n", "");
            }
        }

        public Contact(string FirstName, string LastName, string PubKey)
        {
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.PubKey = PubKey;
        }
    }
}
