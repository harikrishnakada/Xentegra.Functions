using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models.DTO
{
    public class TechnologyDTO
    {
        public string id { get; set; }

        public string name { get; set; }

        public string resourceGroupName { get; set; }
        public string pk { get; set; }
        public string entityType { get; set; }

        public string GetPartitionKey()
        {
            return entityType;
        }
    }
}
