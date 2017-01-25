using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;

namespace Lynicon.Tools
{
    // Windows PowerShell assembly.

    // Declare the class as a cmdlet and specify and 
    // appropriate verb and noun for the cmdlet name.
    [Cmdlet(VerbsData.Initialize, "LyniconAdmin")]
    public class InitializeLyniconAdminCommand : Cmdlet
    {
        // Declare the parameters for the cmdlet.
        [Parameter(Mandatory = true)]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        private string password;

        protected override void ProcessRecord()
        {
            new InitializeLyniconAdmin(this).LocalRun(Password);
        }
    }

    [Serializable]
    public class InitializeLyniconAdmin : LocalProjectContextCommand<RemoteInitializeLyniconAdmin>
    {
        public InitializeLyniconAdmin(Cmdlet caller) : base(caller)
        { }

        /// <summary>
        /// Setup, run and unload the remote task
        /// </summary>
        public void LocalRun(string password)
        {
            var appDomain = AppDomain.CreateDomain("InitializeLyniconAdmin");

            try
            {
                InitializeNewAppDomain(appDomain);

                remote.Run(password);
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
