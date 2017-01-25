using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    [Serializable]
    public class RemoteInitializeLyniconDatabase : RemoteProjectContextCommand
    {
        public void Run()
        {
            ProjectContextLoader.EnsureLoaded(Message);

            string actionsList = null;
            using (AppConfig.Change(ProjectContextLoader.WebConfigPath))
            {
                dynamic pdb = Activator.CreateInstance("Lynicon", "Lynicon.Repositories.PreloadDb").Unwrap();
                try
                {
                    actionsList = (string)pdb.EnsureCoreDb();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("transient failure") || ex.Message.Contains("failed on Open"))
                        Message(new MessageEventArgs(new Exception("The database may not yet exist: please ensure it is created", ex)));
                    else
                        Message(new MessageEventArgs(ex));
                }
            }

            Message(new MessageEventArgs(MessageType.Output, "Initialised Successfully: " + (actionsList ?? "no actions taken")));
        }
    }
}
