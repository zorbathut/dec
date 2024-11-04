using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dec
{
    internal class WriterXmlRecord : WriterXml
    {
        public override bool AllowReflection { get => false; }
        public override Recorder.IUserSettings UserSettings { get; }

        // Maps between object and the in-place element. This does *not* yet have the ref ID tagged, and will have to be extracted into a new Element later.
        private Dictionary<object, XElement> refToElement = new Dictionary<object, XElement>();
        private Dictionary<XElement, object> elementToRef = new Dictionary<XElement, object>();

        // A map from object to the string intended as a reference. This will be filled in only once a second reference to something is created.
        // This is cleared after we resolve references, then re-used for the depth capping code.
        private Dictionary<object, string> refToString = new Dictionary<object, string>();

        // Current reference ID that we're on.
        private int referenceId = 0;

        private XDocument doc;
        private XElement record;
        private XElement refs;
        private XElement rootElement;

        public WriterXmlRecord(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();

            record = new XElement("Record");
            doc.Add(record);

            record.Add(new XElement("recordFormatVersion", 1));

            refs = new XElement("refs");
            record.Add(refs);
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            bool forceProcess = false;

            if (!refToElement.TryGetValue(referenced, out var xelement))
            {
                if (recContext.shared != Recorder.Context.Shared.Deny)
                {
                    // Insert it into our refToElement mapping
                    refToElement[referenced] = element;
                    elementToRef[element] = referenced;
                }
                else
                {
                    // Cannot be referenced, so we insert a fake null entry
                    refToElement[referenced] = null;

                    // Note: It is important not to add an elementToRef entry because this is later used to split long hierarchies
                    // and if you split a long hierarchy around a non-referencable barrier, everything breaks!
                }

                if (Config.TestRefEverything && recContext.shared != Recorder.Context.Shared.Deny)
                {
                    // Test pathway that should only occur during testing.
                    xelement = element;
                    forceProcess = true;
                }
                else
                {
                    return false;
                }
            }

            if (xelement == null)
            {
                // This is an unreferencable object! We are in trouble.
                Dbg.Err("Attempted to create a new reference to an unshared object. This may result in an invalid serialization. If this is coming from a Recorder setup, perhaps you need a .Shared() decorator.");
                return true;
            }

            // We have a referenceable target, but do *we* allow a reference?
            if (recContext.shared == Recorder.Context.Shared.Deny)
            {
                Dbg.Err("Attempted to create a new unshared reference to a previously-seen object. This may result in an invalid serialization. If this is coming from a Recorder setup, it's likely you either need a .Shared() decorator, or you need to ensure that this object is not serialized elsewhere.");
                return true;
            }

            var refId = refToString.TryGetValue(referenced);
            if (refId == null)
            {
                // We already had a reference, but we don't have a string ID for it. We need one now though!
                refId = $"ref{referenceId++:D5}";
                refToString[referenced] = refId;
            }

            // Tag the XML element properly
            element.SetAttributeValue("ref", refId);

            // And we're done!
            // If we're forcing auto-ref'ing, then we allow processing this; otherwise, we tell it to skip because it's already done.
            return !forceProcess;
        }

        public IEnumerable<KeyValuePair<string, XElement>> StripAndOutputReferences()
        {
            // It is *vitally* important that we do this step *after* all references are generated, not inline as we add references.
            // This is because we have to move all the contents of the XML element, but if we do it during generation, a recursive-reference situation could result in us trying to move the XML element before its contents are fully generated.
            // So we do it now, when we know that everything is finished.
            foreach (var refblock in refToString)
            {
                var result = new XElement("Ref");
                result.SetAttributeValue("id", refblock.Value);

                var src = refToElement[refblock.Key];

                // gotta ToArray() because it does not like mutating things while iterating
                // And yes, you have to .Remove() also, otherwise you get copies in both places.
                foreach (var attribute in src.Attributes().ToArray())
                {
                    attribute.Remove();

                    // We will normally not have a ref attribute here, but if we're doing the ref-everything mode, we might.
                    if (attribute.Name != "ref")
                    {
                        result.Add(attribute);
                    }
                }

                foreach (var node in src.Nodes().ToArray())
                {
                    node.Remove();
                    result.Add(node);
                }

                // Patch in the ref link
                src.SetAttributeValue("ref", refblock.Value);

                // We may not have had a class to begin with, but we sure need one now!
                result.SetAttributeValue("class", refblock.Key.GetType().ComposeDecFormatted());

                yield return new KeyValuePair<string, XElement>(refblock.Value, result);
            }

            // We're now done processing this segment and can erase it; we don't want to try doing this a second time!
            refToString.Clear();
        }

        public bool ProcessDepthLimitedReferences(XElement node, int depthRemaining)
        {
            if (depthRemaining <= 0 && elementToRef.ContainsKey(node))
            {
                refToString[elementToRef[node]] = $"ref{referenceId++:D5}";
                // We don't continue recursively because then we're threatening a stack overflow; we'll get it on the next pass

                return true;
            }
            else if (depthRemaining <= -100)
            {
                Dbg.Err("Depth limiter ran into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting. Generated file may not be readable (ask on Discord if you need this) and is likely to be very inefficient.");
                return false;
            }
            else
            {
                bool found = false;
                foreach (var child in node.Elements())
                {
                    found |= ProcessDepthLimitedReferences(child, depthRemaining - 1);
                }

                return found;
            }
        }

        public WriterNodeXml StartData(Type type)
        {
            var node = WriterNodeXml.StartData(this, record, "data", type);
            rootElement = node.GetXElement();
            return node;
        }

        public string Finish(bool pretty)
        {
            // Handle all our pending writes
            DequeuePendingWrites();

            // We now have a giant XML tree, potentially many thousands of nodes deep, where some nodes are references and some *should* be in the reference bank but aren't.
            // We need to do two things:
            // * Make all of our tagged references into actual references in the Refs section
            // * Tag anything deeper than a certain depth as a reference, then move it into the Refs section
            var depthTestsPending = new List<XElement>();
            depthTestsPending.Add(rootElement);

            // This is a loop between "write references" and "tag everything below a certain depth as needing to be turned into a reference".
            // We do this in a loop so we don't have to worry about ironically blowing our stack while making a change required to not blow our stack.
            while (true)
            {
                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in StripAndOutputReferences().OrderBy(kvp => kvp.Key))
                {
                    refs.Add(reference.Value);
                    depthTestsPending.Add(reference.Value);
                }

                bool found = false;
                for (int i = 0; i < depthTestsPending.Count; ++i)
                {
                    // Magic number should probably be configurable at some point
                    found |= ProcessDepthLimitedReferences(depthTestsPending[i], 20);
                }
                depthTestsPending.Clear();

                if (!found)
                {
                    // No new depth-clobbering references found, just move on
                    break;
                }
            }

            if (refs.IsEmpty)
            {
                // strip out the refs 'cause it looks better that way :V
                refs.Remove();
            }

            if (!pretty)
            {
                doc.AddFirst(new XComment("Pretty-print can be enabled as a parameter of the Recorder.Write() call."));
            }
            doc.AddFirst(new XComment("This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)"));

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
        }
    }
}
