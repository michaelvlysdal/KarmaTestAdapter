﻿using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KarmaTestAdapter.TestResults
{
    public class Test : TestItem
    {
        public Test(Item parent, XElement element)
            : base(parent, element)
        {
        }

        public string Framework { get { return Attribute("Framework"); } }
        public int? Line { get { return Attribute("Line").ToInt(); } }
        public int? Column { get { return Attribute("Column").ToInt(); } }
        public int? Index { get { return Attribute("Index").ToInt(); } }

        public string FullyQualifiedName
        {
            get
            {
                // We need Index in the fully qualified name to differentiate between tests with identical names
                var fullyQualifiedName = string.Format("{0}#{1}", Name.Replace('.', '-'), Index);
                if (ParentSuite != null)
                {
                    fullyQualifiedName = ParentSuite.FullyQualifiedName + "." + fullyQualifiedName;
                }
                return fullyQualifiedName;
            }
        }
    }
}
