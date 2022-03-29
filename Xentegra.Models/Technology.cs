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

        public string enityType { get; set; }

        public string description { get; set; }
        public string resourceGroupName { get; set; }

        public void SetPartitionKey()
        {
            this.pk = this.name;
        }

        public Technology SetEntity(Technology entity)
        {
            name = entity.name;
            description = entity.description;
            resourceGroupName = entity.resourceGroupName;

            enityType = typeof(Technology).ToString();
            SetAudit();

            return this;
        }

        public override void OnCreated(string? _id = null)
        {
            this.enityType = typeof(Technology).ToString();
            base.OnCreated(_id);
        }
    }
}
