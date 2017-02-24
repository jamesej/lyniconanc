using Lynicon.Extensibility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Test
{
    [TestFixture]
    public class EventHubTests
    {
        [Test]
        public void SimpleEvents()
        {
            var eh = new EventHub();
            List<string> eventsCalled = null;

            eh.RegisterEventProcessor("ev1",
                ehd =>
                {
                    eventsCalled.Add("ev1");
                    return ehd.Data;
                }, "TestMod");
        }

        
    }
}
