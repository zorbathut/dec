using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dec
{
    internal class WriterXmlRecord : WriterXml
    {
        public override bool AllowReflection { get => false; }
        public override bool AllowDecPath { get => true; }
        public override Recorder.IUserSettings UserSettings { get; }

        // Maps between object and the in-place element. This does *not* yet have the ref ID tagged, and will have to be extracted into a new Element later.
        private Dictionary<object, (XElement element, Path path)> refToElement = new Dictionary<object, (XElement, Path)>();
        private Dictionary<XElement, object> elementToRef = new Dictionary<XElement, object>();

        // Every ref name we've handed out, for the lifetime of this writer. An object is named at most once; a name is never reused.
        private Dictionary<object, string> refNames = new Dictionary<object, string>();

        // Every name we've handed out, generated or user-provided; ref names must be unique within a file.
        private HashSet<string> usedRefNames = new HashSet<string>();

        // The objects whose contents still need to be hoisted out of the tree and into the refs block, drained on every strip pass.
        private List<object> refsPendingHoist = new List<object>();

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

        // The single place a ref name is minted. Names the object and queues its contents for hoisting into the refs block.
        private string RefNameAcquire(object referenced)
        {
            string name = null;

            if (referenced is IRefName refName)
            {
                name = RefNameValidate(refName.RefName(UserSettings), referenced);
            }

            while (name == null)
            {
                // A user may have claimed a name out of the generated namespace, so keep going until we find a free slot.
                string candidate = $"ref{referenceId++:D5}";
                if (usedRefNames.Add(candidate))
                {
                    name = candidate;
                }
            }

            refNames[referenced] = name;
            refsPendingHoist.Add(referenced);

            return name;
        }

        // Vets a user-provided ref name, returning null if we need to fall back on a generated one.
        private string RefNameValidate(string name, object referenced)
        {
            if (name == null)
            {
                // No opinion offered, which is not a mistake; that's what generated names are for.
                return null;
            }

            if (Database.GetFromDecPath(name) != null)
            {
                Dbg.Wrn($"[{refToElement[referenced].path.Serialize()}]: Ref name `{name}` for {referenced.GetType()} is also a dec path, and would be read back as that dec; falling back on a generated name");
                return null;
            }

            if (!usedRefNames.Add(name))
            {
                Dbg.Wrn($"[{refToElement[referenced].path.Serialize()}]: Ref name `{name}` for {referenced.GetType()} is already in use; falling back on a generated name");
                return null;
            }

            return name;
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Settings recSettings, Path path)
        {
            bool forceProcess = false;

            if (!refToElement.TryGetValue(referenced, out var xe_path))
            {
                if (recSettings.shared != Recorder.Settings.Shared.Deny)
                {
                    // Insert it into our refToElement mapping
                    refToElement[referenced] = (element, path);
                    elementToRef[element] = referenced;

                    if (referenced is IRefForce)
                    {
                        // Named here so the strip pass in Finish() hoists it, the same way the depth limiter does. This has to follow the registration above, whose path RefNameValidate reads, and precede the refNames lookup below, which would otherwise mint a second name.
                        RefNameAcquire(referenced);
                    }
                }
                else
                {
                    // Cannot be referenced, so we insert a fake null entry but including the path for tracking
                    refToElement[referenced] = (null, path);

                    // Note: It is important not to add an elementToRef entry because this is later used to split long hierarchies
                    // and if you split a long hierarchy around a non-referencable barrier, everything breaks!

                    if (referenced is IRefForce)
                    {
                        Dbg.Wrn($"[{path.Serialize()}]: {referenced.GetType()} implements IRefForce, but this position doesn't allow shared references; writing it inline instead");
                    }
                }

                if (Config.TestRefEverything && recSettings.shared != Recorder.Settings.Shared.Deny)
                {
                    // Test pathway that should only occur during testing.
                    xe_path = (element, path);
                    forceProcess = true;
                }
                else
                {
                    return false;
                }
            }

            if (xe_path.element == null)
            {
                // This is an unreferencable object! We are in trouble.
                WriterNode.ErrReferenceMismatch(priorWasShared: false, path, xe_path.path, referenced);
                return true;
            }

            // We have a referenceable target, but do *we* allow a reference?
            if (recSettings.shared == Recorder.Settings.Shared.Deny)
            {
                WriterNode.ErrReferenceMismatch(priorWasShared: true, path, xe_path.path, referenced);
                return true;
            }

            var refId = refNames.TryGetValue(referenced);
            if (refId == null)
            {
                // We already had a reference, but we don't have a string ID for it. We need one now though!
                refId = RefNameAcquire(referenced);
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
            foreach (var pending in refsPendingHoist)
            {
                string refName = refNames[pending];

                var result = new XElement("Ref");
                result.SetAttributeValue("id", refName);

                var src = refToElement[pending];

                // gotta ToArray() because it does not like mutating things while iterating
                // And yes, you have to .Remove() also, otherwise you get copies in both places.
                foreach (var attribute in src.element.Attributes().ToArray())
                {
                    attribute.Remove();

                    // We will normally not have a ref attribute here, but if we're doing the ref-everything mode, we might.
                    if (attribute.Name != "ref")
                    {
                        result.Add(attribute);
                    }
                }

                foreach (var node in src.element.Nodes().ToArray())
                {
                    node.Remove();
                    result.Add(node);
                }

                // Patch in the ref link
                src.element.SetAttributeValue("ref", refName);

                // We may not have had a class to begin with, but we sure need one now!
                result.SetAttributeValue("class", pending.GetType().ComposeDecFormatted());

                yield return new KeyValuePair<string, XElement>(refName, result);
            }

            // We're now done processing this segment and can erase it; we don't want to try doing this a second time!
            refsPendingHoist.Clear();
        }

        public bool ProcessDepthLimitedReferences(XElement node, int depthRemaining)
        {
            // An object we've already named has already been hoisted; its element is still in elementToRef, but it's an empty stub by now and stripping it again would produce a second, contentless Ref.
            if (depthRemaining <= 0 && elementToRef.TryGetValue(node, out var referenced) && !refNames.ContainsKey(referenced))
            {
                RefNameAcquire(referenced);
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

        public WriterNodeXml StartRecord(Type type)
        {
            var node = WriterNodeXml.StartRecord(this, record, "data", type, "RECORD");
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
