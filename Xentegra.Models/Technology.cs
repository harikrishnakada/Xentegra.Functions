using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.Interfaces;

namespace Xentegra.Models
{
    public class Technology : DomainModelBase<Technology>, IPartitionKey
    {
        public string name { get; set; }

        public string description { get; set; }
        public string resourceGroupName { get; set; }

        public void SetPartitionKey()
        {
            this.entityType = GetEntityType();
            this.pk = this.entityType;
        }

        public string GetPartitionKey()
        {
            if (string.IsNullOrEmpty(this.pk))
                this.SetPartitionKey();

            return this.pk;
        }

        public Technology SetEntity(Technology entity)
        {
            name = entity.name;
            description = entity.description;
            resourceGroupName = entity.resourceGroupName;

            entityType = GetEntityType();
            SetAudit();

            return this;
        }

        public override void OnCreated(string? _id = null)
        {
            this.entityType = GetEntityType();
            base.OnCreated(_id);
        }
    }
}
