using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames
{
    public class Answer
    {
        public string Word { get; set; }
        public User User { get; set; }
        public Game Game { get; set; }
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public DateTime CreationDate { get; set; }
    }
}
