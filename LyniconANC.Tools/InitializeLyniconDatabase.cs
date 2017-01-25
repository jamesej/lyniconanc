using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using EnvDTE;
using System.Reflection;
using System.IO;

namespace Lynicon.Tools
{
    // Windows PowerShell assembly.

    // Declare the class as a cmdlet and specify and 
    // appropriate verb and noun for the cmdlet name.
    [Cmdlet(VerbsData.Initialize, "LyniconDatabase")]
    public class InitializeLyniconDatabaseCommand : Cmdlet
    {
        protected override void ProcessRecord()
        {
            new InitializeLyniconDatabase(this).LocalRun();
        }
    }

    [Serializable]
    public class InitializeLyniconDatabase : LocalProjectContextCommand<RemoteInitializeLyniconDatabase>
    {
        public InitializeLyniconDatabase(Cmdlet caller) : base(caller)
        {
        }

        /// <summary>
        /// Setup, run and unload the remote task
        /// </summary>
        public void LocalRun()
        {
            var appDomain = AppDomain.CreateDomain("InitializeLyniconDatabase");

            try
            {
                InitializeNewAppDomain(appDomain);

                remote.Run();
            }
            catch (Exception ex)
            {
                MessageHandler.Handle(this.caller, new MessageEventArgs(ex));
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }
    }
}
