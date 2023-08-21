namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Schema;

    /// <summary>
    /// Information on the current cursor position when reading files.
    /// </summary>
    /// <remarks>
    /// Standard output format is $"{node.GetInputContext()}: Your Error Text Here!". This abstracts out the requirements for generating error text.
    /// </remarks>
    public struct InputContext
    {
        internal string filename;
        internal System.Xml.Linq.XElement handle;

        public InputContext(string filename)
        {
            this.filename = filename;
            this.handle = null;
        }

        public InputContext(string filename, System.Xml.Linq.XElement handle)
        {
            this.filename = filename;
            this.handle = handle;
        }

        public override string ToString()
        {
            if (this.handle != null)
            {
                return $"{filename}:{handle.LineNumber()}";
            }
            else
            {
                return filename;
            }
        }
    }

    /// <summary>
    /// Internal serialization utilities.
    /// </summary>
    internal static class Serialization
    {
        // Initialize it to empty in order to support Recorder operations without Dec initialization.
        // At some point we'll figure out how to support Converters at that point as well.
        internal static Dictionary<Type, Converter> Converters = new Dictionary<Type, Converter>();

        internal static void Initialize()
        {
            Converters = new Dictionary<Type, Converter>();

            IEnumerable<Type> conversionTypes;
            if (Config.TestParameters == null)
            {
                conversionTypes = UtilReflection.GetAllTypes().Where(t => t.IsSubclassOf(typeof(Converter)));
            }
            else if (Config.TestParameters.explicitConverters != null)
            {
                conversionTypes = Config.TestParameters.explicitConverters;
            }
            else
            {
                conversionTypes = Enumerable.Empty<Type>();
            }

            foreach (var type in conversionTypes)
            {
                if (type.IsAbstract || type.IsGenericType)
                {
                    // not really valid, just move on
                    // we could do this up in the linq expression but that would be harder to test
                    continue;
                }

                var converter = (Converter)type.CreateInstanceSafe("converter", null);

                if (converter != null && (converter is ConverterString || converter is ConverterRecord || converter is ConverterFactory))
                {
                    Type convertedType = converter.GetConvertedType();
                    if (Converters.ContainsKey(convertedType))
                    {
                        Dbg.Err($"Converters {Converters[convertedType].GetType()} and {type} both generate result {convertedType}");
                    }

                    Converters[convertedType] = converter;
                }
            }
        }

        internal static object GenerateResultFallback(object model, Type type)
        {
            if (model != null)
            {
                return model;
            }
            else if (type.IsValueType)
            {
                // We don't need Safe here because all value types are required to have a default constructor.
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        internal enum ParseMode
        {
            Default,
            Replace,
            //ReplaceOrCreate, // NYI
            Patch,
            //PatchOrCreate, // NYI
            //Create, //NYI
            Append,
            //Delete, //NYI
            //ReplaceIfExists, //NYI
            //PatchIfExists, //NYI
            //DeleteIfExists, //NYI
        }
        internal static ParseMode ParseModeFromString(InputContext context, string str)
        {
            if (str == null)
            {
                return ParseMode.Default;
            }
            else if (str == "replace")
            {
                return ParseMode.Replace;
            }
            else if (str == "patch")
            {
                return ParseMode.Patch;
            }
            else if (str == "append")
            {
                return ParseMode.Append;
            }
            else
            {
                Dbg.Err($"{context}: Invalid `{str}` mode!");

                return ParseMode.Default;
            }
        }

        internal enum ParseCommand
        {
            Replace,
            Patch,
            Append,
        }
        // this interface is pretty janky and takes this form entirely because it's what I needed at the time
        internal static List<(ParseCommand command, T node)> CompileOrders<T>(UtilType.ParseModeCategory modeCategory, IEnumerable<(T, InputContext, ParseMode)> nodes)
        {
            var orders = new List<(ParseCommand command, T payload)>();
            foreach (var (payload, inputContext, s_parseMode) in nodes)
            {
                ParseCommand s_parseCommand;

                switch (modeCategory)
                {
                    case UtilType.ParseModeCategory.Dec:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{inputContext}: Invalid mode {s_parseMode} provided for a Dec-type parse, defaulting to Create");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                                s_parseCommand = ParseCommand.Patch;
                                break;
                        }
                        break;
                    case UtilType.ParseModeCategory.Object:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{inputContext}: Invalid mode {s_parseMode} provided for an Object-type parse, defaulting to Patch");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                            case ParseMode.Patch:
                                s_parseCommand = ParseCommand.Patch;
                                break;
                        }
                        break;
                    case UtilType.ParseModeCategory.OrderedContainer:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{inputContext}: Invalid mode {s_parseMode} provided for an ordered-container-type parse, defaulting to Replace");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                            case ParseMode.Replace:
                                s_parseCommand = ParseCommand.Replace;
                                break;

                            case ParseMode.Append:
                                s_parseCommand = ParseCommand.Append;
                                break;
                        }
                        break;
                    case UtilType.ParseModeCategory.UnorderedContainer:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{inputContext}: Invalid mode {s_parseMode} provided for an unordered-container-type parse, defaulting to Replace");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                            case ParseMode.Replace:
                                s_parseCommand = ParseCommand.Replace;
                                break;

                            case ParseMode.Patch:
                                s_parseCommand = ParseCommand.Patch;
                                break;

                            case ParseMode.Append:
                                s_parseCommand = ParseCommand.Append;
                                break;
                        }

                        break;
                    case UtilType.ParseModeCategory.Value:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{inputContext}: Invalid mode {s_parseMode} provided for a value-type parse, defaulting to Replace");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                            case ParseMode.Replace:
                                s_parseCommand = ParseCommand.Replace;
                                break;
                        }
                        break;
                    default:
                        Dbg.Err($"{inputContext}: Internal error, unknown mode category {modeCategory}, please report");
                        s_parseCommand = ParseCommand.Patch;  // . . . I guess?
                        break;
                }

                if (s_parseCommand == ParseCommand.Replace)
                {
                    orders.Clear();
                    // I'd love to just nuke `result` here, but for things like List<int> we want to preserve the existing object for ref reasons
                    // This is sort of a weird compromise so we can use the same codepath for both Dec and Recorder; Dec doesn't have refs, Recorder doesn't have parse modes, so practically speaking there's an easy choice for both of them
                    // it's just not the same easy choice
                    // but, whatever, we need this distinction anyway so we can do Append
                }

                orders.Add((s_parseCommand, payload));
            }

            return orders;
        }

        internal static object ParseElement(List<ReaderNode> nodes, Type type, object original, ReaderContext context, Recorder.Context recContext, FieldInfo fieldInfo = null, bool isRootDec = false, bool hasReferenceId = false, bool asThis = false)
        {
            if (nodes == null || nodes.Count == 0)
            {
                Dbg.Err("Internal error, Dec failed to provide nodes to ParseElement. Please report this!");
                return original;
            }

            if (context.recorderMode && nodes.Count > 1)
            {
                Dbg.Err("Internal error, multiple nodes provided for recorder-mode behavior. Please report this!");
            }

            // We keep the original around in case of error, but do all our manipulation on a result object.
            object result = original;

            // Verify our Shared flags as the *very* first step to ensure nothing gets past us.
            // In theory this should be fine with Flexible; Flexible only happens on an outer wrapper that was shared, and therefore was null, and therefore this is default also
            if (recContext.shared == Recorder.Context.Shared.Allow)
            {
                if (!type.CanBeShared())
                {
                    // If shared, make sure our input is null and our type is appropriate for sharing
                    Dbg.Wrn($"{nodes[0].GetInputContext()}: Value type `{type}` tagged as Shared in recorder, this is meaningless but harmless");
                }
                else if (original != null && !hasReferenceId)
                {
                    // We need to create objects without context if it's shared, so we kind of panic in this case
                    Dbg.Err($"{nodes[0].GetInputContext()}: Shared `{type}` provided with non-null default object, this may result in unexpected behavior");
                }
            }

            // The next thing we do is parse all our attributes. This is because we want to verify that there are no attributes being ignored.

            // Validate all combinations here
            // This could definitely be more efficient and skip at least one traversal pass
            foreach (var s_node in nodes)
            {
                string nullAttribute = s_node.GetMetadata(ReaderNode.Metadata.Null);
                string refAttribute = s_node.GetMetadata(ReaderNode.Metadata.Ref);
                string classAttribute = s_node.GetMetadata(ReaderNode.Metadata.Class);
                string modeAttribute = s_node.GetMetadata(ReaderNode.Metadata.Mode);

                // Some of these are redundant and that's OK
                if (nullAttribute != null && (refAttribute != null || classAttribute != null || modeAttribute != null))
                {
                    Dbg.Err($"{s_node.GetInputContext()}: Null element may not have ref, class, or mode specified; guessing wildly at intentions");
                }
                else if (refAttribute != null && (nullAttribute != null || classAttribute != null || modeAttribute != null))
                {
                    Dbg.Err($"{s_node.GetInputContext()}: Ref element may not have null, class, or mode specified; guessing wildly at intentions");
                }
                else if (classAttribute != null && (nullAttribute != null || refAttribute != null))
                {
                    Dbg.Err($"{s_node.GetInputContext()}: Class-specified element may not have null or ref specified; guessing wildly at intentions");
                }
                else if (modeAttribute != null && (nullAttribute != null || refAttribute != null))
                {
                    Dbg.Err($"{s_node.GetInputContext()}: Mode-specified element may not have null or ref specified; guessing wildly at intentions");
                }

                var unrecognized = s_node.GetMetadataUnrecognized();
                if (unrecognized != null)
                {
                    Dbg.Err($"{s_node.GetInputContext()}: Has unknown attributes {unrecognized}");
                }
            }

            // Doesn't mean anything outside recorderMode, so we check it for validity just in case
            string refKey;
            ReaderNode refKeyNode = null; // stored entirely for error reporting
            if (!context.recorderMode)
            {
                refKey = null;
                foreach (var s_node in nodes)
                {
                    string nodeRefAttribute = s_node.GetMetadata(ReaderNode.Metadata.Ref);
                    if (nodeRefAttribute != null)
                    {
                        Dbg.Err($"{s_node.GetInputContext()}: Found a reference tag while not evaluating Recorder mode, ignoring it");
                    }
                }
            }
            else
            {
                (refKey, refKeyNode) = nodes.Select(node => (node.GetMetadata(ReaderNode.Metadata.Ref), node)).Where(anp => anp.Item1 != null).LastOrDefault();
            }

            // First figure out type. We actually need type to be set before we can properly analyze and validate the mode flags.
            // If we're in an asThis block, it refers to the outer item, not the inner item; just skip this entirely
            bool isNull = false;
            if (!asThis)
            {
                string classAttribute = null;
                ReaderNode classAttributeNode = null; // stored entirely for error reporting
                bool replaced = false;
                foreach (var s_node in nodes)
                {
                    // However, we do need to watch for Replace, because that means we should nuke the class attribute and start over.
                    string modeAttribute = s_node.GetMetadata(ReaderNode.Metadata.Mode);
                    ParseMode s_parseMode = ParseModeFromString(s_node.GetInputContext(), modeAttribute);
                    if (s_parseMode == ParseMode.Replace)
                    {
                        // we also should maybe be doing this if we're a list, map, or set?
                        classAttribute = null;
                        replaced = true;
                    }

                    // if we get nulled, we kill the class tag and basically treat it like a delete
                    // but we also reset the null tag on every entry
                    isNull = false;
                    string nullAttribute = s_node.GetMetadata(ReaderNode.Metadata.Null);
                    if (nullAttribute != null)
                    {
                        if (!bool.TryParse(nullAttribute, out bool nullValue))
                        {
                            Dbg.Err($"{s_node.GetInputContext()}: Invalid `null` attribute");
                        }
                        else if (nullValue)
                        {
                            isNull = true;
                        }
                    }

                    // update the class based on whatever this says
                    string localClassAttribute = s_node.GetMetadata(ReaderNode.Metadata.Class);
                    if (localClassAttribute != null)
                    {
                        classAttribute = localClassAttribute;
                        classAttributeNode = s_node;
                    }
                }

                if (classAttribute != null)
                {
                    var possibleType = (Type)ParseString(classAttribute, typeof(Type), null, classAttributeNode.GetInputContext());
                    if (!type.IsAssignableFrom(possibleType))
                    {
                        Dbg.Err($"{classAttributeNode.GetInputContext()}: Explicit type {classAttribute} cannot be assigned to expected type {type}");
                    }
                    else if (!replaced && result != null && result.GetType() != possibleType)
                    {
                        Dbg.Err($"{classAttributeNode.GetInputContext()}: Explicit type {classAttribute} does not match already-provided instance {type}");
                    }
                    else
                    {
                        type = possibleType;
                    }
                }
            }

            var converter = Converters.TryGetValue(type);

            // Now we traverse the Mode attributes as prep for our final parse pass.
            UtilType.ParseModeCategory modeCategory = type.CalculateSerializationModeCategory(converter, isRootDec);
            List<(ParseCommand command, ReaderNode node)> orders = CompileOrders(modeCategory, nodes.Select(node => (node, node.GetInputContext(), ParseModeFromString(node.GetInputContext(), node.GetMetadata(ReaderNode.Metadata.Mode)))));

            // Gather info
            bool hasChildren = false;
            ReaderNode hasChildrenNode = null;
            bool hasText = false;
            ReaderNode hasTextNode = null;
            foreach (var (_, node) in orders)
            {
                if (!hasChildren && node.GetChildCount() > 0)
                {
                    hasChildren = true;
                    hasChildrenNode = node;
                }
                if (!hasText && node.GetText() != null)
                {
                    hasText = true;
                    hasTextNode = node;
                }
            }

            // Actually handle our attributes
            if (refKey != null)
            {
                // Ref is the highest priority, largely because I think it's cool

                if (recContext.shared == Recorder.Context.Shared.Deny)
                {
                    Dbg.Err($"{refKeyNode.GetInputContext()}: Found a reference in a non-.Shared() context, using it anyway but this might produce unexpected results");
                }

                if (context.refs == null)
                {
                    Dbg.Err($"{refKeyNode.GetInputContext()}: Found a reference object {refKey} before refs are initialized (is this being used in a ConverterFactory<>.Create()?)");
                    return result;
                }

                if (!context.refs.ContainsKey(refKey))
                {
                    Dbg.Err($"{refKeyNode.GetInputContext()}: Found a reference object {refKey} without a valid reference mapping");
                    return result;
                }

                object refObject = context.refs[refKey];
                if (!type.IsAssignableFrom(refObject.GetType()))
                {
                    Dbg.Err($"{refKeyNode.GetInputContext()}: Reference object {refKey} is of type {refObject.GetType()}, which cannot be converted to expected type {type}");
                    return result;
                }

                return refObject;
            }
            else if (isNull)
            {
                return null;

                // Note: It may seem wrong that we can return null along with a non-null model.
                // The problem is that this is meant to be able to override defaults. If the default is an object, explicitly setting it to null *should* clear the object out.
                // If we actually need a specific object to be returned, for whatever reason, the caller has to do the comparison.
            }

            // Basic early validation

            if (hasChildren && hasText)
            {
                Dbg.Err($"{hasChildrenNode.GetInputContext()} / {hasTextNode.GetInputContext()}: Cannot have both text and child nodes in XML - this is probably a typo, maybe you have the wrong number of close tags or added text somewhere you didn't mean to?");

                // we'll just fall through and try to parse anyway, though
            }

            if (typeof(Dec).IsAssignableFrom(type) && hasChildren && !isRootDec)
            {
                Dbg.Err($"{hasChildrenNode.GetInputContext()}: Defining members of an item of type {type}, derived from Dec.Dec, is not supported within an outer Dec. Either reference a {type} defined independently or remove {type}'s inheritance from Dec.");
                return null;
            }

            // Defer off to converters, whatever they feel like doing
            if (converter != null)
            {
                // string converter
                if (converter is ConverterString converterString)
                {
                    foreach (var (parseCommand, node) in orders)
                    {
                        switch (parseCommand)
                        {
                            case ParseCommand.Replace:
                                // easy, done
                                break;

                            default:
                                Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        if (hasChildren)
                        {
                            Dbg.Err($"{node.GetInputContext()}: String converter {converter.GetType()} called with child XML nodes, which will be ignored");
                        }

                        // We actually accept "no text" here, though, empty-string might be valid!

                        // context might be null; that's OK at the moment
                        try
                        {
                            result = converterString.ReadObj(node.GetText() ?? "", node.GetInputContext());
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(e);

                            result = GenerateResultFallback(result, type);
                        }
                    }
                }
                else if (converter is ConverterRecord converterRecord)
                {
                    foreach (var (parseCommand, node) in orders)
                    {
                        switch (parseCommand)
                        {
                            case ParseCommand.Patch:
                                // easy, done
                                break;

                            case ParseCommand.Replace:
                                result = null;
                                break;

                            default:
                                Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        if (result == null)
                        {
                            result = type.CreateInstanceSafe("converterrecord", node);
                        }

                        // context might be null; that's OK at the moment
                        if (result != null)
                        {
                            try
                            {
                                object returnedResult = converterRecord.RecordObj(result, new RecorderReader(node, context));

                                if (!type.IsValueType && result != returnedResult)
                                {
                                    Dbg.Err($"{node.GetInputContext()}: Converter {converterRecord.GetType()} changed object instance, this is disallowed");
                                }
                                else
                                {
                                    // for value types, this is fine
                                    result = returnedResult;
                                }
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(e);

                                // no fallback needed, we already have a result
                            }
                        }
                    }
                }
                else if (converter is ConverterFactory converterFactory)
                {
                    foreach (var (parseCommand, node) in orders)
                    {
                        switch (parseCommand)
                        {
                            case ParseCommand.Patch:
                                // easy, done
                                break;

                            case ParseCommand.Replace:
                                result = null;
                                break;

                            default:
                                Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        if (result == null)
                        {
                            result = converterFactory.CreateObj(new RecorderReader(node, context, disallowShared: true));
                        }

                        // context might be null; that's OK at the moment
                        if (result != null)
                        {
                            try
                            {
                                result = converterFactory.ReadObj(result, new RecorderReader(node, context));
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(e);

                                // no fallback needed, we already have a result
                            }
                        }
                    }
                }
                else
                {
                    Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                }

                return result;
            }

            // Special case: IRecordables
            if (typeof(IRecordable).IsAssignableFrom(type) && (context.recorderMode || type.GetMethod("Record").GetCustomAttribute<Bespoke.IgnoreRecordDuringParserAttribute>() == null))
            {
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Patch:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    IRecordable recordable = null;

                    if (result != null)
                    {
                        recordable = (IRecordable)result;
                    }
                    else if (recContext.factories == null)
                    {
                        recordable = (IRecordable)type.CreateInstanceSafe("recordable", node);
                    }
                    else
                    {
                        // Iterate back to the appropriate type.
                        Type targetType = type;
                        Func<Type, object> maker = null;
                        while (targetType != null)
                        {
                            if (recContext.factories.TryGetValue(targetType, out maker))
                            {
                                break;
                            }

                            targetType = targetType.BaseType;
                        }

                        if (maker == null)
                        {
                            recordable = (IRecordable)type.CreateInstanceSafe("recordable", node);
                        }
                        else
                        {
                            // want to propogate this throughout the factories list to save on time later
                            // we're actually doing the same BaseType thing again, starting from scratch
                            Type writeType = type;
                            while (writeType != targetType)
                            {
                                recContext.factories[writeType] = maker;
                                writeType = writeType.BaseType;
                            }

                            // oh right and I guess we should actually make the thing too
                            var obj = maker(type);

                            if (obj == null)
                            {
                                // fall back to default behavior
                                recordable = (IRecordable)type.CreateInstanceSafe("recordable", node);
                            }
                            else if (!type.IsAssignableFrom(obj.GetType()))
                            {
                                Dbg.Err($"Custom factory generated {obj.GetType()} when {type} was expected; falling back on a default object");
                                recordable = (IRecordable)type.CreateInstanceSafe("recordable", node);
                            }
                            else
                            {
                                // now that we've checked this is of the right type
                                recordable = (IRecordable)obj;
                            }
                        }
                    }

                    if (recordable != null)
                    {
                        recordable.Record(new RecorderReader(node, context));

                        // TODO: support indices if this is within the Dec system?
                    }

                    result = recordable;
                }

                return result;
            }

            // All our standard text-using options
            if (hasText ||
                (typeof(Dec).IsAssignableFrom(type) && !isRootDec) ||
                type == typeof(Type) ||
                type == typeof(string) ||
                type.IsPrimitive)
            {
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    if (hasChildren)
                    {
                        Dbg.Err($"{node.GetInputContext()}: Child nodes are not valid when parsing {type}");
                    }

                    result = ParseString(node.GetText(), type, result, node.GetInputContext());
                }

                return result;
            }

            // Nothing past this point even supports text, so let's just get angry and break stuff.
            if (hasText)
            {
                Dbg.Err($"{hasTextNode.GetInputContext()}: Text detected in a situation where it is invalid; will be ignored");
            }

            // Special case: Lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // If you have a default list, but specify it in XML, we assume this is a full override. Clear the original list to cut down on GC churn.
                            // TODO: Is some bozo going to store the same "constant" global list on init, then be surprised when we re-use the list instead of creating a new one? Detect this and yell about it I guess.
                            // If you are reading this because you're the bozo, [insert angry emoji here], but also feel free to be annoyed that I haven't fixed it yet despite realizing it's a problem. Ping me on Discord, I'll take care of it, sorry 'bout that.
                            if (result != null)
                            {
                                ((IList)result).Clear();
                            }
                            break;

                        case ParseCommand.Append:
                            // we're good
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    // List<> handling
                    Type referencedType = type.GetGenericArguments()[0];

                    var list = (IList)(result ?? Activator.CreateInstance(type));

                    node.ParseList(list, referencedType, context, recContext);

                    result = list;
                }

                return result;
            }

            // Special case: Arrays
            if (type.IsArray)
            {
                Type referencedType = type.GetElementType();

                foreach (var (parseCommand, node) in orders)
                {
                    Array array;
                    int startOffset = 0;

                    // This is a bit extra-complicated because we can't append stuff after the fact, we need to figure out what our length is when we create the object.
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            /// This is a full override, so we're going to create it here.
                            if (result != null && result.GetType() == type && ((Array)result).Length == node.GetChildCount())
                            {
                                // It is actually vitally important that we fall back on the model when possible, because the Recorder Ref system requires it.
                                array = (Array)result;
                            }
                            else
                            {
                                // Otherwise just make a new one, no harm done.
                                array = (Array)Activator.CreateInstance(type, new object[] { node.GetChildCount() });
                            }

                            break;

                        case ParseCommand.Append:
                            if (result == null)
                            {
                                goto case ParseCommand.Replace;
                            }

                            // This is jankier; we create it here with the intended final length, then copy the elements over, all because arrays can't be resized
                            // (yes, I know, that's the point of arrays, I'm not complaining, just . . . grumbling a little)
                            var oldArray = (Array)result;
                            startOffset = oldArray.Length;
                            array = (Array)Activator.CreateInstance(type, new object[] { startOffset + node.GetChildCount() });
                            oldArray.CopyTo(array, 0);

                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            array = null; // just to break the unassigned-local-variable
                            break;
                    }

                    node.ParseArray(array, referencedType, context, recContext, startOffset);

                    result = array;
                }

                return result;
            }

            // Special case: Dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                foreach (var (parseCommand, node) in orders)
                {
                    bool permitPatch = false;
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // If you have a default dict, but specify it in XML, we assume this is a full override. Clear the original list to cut down on GC churn.
                            // TODO: Is some bozo going to store the same "constant" global dict on init, then be surprised when we re-use the dict instead of creating a new one? Detect this and yell about it I guess.
                            // If you are reading this because you're the bozo, [insert angry emoji here], but also feel free to be annoyed that I haven't fixed it yet despite realizing it's a problem. Ping me on Discord, I'll take care of it, sorry 'bout that.
                            if (result != null)
                            {
                                ((IDictionary)result).Clear();
                            }
                            break;

                        case ParseCommand.Patch:
                            if (original != null)
                            {
                                permitPatch = true;
                            }
                            break;

                        case ParseCommand.Append:
                            // nothing needs to be done, our existing dupe checking will solve it
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    // Dictionary<> handling
                    Type keyType = type.GetGenericArguments()[0];
                    Type valueType = type.GetGenericArguments()[1];

                    var dict = (IDictionary)(result ?? Activator.CreateInstance(type));

                    node.ParseDictionary(dict, keyType, valueType, context, recContext, permitPatch);

                    result = dict;
                }

                return result;
            }

            // Special case: HashSet
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                // HashSet<> handling
                // This is a gigantic pain because HashSet<> doesn't inherit from any non-generic interface that provides the functionality we want
                // So we're stuck doing it all through object and reflection
                // Thanks, HashSet
                // This might be a performance problem and we'll . . . deal with it later I guess?
                // This might actually be a good first place to use IL generation.

                foreach (var (parseCommand, node) in orders)
                {
                    bool permitPatch = false;
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // If you have a default set, but specify it in XML, we assume this is a full override. Clear the original set to cut down on GC churn.
                            // TODO: Is some bozo going to store the same "constant" global set on init, then be surprised when we re-use the set instead of creating a new one? Detect this and yell about it I guess.
                            // If you are reading this because you're the bozo, [insert angry emoji here], but also feel free to be annoyed that I haven't fixed it yet despite realizing it's a problem. Ping me on Discord, I'll take care of it, sorry 'bout that.
                            if (result != null)
                            {
                                // Did you know there's no non-generic interface that HashSet<> supports that includes a Clear function?
                                // Fun fact:
                                // That thing I just wrote!
                                var clearFunction = result.GetType().GetMethod("Clear");
                                clearFunction.Invoke(result, null);
                            }
                            break;

                        case ParseCommand.Patch:
                            if (original != null)
                            {
                                permitPatch = true;
                            }
                            break;

                        case ParseCommand.Append:
                            // nothing needs to be done, our existing dupe checking will solve it
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    Type keyType = type.GetGenericArguments()[0];

                    var set = result ?? Activator.CreateInstance(type);

                    node.ParseHashset(set, keyType, context, recContext, permitPatch);

                    result = set;
                }

                return result;
            }

            // Special case: A bucket of tuples
            // These are all basically identical, but AFAIK there's no good way to test them all in a better way.
            if (type.IsGenericType && (
                    type.GetGenericTypeDefinition() == typeof(Tuple<>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,,>)
                    ))
            {
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    int expectedCount = type.GenericTypeArguments.Length;
                    object[] parameters = new object[expectedCount];

                    node.ParseTuple(parameters, type, fieldInfo?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>()?.TransformNames, context, recContext);

                    // construct!
                    result = Activator.CreateInstance(type, parameters);
                }

                return result;
            }

            // At this point, we're either a class or a struct, and we need to do the reflection thing

            // If we have refs, something has gone wrong; we should never be doing reflection inside a Record system.
            // This is a really ad-hoc way of testing this and should be fixed.
            // One big problem here is that I'm OK with security vulnerabilities in dec xmls. Those are either supplied by the developer or by mod authors who are intended to have full code support anyway.
            // I'm less OK with security vulnerabilities in save files. Nobody expects a savefile can compromise their system.
            // And the full reflection system is probably impossible to secure, whereas the Record system should be secureable.
            if (context.recorderMode)
            {
                // just pick the first node to get something to go on
                Dbg.Err($"{orders[0].node.GetInputContext()}: Falling back to reflection within a Record system while parsing a {type}; this is currently not allowed for security reasons. Either you shouldn't be trying to serialize this, or it should implement Dec.IRecorder (https://zorbathut.github.io/dec/release/documentation/serialization.html), or you need a Dec.Converter (https://zorbathut.github.io/dec/release/documentation/custom.html)");
                return result;
            }

            foreach (var (parseCommand, node) in orders)
            {
                if (!isRootDec)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Patch:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetInputContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }
                }
                else
                {
                    if (parseCommand != ParseCommand.Patch)
                    {
                        Dbg.Err($"{node.GetInputContext()}: Mode provided for root Dec; this is currently not supported in any form");
                    }
                }

                // If we haven't been given a template class from our parent, go ahead and init to defaults
                if (result == null)
                {
                    result = type.CreateInstanceSafe("object", node);

                    if (result == null)
                    {
                        // error already reported
                        return result;
                    }
                }

                node.ParseReflection(result, context, recContext);
            }

            // Set up our index fields; this has to happen last in case we're a struct
            Index.Register(ref result);

            return result;
        }

        internal static object ParseString(string text, Type type, object original, InputContext context)
        {
            // Special case: Converter override
            // This is redundant if we're being called from ParseElement, but we aren't always.
            if (Converters.TryGetValue(type, out Converter converter))
            {
                object result = original;

                try
                {
                    // string converter
                    if (converter is ConverterString converterString)
                    {
                        // context might be null; that's OK at the moment
                        result = converterString.ReadObj(text, context);
                    }
                    else if (converter is ConverterRecord converterRecord)
                    {
                        // string parsing really doesn't apply here, we can't get a full Recorder context out anymore
                        // in theory this could be done with RecordAsThis() but I'm just going to skip it for now
                        Dbg.Err($"{context}: Attempt to string-parse with a ConverterRecord, this is currently not supported, contact developers if you need this feature");
                    }
                    else if (converter is ConverterFactory converterFactory)
                    {
                        // string parsing really doesn't apply here, we can't get a full Recorder context out anymore
                        // in theory this could be done with RecordAsThis() but I'm just going to skip it for now
                        Dbg.Err($"{context}: Attempt to string-parse with a ConverterFactory, this is currently not supported, contact developers if you need this feature");
                    }
                    else
                    {
                        Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                    }
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }

                return result;
            }

            // Special case: decs
            if (typeof(Dec).IsAssignableFrom(type))
            {
                if (text == "" || text == null)
                {
                    // you reference nothing, you get the null (even if this isn't a specified type; null is null, after all)
                    return null;
                }
                else
                {
                    if (type.GetDecRootType() == null)
                    {
                        Dbg.Err($"{context}: Non-hierarchy decs cannot be used as references");
                        return null;
                    }

                    Dec result = Database.Get(type, text);
                    if (result == null)
                    {
                        if (Util.ValidateDecName(text, context))
                        {
                            Dbg.Err($"{context}: Couldn't find {type} named `{text}`");
                        }

                        // If we're an invalid name, we already spat out the error
                    }
                    return result;
                }
            }

            // Special case: types
            if (type == typeof(Type))
            {
                if (text == "")
                {
                    return null;
                }

                return UtilType.ParseDecFormatted(text, context);
            }

            // Various non-composite-type special-cases
            if (text != "")
            {
                // If we've got text, treat us as an object of appropriate type
                try
                {
                    return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(text);
                }
                catch (System.Exception e)  // I would normally not catch System.Exception, but TypeConverter is wrapping FormatException in an Exception for some reason
                {
                    Dbg.Err($"{context}: {e.ToString()}");
                    return original;
                }
            }
            else if (type == typeof(string))
            {
                // If we don't have text, and we're a string, return ""
                return "";
            }
            else
            {
                // If we don't have text, and we've fallen down to this point, that's an error (and return original value I guess)
                Dbg.Err($"{context}: Empty field provided for type {type}");
                return original;
            }
        }

        internal static Type TypeSystemRuntimeType = Type.GetType("System.RuntimeType");
        internal static void ComposeElement(WriterNode node, object value, Type fieldType, FieldInfo fieldInfo = null, bool isRootDec = false, bool asThis = false)
        {
            // Verify our Shared flags as the *very* first step to ensure nothing gets past us.
            // In theory this should be fine with Flexible; Flexible only happens on an outer wrapper that was shared, and therefore was null, and therefore this is default also
            if (node.RecorderContext.shared == Recorder.Context.Shared.Allow && !asThis)
            {
                // If this is an `asThis` parameter, then we may not be writing the field type it looks like, and we're just going to trust that they're doing something sensible.
                if (!fieldType.CanBeShared())
                {
                    // If shared, make sure our type is appropriate for sharing
                    Dbg.Wrn($"Value type `{fieldType}` tagged as Shared in recorder, this is meaningless but harmless");
                }
            }

            // Handle Dec types, if this isn't a root (otherwise we'd just reference ourselves and that's kind of pointless)
            if (!isRootDec && value is Dec)
            {
                // Dec types are special in a few ways.
                // First off, they don't include their type data, because we assume it's of a type provided by the structure.
                // Second, we represent null values as an empty string, not as a null tag.
                // (We'll accept the null tag if you insist, we just have a cleaner special case.)
                // Null tag stuff is done further down, in the null check.

                var rootType = value.GetType().GetDecRootType();
                if (!rootType.IsAssignableFrom(fieldType))
                {
                    // The user has a Dec.Dec or similar, and it has a Dec assigned to it.
                    // This is a bit weird and is something we're not happy with; this means we need to include the Dec type along with it.
                    // But we're OK with that, honestly. We just do that.
                    // If you're saving something like this you don't get to rename Dec classes later on, but, hey, deal with it.
                    // We do, however, tag it with the root type, not the derived type; this is the most general type that still lets us search things in the future.
                    node.TagClass(rootType);
                }
                
                node.WriteDec(value as Dec);

                return;
            }

            // Everything represents "null" with an explicit XML tag, so let's just do that
            // Maybe at some point we want to special-case this for the empty Dec link
            if (value == null)
            {
                if (typeof(Dec).IsAssignableFrom(fieldType))
                {
                    node.WriteDec(null);
                }
                else
                {
                    node.WriteExplicitNull();
                }

                return;
            }

            var valType = value.GetType();

            // This is our value's type, but we may need a little bit of tinkering to make it useful.
            // The current case I know of is System.RuntimeType, which appears if we call .GetType() on a Type.
            // I assume there is a complicated internal reason for this; good news, we can ignore it and just pretend it's a System.Type.
            // Bad news: it's actually really hard to detect this case because System.RuntimeType is private.
            // That's why we have the annoying `static` up above.
            if (valType == TypeSystemRuntimeType)
            {
                valType = typeof(Type);
            }

            // Do all our unreferencables first
            bool unreferenceableComplete = false;

            if (valType.IsPrimitive)
            {
                node.WritePrimitive(value);

                unreferenceableComplete = true;
            }
            else if (value is System.Enum)
            {
                node.WriteEnum(value);

                unreferenceableComplete = true;
            }
            else if (value is string)
            {
                node.WriteString(value as string);

                unreferenceableComplete = true;
            }
            else if (value is Type)
            {
                node.WriteType(value as Type);

                unreferenceableComplete = true;
            }

            // Check to see if we should make this into a ref (yes, even if we're not tagged as Shared)
            // Do this *before* we do the class tagging, otherwise we may add ref/class tags to a single node, which is invalid.
            // Note that it's important we don't write a reference if we had an unreferenceable; it's unnecessarily slow and some of our writer types don't support it.
            if (!valType.IsValueType && !unreferenceableComplete && !asThis)
            {
                if (node.WriteReference(value))
                {
                    // The ref system has set up the appropriate tagging, so we're done!
                    return;
                }

                // If we support references, then this object has not previously shown up in the reference system; keep going so we finish serializing it.
                // If we don't support references at all then obviously we *really* need to finish serializing it.
            }

            // If we have a type that isn't the expected type, tag it. We may need this even for unreferencable value types because everything fits in an `object`.
            if (valType != fieldType)
            {
                if (asThis)
                {
                    Dbg.Err($"RecordAsThis() call attempted to add a class tag, which is currently not allowed; AsThis() calls must not be polymorphic (ask the devs for chained class tags if this is a thing you need)");
                    // . . . I guess we just keep going?
                }
                else
                {
                    node.TagClass(valType);
                }
            }

            // Did we actually write our node type? Alright, we're done.
            if (unreferenceableComplete)
            {
                return;
            }

            // Now we have things that *could* be references, but aren't.

            if (valType.IsArray)
            {
                node.WriteArray(value as Array);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(List<>))
            {
                node.WriteList(value as IList);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                node.WriteDictionary(value as IDictionary);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                node.WriteHashSet(value as IEnumerable);

                return;
            }

            if (valType.IsGenericType && (
                    valType.GetGenericTypeDefinition() == typeof(Tuple<>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>)
                ))
            {
                node.WriteTuple(value, fieldInfo?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>());

                return;
            }

            if (valType.IsGenericType && (
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,>) ||
                    valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,,>)
                ))
            {
                node.WriteValueTuple(value, fieldInfo?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>());

                return;
            }

            if (value is IRecordable && (!node.AllowReflection || value.GetType().GetMethod("Record").GetCustomAttribute<Bespoke.IgnoreRecordDuringParserAttribute>() == null))
            {
                node.WriteRecord(value as IRecordable);

                return;
            }

            {
                // Look for a converter; that's the only way to handle this before we fall back to reflection
                var converter = Serialization.Converters.TryGetValue(valType);
                if (converter != null)
                {
                    node.WriteConvertible(converter, value);
                    return;
                }
            }

            if (!node.AllowReflection)
            {
                Dbg.Err($"Couldn't find a composition method for type {valType}; either you shouldn't be trying to serialize it, or it should implement Dec.IRecorder (https://zorbathut.github.io/dec/release/documentation/serialization.html), or you need a Dec.Converter (https://zorbathut.github.io/dec/release/documentation/custom.html)");
                return;
            }

            // We absolutely should not be doing reflection when in recorder mode; that way lies madness.
            
            foreach (var field in valType.GetSerializableFieldsFromHierarchy())
            {
                ComposeElement(node.CreateMember(field, node.RecorderContext), field.GetValue(value), field.FieldType, fieldInfo: field);
            }

            return;
        }

        internal static void Clear()
        {
            Converters = new Dictionary<Type, Converter>();
        }
    }
}
