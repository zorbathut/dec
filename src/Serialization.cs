using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;

namespace Dec
{
    /// <summary>
    /// Internal serialization utilities.
    /// </summary>
    internal static class Serialization
    {
        // These start null and are populated by Initialize(). Initialize() builds complete
        // dictionaries locally then swaps the references, so no thread ever sees a partially-populated dict.
        // ConverterObjects being non-null is the signal that initialization is complete.
        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, Converter> ConverterObjects = null;
        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, Type> ConverterGenericPrototypes = null;

        internal class ConverterNullableString<T> : ConverterString<T?> where T : struct
        {
            private ConverterString<T> child;

            public ConverterNullableString(ConverterString<T> child)
            {
                this.child = child;
            }

            public override string Write(T? input)
            {
                if (!input.HasValue)
                {
                    Dbg.Err("Internal error: ConverterNullableString.Write called with null value; this should never happen");
                    return "";
                }

                return child.Write(input.Value);
            }

            public override T? Read(string input, Context context)
            {
                // if we're null, we must have already handled this elsewhere
                return child.Read(input, context);
            }
        }

        internal class ConverterNullableRecord<T> : ConverterRecord<T?> where T : struct
        {
            private ConverterRecord<T> child;

            public ConverterNullableRecord(ConverterRecord<T> child)
            {
                this.child = child;
            }

            public override void Record(ref T? input, Recorder recorder)
            {
                if (recorder.Mode == Recorder.Direction.Write)
                {
                    if (!input.HasValue)
                    {
                        Dbg.Err("Internal error: ConverterNullableRecord called with null value in write mode; this should never happen");
                        return;
                    }

                    var value = input.Value;
                    child.Record(ref value, recorder);
                }
                else if (recorder.Mode == Recorder.Direction.Read)
                {
                    T value = default;
                    child.Record(ref value, recorder);
                    input = value;
                }
                else
                {
                    Dbg.Err("Internal error: ConverterNullableRecord called with unknown mode; this should never happen");
                }
            }
        }

        internal class ConverterNullableFactory<T> : ConverterFactory<T?> where T : struct
        {
            private ConverterFactory<T> child;

            public ConverterNullableFactory(ConverterFactory<T> child)
            {
                this.child = child;
            }

            public override T? Create(Recorder recorder)
            {
                return child.Create(recorder);
            }

            public override void Read(ref T? input, Recorder recorder)
            {
                if (!input.HasValue)
                {
                    Dbg.Err("Internal error: ConverterNullableFactory.Read called with null value; this should never happen");
                    return;
                }

                var value = input.Value;
                child.Read(ref value, recorder);
                input = value;
            }

            public override void Write(T? input, Recorder recorder)
            {
                if (!input.HasValue)
                {
                    Dbg.Err("Internal error: ConverterNullableFactory.Write called with null value; this should never happen");
                    return;
                }

                child.Write(input.Value, recorder);
            }
        }

        internal static Converter ConverterFor(Type inputType)
        {
            if (ConverterObjects == null)
            {
                Dbg.Err($"Attempting to look up converter for {inputType} before initialization");
                return null;
            }

            if (ConverterObjects.TryGetValue(inputType, out var converter))
            {
                return converter;
            }

            // check for Nullable
            if (inputType.IsConstructedGenericType && inputType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullableType = inputType.GenericTypeArguments[0];

                // handle all types of converters
                var originalConverter = ConverterFor(nullableType);
                if (originalConverter is ConverterString nullableStringConverter)
                {
                    var nullableConverterType = typeof(ConverterNullableString<>).MakeGenericType(nullableType);
                    var nullableConverter = (ConverterString)Activator.CreateInstance(nullableConverterType, new object[] { originalConverter });
                    ConverterObjects[inputType] = nullableConverter;
                    return nullableConverter;
                }
                else if (originalConverter is ConverterRecord nullableRecordConverter)
                {
                    var nullableConverterType = typeof(ConverterNullableRecord<>).MakeGenericType(nullableType);
                    var nullableConverter = (ConverterRecord)Activator.CreateInstance(nullableConverterType, new object[] { originalConverter });
                    ConverterObjects[inputType] = nullableConverter;
                    return nullableConverter;
                }
                else if (originalConverter is ConverterFactory nullableFactoryConverter)
                {
                    var nullableConverterType = typeof(ConverterNullableFactory<>).MakeGenericType(nullableType);
                    var nullableConverter = (ConverterFactory)Activator.CreateInstance(nullableConverterType, new object[] { originalConverter });
                    ConverterObjects[inputType] = nullableConverter;
                    return nullableConverter;
                }
                else if (originalConverter != null)
                {
                    Dbg.Err($"Found converter {originalConverter} which is not a string, record, or factory converter. This is not allowed.");
                }
            }

            if (inputType.IsConstructedGenericType)
            {
                var genericType = inputType.GetGenericTypeDefinition();
                if (ConverterGenericPrototypes.TryGetValue(genericType, out var converterType))
                {
                    // construct `prototype` with the same generic arguments that `type` has
                    var concreteConverterType = converterType.MakeGenericType(inputType.GenericTypeArguments);
                    converter = (Converter)concreteConverterType.CreateInstanceSafe("converter", null);

                    // yes, do this even if it's null
                    ConverterObjects[inputType] = converter;

                    return converter;
                }
            }

            var factoriedConverter = Config.ConverterFactory?.Invoke(inputType);
            ConverterObjects[inputType] = factoriedConverter;   // cache this so we don't generate a million of them; might be null if there's nothing!
            return factoriedConverter;
        }


