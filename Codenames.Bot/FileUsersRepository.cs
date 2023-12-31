using Telegram.Bot.Types;

namespace Codenames.Bot
{
    public class SqlUsersRepository : IUsersRepostiory
    {
        public void Add(long id, string displayName)
        {
            using var dbContext = new CodenamesDbContext();

            var currentUser = dbContext.Users.Find(id);
            if (currentUser != null)
            {
                if (currentUser.IsDisabled)
                {
                    currentUser.IsDisabled = false;
                    dbContext.SaveChanges();
                }
            }
            else
            {
                var user = new User
                {
                    Id = id,
                    Name = displayName,
                };
                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            }

        }

        public void Delete(long id)
        {
            using var dbContext = new CodenamesDbContext();

            var currentUser = dbContext.Users.Find(id);
            if (currentUser == null)
                return;

            currentUser.IsDisabled = true;
            dbContext.SaveChanges();
        }

        public ICollection<long> GetAllIds()
        {
            using var dbContext = new CodenamesDbContext();

            return dbContext.Users.Where(x => !x.IsDisabled).Select(x => x.Id).ToArray();
        }

        public bool Has(long id)
        {
            using var dbContext = new CodenamesDbContext();

            var user = dbContext.Users.Find(id);
            return user != null && !user.IsDisabled;
        }
    }

    public class FileUsersRepository : IUsersRepostiory
    {
        private readonly string filename = "users.txt";
        private readonly HashSet<long> ids = new HashSet<long>();
        private TextWriter writer;

        public FileUsersRepository()
        {
            if (!System.IO.File.Exists(filename))
                System.IO.File.Create(filename);

            ids = System.IO.File.ReadLines(filename).Select(value => long.Parse(value)).ToHashSet();

            writer = System.IO.File.AppendText(filename);
        }

        public ICollection<long> GetAllIds()
        {
            return ids;
        }

        public void Add(long id, string displayName)
        {
            if (ids.Contains(id))
                return;
               
            ids.Add(id);
            writer.WriteLine(id.ToString());
        }

        public bool Has(long id)
        {
            return ids.Contains(id);
        }

        public void Delete(long id)
        {
            ids.Remove(id);
        }
    }
}
