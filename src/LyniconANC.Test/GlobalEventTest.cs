using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Xunit;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace LyniconANC.Test
{
    public class GlobalEventTest
    {
        [Fact]
        public void ConstraintOrderedCollection()
        {
            var list = new ConstraintOrderedCollection<EventHubData>(ehd => ehd.EventName);
            list.Add(new EventHubData { EventName = "e1" });
            list.Add(new EventHubData { EventName = "e4" });
            list.Add(new EventHubData { EventName = "e2" }, new string[] { "e1" }, new string[] { "e3", "e4" });
            list.Add(new EventHubData { EventName = "e3" }, ConstraintType.ItemsAfter, "e4");
        }

        [Fact]
        public void GlobalEvents()
        {
            EventHub testHub = new EventHub();
            testHub.RegisterEventProcessor("Test.Var1.Var2", ehd => { ((Stack<string>)ehd.Data).Push("mod1"); return ehd.Data; }, "mod1");
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod2"); return ehd.Data; }, "mod2");
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod4"); return ehd.Data; }, "mod4");
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod3"); return ehd.Data; }, "md3",
                new OrderConstraint("md3", ConstraintType.ItemsAfter, "mod4"));
            testHub.RegisterEventProcessor("Test", ehd => { ((Stack<string>)ehd.Data).Push("mod5"); return ehd.Data; }, "mod5",
                new OrderConstraint("mod5", "mod4"));
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod25"); return ehd.Data; }, "mod25",
                new OrderConstraint("mod25", new string[] { "mod2" }, new string[] { "md3", "mod4" }));
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod2.x"); return ehd.Data; }, "mod2.x");
            testHub.RegisterEventProcessor("Test.Var1", ehd => { ((Stack<string>)ehd.Data).Push("mod2.x.y"); return ehd.Data; }, "mod2.x.y");

            Stack<string> callOrder;
            List<string> callList;

            callOrder = (Stack<string>)testHub.ProcessEvent("Test", this, new Stack<string>()).Data;
            Assert.True(callOrder.SequenceEqual(new string[] { "mod5" }));

            callOrder = (Stack<string>)testHub.ProcessEvent("Test.Var1", this, new Stack<string>()).Data;
            callList = callOrder.Reverse().ToList();
            Assert.True(CheckOrderA(callList), "Test.Var1 event sequencing error");

            callOrder = (Stack<string>)testHub.ProcessEvent("Test.Var1.Var2", this, new Stack<string>()).Data;
            callList = callOrder.Reverse().ToList();
            Assert.True(CheckOrderA(callList));
            Assert.True(callList.Contains("mod1"));

        }

        private bool CheckOrderA(List<string> callList)
        {
            return callList.Contains("mod2") && callList.Contains("mod3") && callList.Contains("mod4") && callList.Contains("mod5") && callList.Contains("mod25")
                    && callList.IndexOf("mod3") < callList.IndexOf("mod4")
                    && callList.IndexOf("mod5") > callList.IndexOf("mod4")
                    && callList.IndexOf("mod25") > callList.IndexOf("mod2")
                    && callList.IndexOf("mod25") < callList.IndexOf("mod3")
                    && callList.IndexOf("mod25") < callList.IndexOf("mod4")
                    && callList.IndexOf("mod2.x") < callList.IndexOf("mod25")
                    && callList.IndexOf("mod2.x.y") < callList.IndexOf("mod25");
        }
    }
}
