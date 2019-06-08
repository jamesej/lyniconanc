using Lynicon.Repositories;
using Lynicon.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Commands
{
    public class InitializeDatabaseCmd : ToolsCommandBase
    {
        LyniconSystem sys;

        public override string CommandWord
        {
            get
            {
                return "initialize-database";
            }
        }

        public InitializeDatabaseCmd(LyniconSystem sys)
        {
            this.sys = sys;
        }

        public override bool Execute(params string[] args)
        {
            string actionsList = null;

            Console.WriteLine("Found connection string: " + sys.Settings.SqlConnectionString);

            if (sys.Settings.SqlConnectionString == "...")
            {
                Console.WriteLine("Failed: please enter a connection string for the content database in web.config");
                return false;
            }
                
            try
            {
                var pdb = new PreloadDb(builder => sys.Settings.ApplyDbContext(builder, sys.Settings.SqlConnectionString));
                actionsList = (string)pdb.EnsureCoreDb();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("transient failure") || ex.Message.Contains("failed on Open"))
                    Console.WriteLine("The database may not yet exist: please ensure it is created:\n" + ex.ToString());
                else
                    Console.WriteLine(ex.ToString());

                return false;
            }

            Console.WriteLine("Initialised Successfully: " + (actionsList ?? "no actions taken"));
            return true;
        }
    }
}
