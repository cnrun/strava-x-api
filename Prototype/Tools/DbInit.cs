using Prototype.Model;

namespace Prototype.Tools
{    
    public class DbInit
    {
        static public int EnsureCreated(string[] args)
        {
            using (StravaXApiContext db = new StravaXApiContext())
            {
                // https://stackoverflow.com/questions/38238043/how-and-where-to-call-database-ensurecreated-and-database-migrate
                db.Database.EnsureCreated();
            }
            return 0;
        }
    }
}
