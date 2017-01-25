using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lynicon.Utility;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Class of object sent between processors of a global event
    /// </summary>
    public class EventHubData
    {
        /// <summary>
        /// Object sending event
        /// </summary>
        public object Sender { get; set; }
        /// <summary>
        /// Name of the event being sent
        /// </summary>
        public string EventName { get; set; }
        /// <summary>
        /// Arbitrary data sent
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Utility to extract values from Data where this is a Dictionary<string, object>
        /// </summary>
        /// <typeparam name="T">type of item to get from the dictionary</typeparam>
        /// <param name="key">key of item in dictionary</param>
        /// <returns>The value of the key provided</returns>
        public T GetDataItem<T>(string key)
        {
            if (!(Data is Dictionary<string, object>))
                return default(T);
            else
            {
                var dict = (Dictionary<string, object>)Data;
                if (!dict.ContainsKey(key))
                    return default(T);
                else
                    return (T)dict[key];
            }
        }
    }
    
    /// <summary>
    /// EventHub is a powerful global event manager.  Any code can register handlers (known as processors) for events.  Processors
    /// can be registered with order of execution constraints with relation to other processors which may exist now or in the future in the
    /// processing queue.  Events have hierarchical names, and processors can be registered for a group of events.  Processors also
    /// can have hierarchical names so that constraints can apply to groups of processors.
    /// </summary>
    public class EventHub
    {
        private class ProcessorInfo
        {
            public string ModuleId { get; set; }
            public string EventName { get; set; }
            public Func<EventHubData, object> Processor { get; set; }
        }

        static readonly EventHub instance = new EventHub();
        public static EventHub Instance { get { return instance; } }

        static EventHub() { }

        Dictionary<string, ConstraintOrderedCollection<ProcessorInfo>> Processors = new Dictionary<string, ConstraintOrderedCollection<ProcessorInfo>>();

        /// <summary>
        /// Register a function to process event data on raising of an event elsewhere
        /// </summary>
        /// <typeparam name="TEventData">The event data processed in turn by the processors</typeparam>
        /// <param name="eventName">The name of the event being raised</param>
        /// <param name="processor">A delegate which takes the current event data and the sender and returns the processed event data</param>
        /// <param name="moduleId">The name of the module. This can be moduletype.module</param>
        public void RegisterEventProcessor(
            string eventName,
            Func<EventHubData, object> processor,
            string moduleId)
        {
            RegisterEventProcessor(eventName, processor, moduleId, new OrderConstraint());
        }
        /// <summary>
        /// Register a function to process event data on raising of an event elsewhere ensuring specified processes on which it
        /// is dependent are performed first
        /// </summary>
        /// <param name="eventName">The name of the event being raised</param>
        /// <param name="processor">A delegate which takes the current event data and the sender and returns the processed event data</param>
        /// <param name="moduleId">The name of the module. This can be moduletype.module</param>
        /// <param name="constraint">An order constraint specifying restrictions on the relative position of this processor in any chain of processors</param>
        public void RegisterEventProcessor(
            string eventName,
            Func<EventHubData, object> processor,
            string moduleId,
            OrderConstraint constraint)
        {
            var newProcessor = new ProcessorInfo
            {
                EventName = eventName,
                ModuleId = moduleId,
                Processor = processor
            };

            // register processor on all processor queues where event is or covers a subset of this event
            var subEvents = Processors.Keys.Where(k => k.StartsWith(eventName)).ToList();
            bool matched = false;
            foreach (string subEvent in subEvents)
            {
                if (Processors[subEvent].Any(p => p.ModuleId == newProcessor.ModuleId))
                    throw new Exception("Trying to add more than 1 processor for module " + newProcessor.ModuleId + " to event queue " + subEvent);
                Processors[subEvent].Add(newProcessor, constraint);
                matched = (subEvent == eventName);
            }

            // if not regstered anywhere, create a new processor queue containing the processes from the least super event of this one
            if (!matched)
            {
                var leastSuperEvent = LeastSuperEvent(eventName);

                var pList = new ConstraintOrderedCollection<ProcessorInfo>(pi => pi.ModuleId);
                if (leastSuperEvent != null)
                    pList = Processors[leastSuperEvent].Copy();

                pList.Add(newProcessor, constraint);

                Processors.Add(eventName, pList);
            }
        }

        /// <summary>
        /// Remove an event processor
        /// </summary>
        /// <param name="eventName">The name of the event the processor was registered for</param>
        /// <param name="moduleId">The name of the module (this could have been moduletype.module)</param>
        public void DeregisterEventProcessor(string eventName, string moduleId)
        {
            Processors
                .Where(kvp => kvp.Key.StartsWith(eventName))
                .Do(p =>
                    {
                        var removed = p.Value.FirstOrDefault(pi => pi.ModuleId == moduleId && pi.EventName == eventName);
                        if (removed != null)
                            p.Value.Remove(removed);
                    });
        }

        /// <summary>
        /// Find the most specific event queue hierarchical event name which
        /// contains or matches the given hierarchical event name
        /// </summary>
        /// <param name="eventName">The event name to search for</param>
        /// <returns>The most specific event queue name which contains or matches the given event name</returns>
        private string LeastSuperEvent(string eventName)
        {
            string leastSuperEvent = null;
            foreach (string en in Processors.Keys)
                if ((eventName == en || eventName.StartsWith(en + "."))
                    && (leastSuperEvent == null || en.Length > leastSuperEvent.Length))
                    leastSuperEvent = en;
            return leastSuperEvent;
        }

        /// <summary>
        /// Raise an event with event data for processing
        /// </summary>
        /// <typeparam name="TEventData">The type of the event data</typeparam>
        /// <param name="eventName">The name of the event being raised</param>
        /// <param name="sender">The sender of the event</param>
        /// <param name="eventData">The data which will be processed by all relevant processors</param>
        /// <returns>The event data after all processing</returns>
        public EventHubData ProcessEvent(string eventName, object sender, object eventData)
        {
            EventHubData data = new EventHubData
            {
                EventName = eventName,
                Sender = sender,
                Data = eventData
            };

            string leastSuperEvent = LeastSuperEvent(data.EventName);
            if (leastSuperEvent == null)
                return data;

            foreach (var proc in Processors[leastSuperEvent])
            {
                data.Data = proc.Processor(data);
            }

            return data;
        }

        ConcurrentDictionary<string, CancellationTokenSource> timedEventCancellations = new ConcurrentDictionary<string, CancellationTokenSource>();

        /// <summary>
        /// Raise/process an event at a fixed frequency with an optional random offset to avoid unwanted
        /// synchronisations
        /// </summary>
        /// <param name="eventName">Event name to process</param>
        /// <param name="period">Period between events</param>
        /// <param name="periodVariance">Maximum additional period to add to this - added period varies between 0 and this maximum randomly</param>
        public void GenerateTimedEvent(string eventName, TimeSpan period, TimeSpan periodVariance)
        {
            if (timedEventCancellations.ContainsKey(eventName))
                timedEventCancellations[eventName].Cancel();

            var cts = new CancellationTokenSource();
            timedEventCancellations[eventName] = cts;
            var token = cts.Token;
            Random r = new Random();

            Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        ProcessEvent(eventName, this, null);
                        var actualPeriod = period + TimeSpan.FromTicks((int)(periodVariance.Ticks * r.NextDouble()));
                        Task.Delay(actualPeriod, token);
                    }
                    token.ThrowIfCancellationRequested();
                }, cts.Token);
        }

        /// <summary>
        /// Cancel the production of timed events for a given event name
        /// </summary>
        /// <param name="eventName">Event name for which to cancel timed events</param>
        public void CancelTimedEvent(string eventName)
        {
            if (timedEventCancellations.ContainsKey(eventName))
            {
                timedEventCancellations[eventName].Cancel();
                CancellationTokenSource cts;
                timedEventCancellations.TryRemove(eventName, out cts);
            }
        }
    }
}
