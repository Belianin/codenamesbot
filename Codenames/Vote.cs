using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames
{
    public class Vote
    {        
        public User User { get; set; }
        public string Word { get; set; }
        public Game Game { get; set; }
    }
}
