﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Events
{
    public static class Library
    {
        private static List<ScheduledEvent> Events;
        private static bool Initialized = false;

        private static void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;

            Events = FileUtils.LoadJsonListFromDirectory<ScheduledEvent>(ContentPaths.events, null, p => p.Name);
            Console.WriteLine("Loaded Event Library.");
        }

        public static IEnumerable<ScheduledEvent> Enumerate()
        {
            Initialize();
            return Events;
        }
    }
}
