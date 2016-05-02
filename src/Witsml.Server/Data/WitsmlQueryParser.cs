﻿//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Witsml200 = Energistics.DataAccess.WITSML200;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    public class WitsmlQueryParser
    {
        private XNamespace _namespace;
        private XDocument _document;
        private IEnumerable<XElement> _elements;

        private WitsmlQueryParser(WitsmlQueryParser parser, XElement element, string objectType)
        {
            Context = new RequestContext(
                function: parser.Context.Function, 
                objectType: objectType,
                xml: element.ToString(),
                options: parser.Context.Options,
                capabilities: parser.Context.Capabilities);

            Options = parser.Options;
            _document = parser._document;
            _namespace = parser._namespace;
            _elements = new[] { element };
        }

        public WitsmlQueryParser(RequestContext context)
        {
            Context = context;
            Options = OptionsIn.Parse(context.Options);
            _document = WitsmlParser.Parse(context.Xml);
            _namespace = _document.Root.GetDefaultNamespace();

            if (_document.Root.Attributes("version").Any())
            {
                _elements = _document.Root.Elements(_namespace + Context.ObjectType);
            }
            else
            {
                _elements = _document.Elements();
            }
        }

        public RequestContext Context { get; private set; }

        public Dictionary<string, string> Options { get; private set; }

        public string ReturnElements()
        {
            return OptionsIn.GetValue(Options, OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Requests the object selection capability.
        /// </summary>
        /// <returns>The capability value.</returns>
        public string RequestObjectSelectionCapability()
        {
            return OptionsIn.GetValue(Options, OptionsIn.RequestObjectSelectionCapability.None);
        }

        /// <summary>
        /// Requests the private group only.
        /// </summary>
        /// <returns></returns>
        public bool RequestPrivateGroupOnly()
        {
            string value = OptionsIn.GetValue(Options, OptionsIn.RequestPrivateGroupOnly.False);
            bool result;

            if (!bool.TryParse(value, out result))
                result = false;

            return result;
        }

        /// <summary>
        /// Requests the maximum number of nodes that can be returned to the client.
        /// </summary>
        /// <returns></returns>
        public int? MaxReturnNodes()
        {
            return Options.ContainsKey(OptionsIn.MaxReturnNodes.Keyword)
                ? int.Parse(Options[OptionsIn.MaxReturnNodes.Keyword])
                : (int?)null;
        }

        /// <summary>
        /// Requests the latest values.
        /// </summary>
        /// <returns></returns>
        public int? RequestLatestValues()
        {
            return Options.ContainsKey(OptionsIn.RequestLatestValues.Keyword)
                ? int.Parse(Options[OptionsIn.RequestLatestValues.Keyword])
                : (int?)null;
        }

        public EtpUri GetUri<T>()
        {
            return GetUri(typeof(T));
        }

        public EtpUri GetUri(Type type)
        {
            var objectType = ObjectTypes.GetObjectType(type);
            var baseUri = EtpUris.GetUriFamily(type);

            if (typeof(Witsml200.ComponentSchemas.AbstractObject).IsAssignableFrom(type))
                return baseUri.Append(objectType, Attribute("uuid"));

            if (typeof(IWellObject).IsAssignableFrom(type))
                baseUri = baseUri.Append(ObjectTypes.Well, Attribute("uidWell"));

            if (typeof(IWellboreObject).IsAssignableFrom(type))
                baseUri = baseUri.Append(ObjectTypes.Wellbore, Attribute("uidWellbore"));

            return baseUri.Append(objectType, Attribute("uid"));
        }

        public IEnumerable<XElement> Elements()
        {
            return _elements;
        }

        public XElement Element()
        {
            return Elements().FirstOrDefault();
        }

        public String Attribute(string name)
        {
            if (HasAttribute(name))
            {
                return (String)Element().Attribute(name);
            }
            return null;
        }

        public bool HasAttribute(string name)
        {
            var element = Element();
            return element != null && element.Attribute(name) != null;
        }

        public bool Contains(string name)
        {
            return Element().Elements(_namespace + name).Any();
        }

        public XElement Property(string name)
        {
            return Element().Elements(_namespace + name).FirstOrDefault();
        }

        public IEnumerable<XElement> Properties(string name)
        {
            return Properties(Element(), name);
        }

        public IEnumerable<XElement> Properties(XElement element, string name)
        {
            return element.Elements(_namespace + name);
        }

        public IEnumerable<XElement> Properties(IEnumerable<XElement> elements, string name)
        {
            return elements.Elements(_namespace + name);
        }

        public bool HasElements(string name)
        {
            return HasElements(Element(), name);
        }

        public bool HasElements(XElement element, string name)
        {
            return element != null &&
                element.Elements(_namespace + name).Any();
        }

        public string PropertyValue(string name)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return PropertyValue(Element(), name);
        }

        public string PropertyValue(XElement element, string name)
        {
            if (!HasElements(element, name))
            {
                return null;
            }
            return element
                .Elements(_namespace + name)
                .Select(e => e.Value)
                .FirstOrDefault();
        }

        public string PropertyAttribute(string name, string attribute)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return Element()
                .Elements(_namespace + name)
                .Select(e => (String)e.Attribute(attribute))
                .FirstOrDefault();
        }

        public WitsmlQueryParser Fork(XElement element, string objectType)
        {
            return new WitsmlQueryParser(this, element, objectType);
        }

        public IEnumerable<WitsmlQueryParser> Fork(IEnumerable<XElement> elements, string objectType)
        {
            foreach (var element in elements)
                yield return Fork(element, objectType);
        }

        public IEnumerable<WitsmlQueryParser> ForkProperties(string name, string objectType)
        {
            return Fork(Properties(name), objectType);
        }

        public IEnumerable<WitsmlQueryParser> ForkElements()
        {
            return Fork(Elements(), Context.ObjectType);
        }
    }
}
