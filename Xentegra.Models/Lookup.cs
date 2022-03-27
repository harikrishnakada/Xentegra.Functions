using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.Interfaces;

namespace Xentegra.Models
{
    public class Lookup : DomainModelBase, IPartitionKey
    {
        public string lookupType { get; set; }

        public string name { get; set; }

        public string internalIdentifier { get; set; }

        public void SetPartitionKey()
        {
            pk = lookupType;
        }
    }
}
