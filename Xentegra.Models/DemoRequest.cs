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
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string emailAddress { get; set; }
        public string phoneNumber { get; set; }
        public TechnologyDTO technology { get; set; }
        public string requestStatus { get; set; }
        public string addtionalNotes { get; set; }
        public void SetPartitionKey()
        {
            this.pk = technology.name;
        }

        public DemoRequest SetEntity(DemoRequest entity)
        {
            requestType = entity.requestType;
            firstName = entity.firstName;
            lastName = entity.lastName;
            emailAddress = entity.emailAddress;
            phoneNumber = entity.phoneNumber;
            technology = entity.technology;
            requestStatus = entity.requestStatus;
            addtionalNotes = entity.addtionalNotes;

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
