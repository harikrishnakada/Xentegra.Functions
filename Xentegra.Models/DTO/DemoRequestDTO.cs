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
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string emailAddress { get; set; }
        public string phoneNumber { get; set; }
        public TechnologyDTO technology { get; set; }
        public string requestStatus { get; set; }
        public string addtionalNotes { get; set; }
        public string pk { get; set; }
        public string GetPartitionKey()
        {
            return technology.name;
        }
    }
}
