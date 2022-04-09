using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models.DTO
{
    public class DemoRequestDTO
    {
        public string id { get; set; }

        public string requestType { get; set; }

        public string name { get; set; }

        public string email { get; set; }

        public string phone { get; set; }
        public string company { get; set; }

        public string requestStatus { get; set; }
        public string pk { get; set; }
        public TechnologyDTO technology { get; set; }

        public string GetPartitionKey()
        {
            return technology.name;
        }
    }
}
