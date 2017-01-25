using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    [Serializable]
    public class RemoteInitializeLyniconAdmin : RemoteProjectContextCommand
    {
        public void Run(string password)
        {
            ProjectContextLoader.InitialiseDataApi(Message);

            using (AppConfig.Change(ProjectContextLoader.WebConfigPath))
            {
                try
                {
                    Assembly lynicon = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.ToLower() == "lynicon");
                    Type secmType = lynicon.GetType("Lynicon.Membership.LyniconSecurityManager");
                    MethodInfo ensureAdminUser = secmType.GetMethod("EnsureAdminUser", BindingFlags.Public | BindingFlags.Static);
                    ensureAdminUser.Invoke(null, new object[] { password });
                }
                catch (Exception ex)
                {
                    Message(new MessageEventArgs(ex));
                }
            }

            Message(new MessageEventArgs(MessageType.Output, "Created admin user, username: administrator, email: admin@lynicon-app.com, with supplied password"));
        }

    }
}
