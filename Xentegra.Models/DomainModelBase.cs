using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models
{
    public class DomainModelBase
    {
        public string id { get; set; }

        public DateTime createDateTime { get; set; } = DateTime.Now;
        public DateTime lastChangedDateTime { get; set; }

        public string pk { get; set; }

        public void OnCreated(string? _id = null)
        {
            if (string.IsNullOrEmpty(_id))
                this.id = Guid.NewGuid().ToString();
            else
                this.id = _id.Trim().ToUpper();

            this.createDateTime = DateTime.Now;
            this.lastChangedDateTime = this.createDateTime;
        }

        public void OnChanged()
        {
            this.lastChangedDateTime = DateTime.Now;
        }
    }
}
