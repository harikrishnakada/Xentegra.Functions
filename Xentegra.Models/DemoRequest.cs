using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.DTO;
using Xentegra.Models.Interfaces;

namespace Xentegra.Models
{
    public class DemoRequest: DomainModelBase, IPartitionKey
    {
        public string requestType { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string company { get; set; }
        public TechnologyDTO technology { get; set; }
        public void SetPartitionKey()
        {
            this.pk = technology.name;
        }
    }
}
