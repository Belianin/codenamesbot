using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    public class GameWinners
    {
        public int Place { get; set; }
        public int Votes { get; set; }
        public Dictionary<string, ICollection<User>> Answers { get; set; }
    }
}
