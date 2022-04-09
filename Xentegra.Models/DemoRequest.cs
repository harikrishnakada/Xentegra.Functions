using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.Constants;
using Xentegra.Models.DTO;
using Xentegra.Models.Interfaces;

namespace Xentegra.Models
{
    public class DemoRequest : DomainModelBase<DemoRequest>, IPartitionKey
    {
        public string requestType { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string company { get; set; }
        public TechnologyDTO technology { get; set; }
        public string requestStatus { get; set; }
        public void SetPartitionKey()
        {
            this.pk = technology.name;
        }

        public DemoRequest SetEntity(DemoRequest entity)
        {
            requestType = entity.requestType;
            name = entity.name;
            email = entity.email;
            phone = entity.phone;
            company = entity.company;
            technology = entity.technology;
            requestStatus = entity.requestStatus;

            entityType = GetEntityType();

            //Only save the necessary information.
            technology = new()
            {
                id = entity.technology.id,
                name = entity.technology.name,
            };

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