        internal static void Initialize()
        {
            if (ConverterObjects != null)
            {
                return;
            }

            var converterObjects = new System.Collections.Concurrent.ConcurrentDictionary<Type, Converter>();
            var converterGenericPrototypes = new System.Collections.Concurrent.ConcurrentDictionary<Type, Type>();

            IEnumerable<Type> conversionTypes;
            if (Config.TestParameters == null)
            {
                conversionTypes = UtilReflection.GetAllUserTypes().Where(t => t.IsSubclassOf(typeof(Converter)));
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
                if (type.IsAbstract)
                {
                    Dbg.Err($"Found converter {type} which is abstract. This is not allowed.");
                    continue;
                }

                if (type.IsGenericType)
                {
                    var baseConverterType = type;
                    while (baseConverterType.BaseType != typeof(ConverterString) && baseConverterType.BaseType != typeof(ConverterRecord) && baseConverterType.BaseType != typeof(ConverterFactory))
                    {
                        baseConverterType = baseConverterType.BaseType;
                    }

                    // we are now, presumably, at ConverterString<T> or ConverterRecord<T> or ConverterFactory<T>
                    // this *really* needs more error checking
                    Type converterTarget = baseConverterType.GenericTypeArguments[0];

                    if (!converterTarget.IsGenericType)
                    {
                        Dbg.Err($"Found generic converter {type} which is not referring to a generic constructed type.");
                        continue;
                    }

                    converterTarget = converterTarget.GetGenericTypeDefinition();
                    if (converterGenericPrototypes.ContainsKey(converterTarget))
                    {
                        Dbg.Err($"Found multiple converters for {converterTarget}: {converterGenericPrototypes[converterTarget]} and {type}");
                    }

                    converterGenericPrototypes[converterTarget] = type;
                    continue;
                }

                var converter = (Converter)type.CreateInstanceSafe("converter", null);
                if (converter != null && (converter is ConverterString || converter is ConverterRecord || converter is ConverterFactory))
                {
                    Type convertedType = converter.GetConvertedTypeHint();
                    if (converterObjects.ContainsKey(convertedType))
                    {
                        Dbg.Err($"Found multiple converters for {convertedType}: {converterObjects[convertedType]} and {type}");
                    }

                    converterObjects[convertedType] = converter;
                    continue;
                }
            }

            // Set ConverterGenericPrototypes first; ConverterObjects being non-null is the signal that initialization is complete.
            ConverterGenericPrototypes = converterGenericPrototypes;
            ConverterObjects = converterObjects;
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
            Patch,
            Append,

            // Dec-only
            Create,
            CreateOrReplace,
            CreateOrPatch,
            CreateOrIgnore,
            Delete,
            ReplaceIfExists,
            PatchIfExists,
            DeleteIfExists,
        }
        internal static ParseMode ParseModeFromString(Context context, string str)
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
            else if (str == "create")
            {
                return ParseMode.Create;
            }
            else if (str == "createOrReplace")
            {
                return ParseMode.CreateOrReplace;
            }
            else if (str == "createOrPatch")
            {
                return ParseMode.CreateOrPatch;
            }
            else if (str == "createOrIgnore")
            {
                return ParseMode.CreateOrIgnore;
            }
            else if (str == "delete")
            {
                return ParseMode.Delete;
            }
            else if (str == "replaceIfExists")
            {
                return ParseMode.ReplaceIfExists;
            }
            else if (str == "patchIfExists")
            {
                return ParseMode.PatchIfExists;
            }
            else if (str == "deleteIfExists")
            {
                return ParseMode.DeleteIfExists;
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
        internal static List<(ParseCommand command, ReaderNodeParseable node)> CompileOrders(UtilType.ParseModeCategory modeCategory, List<ReaderNodeParseable> nodes)
        {
            var orders = new List<(ParseCommand command, ReaderNodeParseable payload)>();

            if (modeCategory == UtilType.ParseModeCategory.Dec)
            {
                Dbg.Err($"Internal error: CompileOrders called with Dec mode category, this should never happen! Please report it.");
                return orders;
            }

            foreach (var node in nodes)
            {
                var context = node.GetContext();
                var s_parseMode = ParseModeFromString(context, node.GetMetadata(ReaderNodeParseable.Metadata.Mode));

                ParseCommand s_parseCommand;

                switch (modeCategory)
                {
                    case UtilType.ParseModeCategory.Object:
                        switch (s_parseMode)
                        {
                            default:
                                Dbg.Err($"{context}: Invalid mode {s_parseMode} provided for an Object-type parse, defaulting to Patch");
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
                                Dbg.Err($"{context}: Invalid mode {s_parseMode} provided for an ordered-container-type parse, defaulting to Replace");
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
                                Dbg.Err($"{context}: Invalid mode {s_parseMode} provided for an unordered-container-type parse, defaulting to Replace");
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
                                Dbg.Err($"{context}: Invalid mode {s_parseMode} provided for a value-type parse, defaulting to Replace");
                                goto case ParseMode.Default;

                            case ParseMode.Default:
                            case ParseMode.Replace:
                                s_parseCommand = ParseCommand.Replace;
                                break;
                        }
                        break;
                    default:
                        Dbg.Err($"{context}: Internal error, unknown mode category {modeCategory}, please report");
                        s_parseCommand = ParseCommand.Patch;  // . . . I guess?
                        break;
                }

                if (s_parseCommand == ParseCommand.Replace)
                {
                    orders.Clear();
                }

                orders.Add((s_parseCommand, node));
            }

            return orders;
        }

        internal static List<ReaderFileDec.ReaderDec> CompileDecOrders(List<ReaderFileDec.ReaderDec> decs)
        {
            var orders = new List<ReaderFileDec.ReaderDec>();
            bool everExisted = false;
            foreach (var item in decs)
            {
                // this is also horribly inefficient
                var s_parseMode = ParseModeFromString(item.context, item.nodeFactory(null).GetMetadata(ReaderNodeParseable.Metadata.Mode));

                switch (s_parseMode)
                {
                    default:
                        Dbg.Err($"{item.context}: Invalid mode {s_parseMode} provided for a Dec-type parse, defaulting to Create");
                        goto case ParseMode.Default;

                    case ParseMode.Default:
                    case ParseMode.Create:
                        if (orders.Count != 0)
                        {
                            Dbg.Err($"{item.context}: Create mode used when a Dec already exists, falling back to Patch");
                            goto case ParseMode.Patch;
                        }
                        orders.Add(item);
                        everExisted = true;
                        break;

                    case ParseMode.Replace:
                        if (orders.Count == 0)
                        {
                            Dbg.Err($"{item.context}: Replace mode used when a Dec doesn't exist, falling back to Create");
                            goto case ParseMode.Create;
                        }
                        orders.Clear();
                        orders.Add(item);
                        break;

                    case ParseMode.Patch:
                        if (orders.Count == 0)
                        {
                            Dbg.Err($"{item.context}: Patch mode used when a Dec doesn't exist, falling back to Create");
                            goto case ParseMode.Create;
                        }
                        orders.Add(item);
                        break;

                    case ParseMode.CreateOrReplace:
                        // doesn't matter if we have a thing or not
                        orders.Clear();
                        orders.Add(item);
                        everExisted = true;
                        break;

                    case ParseMode.CreateOrPatch:
                        // doesn't matter if we have a thing or not
                        orders.Add(item);
                        everExisted = true;
                        break;

                    case ParseMode.CreateOrIgnore:
                        if (orders.Count == 0)
                        {
                            orders.Add(item);
                            everExisted = true;
                        }
                        break;

                    case ParseMode.Delete:
                        if (!everExisted)
                        {
                            Dbg.Err($"{item.context}: Delete mode used when a Dec doesn't exist; did you want deleteIfExists?");
                        }
                        orders.Clear();
                        break;

                    case ParseMode.ReplaceIfExists:
                        if (orders.Count != 0)
                        {
                            orders.Clear();
                            orders.Add(item);
                        }
                        break;

                    case ParseMode.PatchIfExists:
                        if (orders.Count != 0)
                        {
                            orders.Add(item);
                        }
                        break;

                    case ParseMode.DeleteIfExists:
                        orders.Clear();
                        break;
                }
            }

            return orders;
        }

        internal static object ParseElement(List<ReaderNodeParseable> nodes, Type type, object original,
            ReaderGlobals globals, Recorder.Settings recSettings, FieldInfo fieldInfo = null, bool isRootDec = false,
            bool hasReferenceId = false, bool asThis = false,
            List<(ParseCommand command, ReaderNodeParseable node)> ordersOverride = null)
        {
            var result = ParseElement_Worker(nodes, type, original, globals, recSettings, fieldInfo, isRootDec, hasReferenceId, asThis, ordersOverride);

            // I just really don't want to put this code at the end of *every single return*, that would be insane
            // we don't allow dec references, we've already got those!
            if (globals.writeDecPaths && result != null)
            {
                var resultType = result.GetType();

                // I really feel like whatever I'm expressing here must exist elsewhere in the codebase, but I can't find it
                if (!resultType.IsValueType && resultType != typeof(string) && resultType != typeof(Type) && resultType != TypeSystemRuntimeType && !typeof(Dec).IsAssignableFrom(resultType))
                {
                    // these paths *should* all match up, so we're just choosing one
                    Database.DecPathRegister(result, nodes[0].GetContext().path);
                }
            }

            return result;
        }

        internal static T ParseElementTyped<T>(List<ReaderNodeParseable> nodes, Type type, object original, ReaderGlobals globals, Recorder.Settings recSettings, FieldInfo fieldInfo = null, bool isRootDec = false, bool hasReferenceId = false, bool asThis = false, List<(ParseCommand command, ReaderNodeParseable node)> ordersOverride = null)
        {
            var result = ParseElement(nodes, type, original, globals, recSettings, fieldInfo, isRootDec, hasReferenceId, asThis, ordersOverride);
            if (result != null)
            {
                return (T)result;
            }
            else
            {
                return default;
            }
        }

        internal static object ParseElement_Worker(List<ReaderNodeParseable> nodes, Type type, object original, ReaderGlobals globals, Recorder.Settings recSettings, FieldInfo fieldInfo = null, bool isRootDec = false, bool hasReferenceId = false, bool asThis = false, List<(ParseCommand command, ReaderNodeParseable node)> ordersOverride = null)
        {
            if (nodes == null || nodes.Count == 0)
            {
                Dbg.Err("Internal error, Dec failed to provide nodes to ParseElement. Please report this!");
                return original;
            }

            if (!globals.allowReflection && nodes.Count > 1)
            {
                Dbg.Err("Internal error, multiple nodes provided for recorder-mode behavior. Please report this!");
            }

            // We keep the original around in case of error, but do all our manipulation on a result object.
            object result = original;

            // Verify our Shared flags as the *very* first step to ensure nothing gets past us.
            // In theory this should be fine with Flexible; Flexible only happens on an outer wrapper that was shared, and therefore was null, and therefore this is default also
            if (recSettings.shared == Recorder.Settings.Shared.Allow)
            {
                if (!type.CanBeShared())
                {
                    // If shared, make sure our input is null and our type is appropriate for sharing
                    Dbg.Wrn($"{nodes[0].GetContext()}: Value type `{type}` tagged as Shared in recorder, this is meaningless but harmless");
                }
                else if (original != null && !hasReferenceId)
                {
                    // We need to create objects without context if it's shared, so we kind of panic in this case
                    Dbg.Err($"{nodes[0].GetContext()}: Shared `{type}` provided with non-null default object, this may result in unexpected behavior");
                }
            }

            // The next thing we do is parse all our attributes. This is because we want to verify that there are no attributes being ignored.

            // Validate all combinations here
            // This could definitely be more efficient and skip at least one traversal pass
            foreach (var s_node in nodes)
            {
                string nullAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Null);
                string refAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Ref);
                string classAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Class);
                string modeAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Mode);

