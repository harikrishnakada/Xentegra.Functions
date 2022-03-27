using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.Interfaces;

namespace Xentegra.Models
{
    public class Technology : DomainModelBase, IPartitionKey
    {
        public string name { get; set; }
        public string description { get; set; }
        public string resourceGroupName { get; set; }

        public void SetPartitionKey()
        {
            this.pk = this.name;
        }
    }
}
