using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    public interface IUsersRepostiory
    {
        ICollection<long> GetAllIds();
        void Add(long id, string displayName);
        bool Has(long id);
        void Delete(long id);
    }
}