                // Some of these are redundant and that's OK
                if (nullAttribute != null && (refAttribute != null || classAttribute != null || modeAttribute != null))
                {
                    Dbg.Err($"{s_node.GetContext()}: Null element may not have ref, class, or mode specified; guessing wildly at intentions");
                }
                else if (refAttribute != null && (nullAttribute != null || classAttribute != null || modeAttribute != null))
                {
                    Dbg.Err($"{s_node.GetContext()}: Ref element may not have null, class, or mode specified; guessing wildly at intentions");
                }
                else if (classAttribute != null && (nullAttribute != null || refAttribute != null))
                {
                    Dbg.Err($"{s_node.GetContext()}: Class-specified element may not have null or ref specified; guessing wildly at intentions");
                }
                else if (modeAttribute != null && (nullAttribute != null || refAttribute != null))
                {
                    Dbg.Err($"{s_node.GetContext()}: Mode-specified element may not have null or ref specified; guessing wildly at intentions");
                }

                var unrecognized = s_node.GetMetadataUnrecognized();
                if (unrecognized != null)
                {
                    Dbg.Err($"{s_node.GetContext()}: Has unknown attributes {unrecognized}");
                }
            }

            // Doesn't mean anything outside recorderMode, so we check it for validity just in case
            string refKey;
            ReaderNode refKeyNode = null; // stored entirely for error reporting
            if (!globals.allowRefs)
            {
                refKey = null;
                foreach (var s_node in nodes)
                {
                    string nodeRefAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Ref);
                    if (nodeRefAttribute != null)
                    {
                        Dbg.Err($"{s_node.GetContext()}: Found a reference tag while not evaluating Recorder mode, ignoring it");
                    }
                }
            }
            else
            {
                (refKey, refKeyNode) = nodes.Select(node => (node.GetMetadata(ReaderNodeParseable.Metadata.Ref), node)).Where(anp => anp.Item1 != null).LastOrDefault();
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
                    string modeAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Mode);
                    ParseMode s_parseMode = ParseModeFromString(s_node.GetContext(), modeAttribute);
                    if (s_parseMode == ParseMode.Replace)
                    {
                        // we also should maybe be doing this if we're a list, map, or set?
                        classAttribute = null;
                        replaced = true;
                    }

                    // if we get nulled, we kill the class tag and basically treat it like a delete
                    // but we also reset the null tag on every entry
                    isNull = false;
                    string nullAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Null);
                    if (nullAttribute != null)
                    {
                        if (!bool.TryParse(nullAttribute, out bool nullValue))
                        {
                            Dbg.Err($"{s_node.GetContext()}: Invalid `null` attribute");
                        }
                        else if (nullValue)
                        {
                            isNull = true;
                        }
                    }

                    // update the class based on whatever this says
                    string localClassAttribute = s_node.GetMetadata(ReaderNodeParseable.Metadata.Class);
                    if (localClassAttribute != null)
                    {
                        classAttribute = localClassAttribute;
                        classAttributeNode = s_node;
                    }
                }

                if (classAttribute != null)
                {
                    var possibleType = (Type)ParseString(classAttribute, typeof(Type), null, classAttributeNode.GetContext());
                    if (!type.IsAssignableFrom(possibleType))
                    {
                        Dbg.Err($"{classAttributeNode.GetContext()}: Explicit type {classAttribute} cannot be assigned to expected type {type}");
                    }
                    else if (!replaced && result != null && result.GetType() != possibleType)
                    {
                        Dbg.Err($"{classAttributeNode.GetContext()}: Explicit type {classAttribute} does not match already-provided instance {type}");
                    }
                    else
                    {
                        type = possibleType;
                    }
                }
            }

            var converter = ConverterFor(type);

            // Now we traverse the Mode attributes as prep for our final parse pass.
            // ordersOverride makes `nodes` admittedly a little unnecessary.
            List<(ParseCommand command, ReaderNodeParseable node)> orders = ordersOverride ?? CompileOrders(type.CalculateSerializationModeCategory(converter, isRootDec), nodes);

            // Gather info
            bool hasChildren = false;
            ReaderNode hasChildrenNode = null;
            bool hasImportantText = false;
            ReaderNode hasTextNode = null;
            foreach (var (_, node) in orders)
            {
                if (!hasChildren && node.HasChildren())
                {
                    hasChildren = true;
                    hasChildrenNode = node;
                }

                // We trim this to get info on whether there's text that must not be ignored. If we turn out to be a string, we'll grudingly accept pure whitespace anyway.
                if (!hasImportantText && node.GetText() != null)
                {
                    if (!node.GetText().Trim().IsNullOrEmpty())
                    {
                        hasImportantText = true;
                        hasTextNode = node;
                    }
                }
            }

            // Actually handle our attributes
            if (refKey != null)
            {
                // Ref is the highest priority, largely because I think it's cool

                // First we check if this is a valid Dec path ref; those don't require .Shared()
                var decRef = Database.GetFromDecPath(refKey);
                if (decRef != null)
                {
                    // check types
                    if (!type.IsAssignableFrom(decRef.GetType()))
                    {
                        Dbg.Err($"{refKeyNode.GetContext()}: Dec path reference object [{refKey}] is of type {decRef.GetType()}, which cannot be converted to expected type {type}");
                        return result;
                    }

                    // toot
                    return decRef;
                }

                if (recSettings.shared == Recorder.Settings.Shared.Deny)
                {
                    Dbg.Err($"{refKeyNode.GetContext()}: Found a reference in a non-.Shared() context; this should happen only if you've removed the .Shared() tag since the file was generated, or if you hand-wrote a file that is questionably valid. Using the reference anyway but this might produce unexpected results");
                }

                if (globals.refs == null)
                {
                    Dbg.Err($"{refKeyNode.GetContext()}: Found a reference object {refKey} before refs are initialized (is this being used in a ConverterFactory<>.Create()?)");
                    return result;
                }

                if (!globals.refs.ContainsKey(refKey))
                {
                    Dbg.Err($"{refKeyNode.GetContext()}: Found a reference object {refKey} without a valid reference mapping");
                    return result;
                }

                object refObject = globals.refs[refKey];
                if (refObject == null && !type.IsValueType)
                {
                    // okay, good enough
                    return refObject;
                }

                if (!type.IsAssignableFrom(refObject.GetType()))
                {
                    Dbg.Err($"{refKeyNode.GetContext()}: Reference object {refKey} is of type {refObject.GetType()}, which cannot be converted to expected type {type}");
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

            if (hasChildren && hasImportantText)
            {
                Dbg.Err($"{hasChildrenNode.GetContext()} / {hasTextNode.GetContext()}: Cannot have both text and child nodes in XML - this is probably a typo, maybe you have the wrong number of close tags or added text somewhere you didn't mean to?");

                // we'll just fall through and try to parse anyway, though
            }

            if (typeof(Dec).IsAssignableFrom(type) && hasChildren && !isRootDec)
            {
                Dbg.Err($"{hasChildrenNode.GetContext()}: Defining members of an item of type {type}, derived from Dec.Dec, is not supported within an outer Dec. Either reference a {type} defined independently or remove {type}'s inheritance from Dec.");
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
                                Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        if (hasChildren)
                        {
                            Dbg.Err($"{node.GetContext()}: String converter {converter.GetType()} called with child XML nodes, which will be ignored");
                        }

                        // We actually accept "no text" here, though, empty-string might be valid!

                        // context might be null; that's OK at the moment
                        try
                        {
                            result = converterString.ReadObj(node.GetText() ?? "", node.GetContext());
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(new ConverterReadException(node.GetContext(), converter, e));

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
                                Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        bool isNullable = type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

                        if (result == null && !isNullable)
                        {
                            result = type.CreateInstanceSafe("converterrecord", node);
                        }

                        // context might be null; that's OK at the moment
                        if (result != null || isNullable)
                        {
                            var recorderReader = new RecorderReader(node, globals, trackUsage: true);
                            try
                            {
                                object returnedResult = converterRecord.RecordObj(result, recorderReader);

                                if (!type.IsValueType && result != returnedResult)
                                {
                                    Dbg.Err($"{node.GetContext()}: Converter {converterRecord.GetType()} changed object instance, this is disallowed");
                                }
                                else
                                {
                                    // for value types, this is fine
                                    result = returnedResult;
                                }

                                recorderReader.ReportUnusedFields();
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(new ConverterReadException(node.GetContext(), converter, e));

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
                                Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        var recorderReader = new RecorderReader(node, globals, disallowShared: true, trackUsage: true);
                        if (result == null)
                        {
                            try
                            {
                                result = converterFactory.CreateObj(recorderReader);
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(new ConverterReadException(node.GetContext(), converter, e));
                            }
                        }

                        // context might be null; that's OK at the moment
                        if (result != null)
                        {
                            recorderReader.AllowShared(globals);
                            try
                            {
                                result = converterFactory.ReadObj(result, recorderReader);
                                recorderReader.ReportUnusedFields();
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(new ConverterReadException(node.GetContext(), converter, e));

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

            // All our standard text-using options
            // Placed before IRecordable just in case we have a Dec that is IRecordable
            if ((typeof(Dec).IsAssignableFrom(type) && !isRootDec) ||
                    type == typeof(Type) ||
                    type == typeof(string) ||
                    type.IsPrimitive ||
                    (TypeDescriptor.GetConverter(type)?.CanConvertFrom(typeof(string)) ?? false)   // this is last because it's slow
                )
            {
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    if (hasChildren)
                    {
                        Dbg.Err($"{node.GetContext()}: Child nodes are not valid when parsing {type}");
                    }

                    result = ParseString(node.GetText(), type, result, node.GetContext());
                }

                return result;
            }

            // Special case: IRecordables
            IRecordable recordableBuffered = null;
            if (typeof(IRecordable).IsAssignableFrom(type))
            {
                // we're going to need to make one anyway so let's just go ahead and do that
                IRecordable recordable = null;

                if (result != null)
                {
                    recordable = (IRecordable)result;
                }
                else if (recSettings.factories == null)
                {
                    recordable = (IRecordable)type.CreateInstanceSafe("recordable", orders[0].node);
                }
                else
                {
                    recordable = recSettings.CreateRecordableFromFactory(type, "recordable", orders[0].node);
                }

                // we hold on to this so that, *if* we end up not using this object, we can optionally reuse it later for reflection
                // in an ideal world we wouldn't create it at all in the first place, but we need to create it to call IConditionalRecordable's function
                recordableBuffered = recordable;

                var conditionalRecordable = recordable as IConditionalRecordable;
                if (conditionalRecordable == null || conditionalRecordable.ShouldRecord(nodes[0].UserSettings))
                {
                    foreach (var (parseCommand, node) in orders)
                    {
                        switch (parseCommand)
                        {
                            case ParseCommand.Patch:
                                // easy, done
                                break;

                            default:
                                Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                                break;
                        }

                        if (recordable != null)
                        {
                            var recorderReader = new RecorderReader(node, globals, trackUsage: true);
                            try
                            {
                                recordable.Record(recorderReader);
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(e);
                            }
                            recorderReader.ReportUnusedFields();

                            // TODO: support indices if this is within the Dec system?
                        }
                    }

                    result = recordable;
                    return result;
                }

                // otherwise we just fall through
            }

            // Special case: byte[] arrays with base64 encoding
            if (type == typeof(byte[]) && hasImportantText && !hasChildren)
            {
                // This is a byte array encoded as base64 text
                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // easy, done
                            break;

                        default:
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    try
                    {
                        byte[] decodedArray = Convert.FromBase64String(node.GetText());

                        // Check if we can reuse the existing array
                        if (result != null && result.GetType() == type && ((byte[])result).Length == decodedArray.Length)
                        {
                            // Copy into existing array for reference preservation
                            Array.Copy(decodedArray, (byte[])result, decodedArray.Length);
                        }
                        else
                        {
                            // Use this as the new array
                            result = decodedArray;
                        }
                    }
                    catch (FormatException)
                    {
                        Dbg.Err($"{node.GetContext()}: Invalid base64 string for byte array");

                        if (result == null)
                        {
                            result = new byte[0];   // kind of an ugly fallback
                        }
                    }
                }

                return result;
            }

            // Nothing past this point even supports text, so let's just get angry and break stuff.
            if (hasImportantText)
            {
                Dbg.Err($"{hasTextNode.GetContext()}: Text detected in a situation where it is invalid; will be ignored");
                return result;
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
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    // List<> handling
                    Type referencedType = type.GetGenericArguments()[0];

                    var list = (IList)(result ?? Activator.CreateInstance(type));

                    node.ParseList(list, referencedType, globals, recSettings);

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
                    Array array = null;
                    int startOffset = 0;

                    // This is a bit extra-complicated because we can't append stuff after the fact, we need to figure out what our length is when we create the object.
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                        {
                            // This is a full override, so we're going to create it here.
                            // It is actually vitally important that we fall back on the model when possible, because the Recorder Ref system requires it.
                            bool match = result != null && result.GetType() == type;
                            var arrayDimensions = node.GetArrayDimensions(type.GetArrayRank());
                            if (match)
                            {
                                array = (Array)result;
                                if (array.Rank != type.GetArrayRank())
                                {
                                    match = false;
                                }
                                else
                                {
                                    for (int i = 0; i < array.Rank; i++)
                                    {
                                        if (array.GetLength(i) != arrayDimensions[i])
                                        {
                                            match = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!match)
                            {
                                // Otherwise just make a new one, no harm done.
                                array = UtilType.CreateDynamicArray(referencedType, arrayDimensions);
                            }

                            break;
                        }

                        case ParseCommand.Append:
                        {
                            if (result == null)
                            {
                                goto case ParseCommand.Replace;
                            }

                            // This is jankier; we create it here with the intended final length, then copy the elements over, all because arrays can't be resized
                            // (yes, I know, that's the point of arrays, I'm not complaining, just . . . grumbling a little)
                            var oldArray = (Array)result;
                            startOffset = oldArray.Length;
                            var arrayDimensions = node.GetArrayDimensions(type.GetArrayRank());
                            arrayDimensions[0] += startOffset;
                            array = UtilType.CreateDynamicArray(referencedType, arrayDimensions);
                            if (arrayDimensions.Length == 1)
                            {
                                oldArray.CopyTo(array, 0);
                            }
                            else
                            {
                                // oy
                                void CopyArray(Array source, Array destination, int[] indices, int rank = 0)
                                {
                                    if (rank < source.Rank)
                                    {
                                        for (int i = 0; i < source.GetLength(rank); i++)
                                        {
                                            indices[rank] = i;
                                            CopyArray(source, destination, indices, rank + 1);
                                        }
                                    }
                                    else
                                    {
                                        destination.SetValue(source.GetValue(indices), indices);
                                    }
                                }

                                {
                                    int[] indices = new int[arrayDimensions.Length];
                                    CopyArray(oldArray, array, indices, 0);
                                }
                            }

                            break;
                        }

                        default:
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            array = null; // just to break the unassigned-local-variable
                            break;
                    }

                    node.ParseArray(array, referencedType, globals, recSettings, startOffset);

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
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    // Dictionary<> handling
                    Type keyType = type.GetGenericArguments()[0];
                    Type valueType = type.GetGenericArguments()[1];

                    var dict = (IDictionary)(result ?? Activator.CreateInstance(type));

                    node.ParseDictionary(dict, keyType, valueType, globals, recSettings, permitPatch);

                    result = dict;
                }

                return result;
            }

            // Special case: HashSet
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
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
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    Type keyType = type.GetGenericArguments()[0];

                    var set = result ?? Activator.CreateInstance(type);

                    node.ParseHashset(set, keyType, globals, recSettings, permitPatch);

                    result = set;
                }

                return result;
            }

            // Special case: Stack
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                // Stack<> handling
                // Again, no sensible non-generic interface to use, so we're stuck with reflection

                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // If you have a default stack, but specify it in XML, we assume this is a full override. Clear the original stack to cut down on GC churn.
                            // TODO: Is some bozo going to store the same "constant" global stack on init, then be surprised when we re-use the stack instead of creating a new one? Detect this and yell about it I guess.
                            // If you are reading this because you're the bozo, [insert angry emoji here], but also feel free to be annoyed that I haven't fixed it yet despite realizing it's a problem. Ping me on Discord, I'll take care of it, sorry 'bout that.
                            if (result != null)
                            {
                                var clearFunction = result.GetType().GetMethod("Clear");
                                clearFunction.Invoke(result, null);
                            }
                            break;

                        case ParseCommand.Append:
                            break;

                        // There definitely starts being an argument for prepend.

                        default:
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    Type keyType = type.GetGenericArguments()[0];

                    var set = result ?? Activator.CreateInstance(type);

                    node.ParseStack(set, keyType, globals, recSettings);

                    result = set;
                }

                return result;
            }

            // Special case: Queue
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                // Queue<> handling
                // Again, no sensible non-generic interface to use, so we're stuck with reflection

                foreach (var (parseCommand, node) in orders)
                {
                    switch (parseCommand)
                    {
                        case ParseCommand.Replace:
                            // If you have a default queue, but specify it in XML, we assume this is a full override. Clear the original queue to cut down on GC churn.
                            // TODO: Is some bozo going to store the same "constant" global queue on init, then be surprised when we re-use the queue instead of creating a new one? Detect this and yell about it I guess.
                            // If you are reading this because you're the bozo, [insert angry emoji here], but also feel free to be annoyed that I haven't fixed it yet despite realizing it's a problem. Ping me on Discord, I'll take care of it, sorry 'bout that.
                            if (result != null)
                            {
                                var clearFunction = result.GetType().GetMethod("Clear");
                                clearFunction.Invoke(result, null);
                            }
                            break;

                        case ParseCommand.Append:
                            break;

                        // There definitely starts being an argument for prepend.

                        default:
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    Type keyType = type.GetGenericArguments()[0];

                    var set = result ?? Activator.CreateInstance(type);

                    node.ParseQueue(set, keyType, globals, recSettings);

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
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }

                    int expectedCount = type.GenericTypeArguments.Length;
                    object[] parameters = new object[expectedCount];

                    node.ParseTuple(parameters, type, fieldInfo?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>()?.TransformNames, globals, recSettings);

                    // construct!
                    result = Activator.CreateInstance(type, parameters);
                }

                return result;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Intercept and handle appropriately
                // We've already handled the `null` attribute, so we know it isn't null - just go ahead and deal with it by passing it up to a parent
                // I'm not sure if this is the right approach?
                result = ParseElement(nodes, type.GetGenericArguments()[0], original, globals, recSettings);
                return result;
            }

            // At this point, we're either a class or a struct, and we need to do the reflection thing

            // If we have refs, something has gone wrong; we should never be doing reflection inside a Record system.
            // This is a really ad-hoc way of testing this and should be fixed.
            // One big problem here is that I'm OK with security vulnerabilities in dec xmls. Those are either supplied by the developer or by mod authors who are intended to have full code support anyway.
            // I'm less OK with security vulnerabilities in save files. Nobody expects a savefile can compromise their system.
            // And the full reflection system is probably impossible to secure, whereas the Record system should be secureable.
            if (!globals.allowReflection)
            {
                // just pick the first node to get something to go on
                Dbg.Err($"{orders[0].node.GetContext()}: Falling back to reflection within a Record system while parsing a {type}; this is currently not allowed for security reasons. Either you shouldn't be trying to serialize this, or it should implement Dec.IRecorder (https://zorbathut.github.io/dec/release/documentation/serialization.html), or you need a Dec.Converter (https://zorbathut.github.io/dec/release/documentation/custom.html)");
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
                            Dbg.Err($"{node.GetContext()}: Internal error, got invalid mode {parseCommand}");
                            break;
                    }
                }
                else
                {
                    if (parseCommand != ParseCommand.Patch)
                    {
                        Dbg.Err($"{node.GetContext()}: Mode provided for root Dec; this is currently not supported in any form");
                    }
                }

                // If we haven't been given a generic class from our parent, go ahead and init to defaults
                if (result == null && recordableBuffered != null)
                {
                    result = recordableBuffered;
                }

                if (result == null)
                {
                    // okay fine
                    result = type.CreateInstanceSafe("object", node);

                    if (result == null)
                    {
                        // error already reported
                        return result;
                    }
                }

                node.ParseReflection(result, globals, recSettings);
            }

            // Set up our index fields; this has to happen last in case we're a struct
            Index.Register(ref result);

            return result;
        }

        internal static object ParseString(string text, Type type, object original, Context context)
        {
            // Special case: Converter override
            // This is redundant if we're being called from ParseElement, but we aren't always.
            if (ConverterFor(type) is Converter converter)
            {
                object result = original;

                try
                {
                    // string converter
                    if (converter is ConverterString converterString)
                    {
                        // context might be null; that's OK at the moment
                        try
                        {
                            result = converterString.ReadObj(text, context);
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(new ConverterReadException(context, converter, e));

                            result = GenerateResultFallback(result, type);
                        }
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
                        if (UtilMisc.ValidateDecName(text, context))
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
                    if (type == typeof(float))
                    {
                        // first check the various strings, case-insensitive
                        if (String.Compare(text, "nan", true) == 0)
                        {
                            return float.NaN;
                        }

                        if (String.Compare(text, "infinity", true) == 0)
                        {
                            return float.PositiveInfinity;
                        }

                        if (String.Compare(text, "-infinity", true) == 0)
                        {
                            return float.NegativeInfinity;
                        }

                        if (text.StartsWith("nanbox", StringComparison.CurrentCultureIgnoreCase))
                        {
                            const int expectedFloatSize = 6 + 8;

                            if (type == typeof(float) && text.Length != expectedFloatSize)
                            {
                                Dbg.Err($"{context}: Found nanboxed value without the expected number of characters, expected {expectedFloatSize} but got {text.Length}");
                                return float.NaN;
                            }

                            int number = Convert.ToInt32(text.Substring(6), 16);
                            return BitConverter.Int32BitsToSingle(number);
                        }
                    }

                    if (type == typeof(double))
                    {
                        // first check the various strings, case-insensitive
                        if (String.Compare(text, "nan", true) == 0)
                        {
                            return double.NaN;
                        }

                        if (String.Compare(text, "infinity", true) == 0)
                        {
                            return double.PositiveInfinity;
                        }

                        if (String.Compare(text, "-infinity", true) == 0)
                        {
                            return double.NegativeInfinity;
                        }

                        if (text.StartsWith("nanbox", StringComparison.CurrentCultureIgnoreCase))
                        {
                            const int expectedDoubleSize = 6 + 16;

                            if (type == typeof(double) && text.Length != expectedDoubleSize)
                            {
                                Dbg.Err($"{context}: Found nanboxed value without the expected number of characters, expected {expectedDoubleSize} but got {text.Length}");
                                return double.NaN;
                            }

                            long number = Convert.ToInt64(text.Substring(6), 16);
                            return BitConverter.Int64BitsToDouble(number);
                        }
                    }

                    return TypeDescriptor.GetConverter(type).ConvertFromString(text);
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

        private class ComposeStrategy
        {
            internal Action<WriterNode, object, FieldInfo> writer;
            internal bool unreferenceable;
            internal bool canBeShared;
            internal bool canBeCloneCopied;
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<Type, ComposeStrategy> ComposeStrategyCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, ComposeStrategy>();

        private static ComposeStrategy BuildComposeStrategy(Type valType)
        {
            var strategy = new ComposeStrategy();
            strategy.canBeShared = Util.CanBeShared(valType);
            strategy.canBeCloneCopied = UtilType.CanBeCloneCopied(valType);

            // Check unreferenceable types first — these are written before reference tracking
            if (valType.IsPrimitive)
            {
                strategy.writer = (node, value, fi) => node.WritePrimitive(value);
                strategy.unreferenceable = true;
                return strategy;
            }

            if (typeof(System.Enum).IsAssignableFrom(valType))
            {
                strategy.writer = (node, value, fi) => node.WriteEnum(value);
                strategy.unreferenceable = true;
                return strategy;
            }

            if (valType == typeof(string))
            {
                strategy.writer = (node, value, fi) => node.WriteString(value as string);
                strategy.unreferenceable = true;
                return strategy;
            }

            if (valType == typeof(Type))
            {
                strategy.writer = (node, value, fi) => node.WriteType(value as Type);
                strategy.unreferenceable = true;
                return strategy;
            }

            // Referenceable types
            if (valType == typeof(byte[]))
            {
                strategy.writer = (node, value, fi) => node.WriteByteArray(value as byte[]);
                return strategy;
            }

            if (valType.IsArray)
            {
                strategy.writer = (node, value, fi) => node.WriteArray(value as Array);
                return strategy;
            }

            if (valType.IsGenericType)
            {
                var genericDef = valType.GetGenericTypeDefinition();

                if (genericDef == typeof(List<>)) { strategy.writer = (node, value, fi) => node.WriteList(value as IList); return strategy; }
                if (genericDef == typeof(Dictionary<,>)) { strategy.writer = (node, value, fi) => node.WriteDictionary(value as IDictionary); return strategy; }
                if (genericDef == typeof(HashSet<>)) { strategy.writer = (node, value, fi) => node.WriteHashSet(value as IEnumerable); return strategy; }
                if (genericDef == typeof(Queue<>)) { strategy.writer = (node, value, fi) => node.WriteQueue(value as IEnumerable); return strategy; }
                if (genericDef == typeof(Stack<>)) { strategy.writer = (node, value, fi) => node.WriteStack(value as IEnumerable); return strategy; }

                if (genericDef == typeof(Tuple<>) ||
                    genericDef == typeof(Tuple<,>) ||
                    genericDef == typeof(Tuple<,,>) ||
                    genericDef == typeof(Tuple<,,,>) ||
                    genericDef == typeof(Tuple<,,,,>) ||
                    genericDef == typeof(Tuple<,,,,,>) ||
                    genericDef == typeof(Tuple<,,,,,,>) ||
                    genericDef == typeof(Tuple<,,,,,,,>))
                {
                    strategy.writer = (node, value, fi) => node.WriteTuple(value, fi?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>());
                    return strategy;
                }

                if (genericDef == typeof(ValueTuple<>) ||
                    genericDef == typeof(ValueTuple<,>) ||
                    genericDef == typeof(ValueTuple<,,>) ||
                    genericDef == typeof(ValueTuple<,,,>) ||
                    genericDef == typeof(ValueTuple<,,,,>) ||
                    genericDef == typeof(ValueTuple<,,,,,>) ||
                    genericDef == typeof(ValueTuple<,,,,,,>) ||
                    genericDef == typeof(ValueTuple<,,,,,,,>))
                {
                    strategy.writer = (node, value, fi) => node.WriteValueTuple(value, fi?.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>());
                    return strategy;
                }
            }

            // Build the converter-or-reflection fallthrough writer; used standalone for non-IRecordable types,
            // or captured by the IRecordable closure for IConditionalRecordable fallthrough.
            Action<WriterNode, object, FieldInfo> fallthrough;
            {
                var converter = ConverterFor(valType);
                if (converter != null)
                {
                    fallthrough = (node, value, fi) =>
                    {
                        // Check if this type can be reconstructed with its converter
                        if (!valType.CanBeConstructed())
                        {
                            Dbg.Wrn($"{node.Path}: Serializing type {valType} with converter {converter.GetType().Name} but the type cannot be constructed (missing no-argument constructor for ConverterRecord, or no ConverterString/ConverterFactory). This object will fail to deserialize.");
                        }

                        node.WriteConvertible(converter, value);
                    };
                }
                else
                {
                    // Reflection fallback (or error if node doesn't allow reflection)
                    // We absolutely should not be doing reflection when in recorder mode; that way lies madness.
                    fallthrough = (node, value, fi) =>
                    {
                        if (!node.AllowReflection)
                        {
                            Dbg.Err($"Couldn't find a composition method for type {valType}; either you shouldn't be trying to serialize it, or it should implement Dec.IRecorder (https://zorbathut.github.io/dec/release/documentation/serialization.html), or you need a Dec.Converter (https://zorbathut.github.io/dec/release/documentation/custom.html)");
                            node.WriteError();
                            return;
                        }

                        foreach (var field in valType.GetSerializableFieldsFromHierarchy())
                        {
                            ComposeElement(node.CreateReflectionChild(field, node.RecorderSettings), field.GetValue(value), field.FieldType, fieldInfo: field);
                        }
                    };
                }
            }

            // IRecordable check
            if (typeof(IRecordable).IsAssignableFrom(valType))
            {
                bool isConditional = typeof(IConditionalRecordable).IsAssignableFrom(valType);

                strategy.writer = (node, value, fi) =>
                {
                    if (!isConditional || (value as IConditionalRecordable).ShouldRecord(node.UserSettings))
                    {
                        // Check if this type can be reconstructed
                        if (!valType.CanBeConstructed())
                        {
                            Dbg.Wrn($"{node.Path}: Serializing type {valType} which implements IRecordable but cannot be constructed (missing no-argument constructor). This object will fail to deserialize!");
                        }

                        node.WriteRecord(value as IRecordable);
                        return;
                    }

                    // IConditionalRecordable.ShouldRecord() returned false; fall through to converter or reflection
                    fallthrough(node, value, fi);
                };
                return strategy;
            }

            // Not IRecordable; use the fallthrough writer directly
            strategy.writer = fallthrough;
            return strategy;
        }

        internal static void ComposeElement(WriterNode node, object value, Type fieldType, FieldInfo fieldInfo = null, bool isRootDec = false, bool asThis = false)
        {
            // Verify our Shared flags as the *very* first step to ensure nothing gets past us.
            // In theory this should be fine with Flexible; Flexible only happens on an outer wrapper that was shared, and therefore was null, and therefore this is default also
            bool canBeShared = fieldType.CanBeShared();
            if (node.RecorderSettings.shared == Recorder.Settings.Shared.Allow && !asThis)
            {
                // If this is an `asThis` parameter, then we may not be writing the field type it looks like, and we're just going to trust that they're doing something sensible.
                if (!canBeShared)
                {
                    // If shared, make sure our type is appropriate for sharing
                    // this really needs the recorder name and the field name too
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

            if (Database.IsForbidden(value))
            {
                Dbg.Err($"Attempting to record {value} which has been explicitly forbidden from recording");
                node.WriteExplicitNull();
                return;
            }

            if (node.AllowDecPath)
            {
                // Try to snag a Dec path
                var decPath = Database.GetDecPathFromObj(value);

                if (decPath != null)
                {
                    node.WriteDecPathRef(value);
                    return;
                }
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

            // Look up the cached strategy for this type
            if (!ComposeStrategyCache.TryGetValue(valType, out var strategy))
            {
                strategy = BuildComposeStrategy(valType);
                ComposeStrategyCache[valType] = strategy;
            }

            // Unreferenceable types (primitives, enums, strings, Types) are written before reference tracking
            if (strategy.unreferenceable)
            {
                strategy.writer(node, value, fieldInfo);

                // If we have a type that isn't the expected type, tag it. We may need this even for unreferencable value types because everything fits in an `object`.
                ComposeElement_TagClass(node, valType, fieldType, asThis);

                return;
            }

            // Check to see if we should make this into a ref (yes, even if we're not tagged as Shared)
            // Do this *before* we do the class tagging, otherwise we may add ref/class tags to a single node, which is invalid.
            if (strategy.canBeShared && !asThis)
            {
                if (node.WriteReference(value, node.Path))
                {
                    // The ref system has set up the appropriate tagging, so we're done!
                    return;
                }

                // If we support references, then this object has not previously shown up in the reference system; keep going so we finish serializing it.
                // If we don't support references at all then obviously we *really* need to finish serializing it.
            }

            // If we have a type that isn't the expected type, tag it.
            ComposeElement_TagClass(node, valType, fieldType, asThis);

            // Now we have things that *could* be references, but aren't.

            if (node.AllowCloning && strategy.canBeCloneCopied)
            {
                node.WriteCloneCopy(value);

                return;
            }

            strategy.writer(node, value, fieldInfo);
        }

        private static void ComposeElement_TagClass(WriterNode node, Type valType, Type fieldType, bool asThis)
        {
            bool tagClass = valType != fieldType;
            if (fieldType.IsConstructedGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // If we're a Nullable<> then we know the type, so we unwrap it a layer
                tagClass = valType != fieldType.GetGenericArguments()[0];
            }

            if (tagClass)
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
        }

        internal static void Clear()
        {
            ConverterObjects = null;
            ConverterGenericPrototypes = null;
            ComposeStrategyCache.Clear();
            WriterNodeClone.ResolveStrategyCache.Clear();
        }
    }
}
