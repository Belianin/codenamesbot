using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsAdmin { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
