using System;
using System.Collections.Generic;

namespace Dec
{
    public partial class Recorder
    {
        /// <summary>
        /// Returns a fully-formed XML document starting at an object. Supports non-tree layouts with shared objects, including loops and self-referencing objects.
        /// </summary>
        public static string Write<T>(T target, bool pretty = false, IUserSettings userSettings = null)
        {
            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterXmlRecord(userSettings);

                Serialization.ComposeElement(writerContext.StartData(typeof(T)), target, typeof(T));

                return writerContext.Finish(pretty);
            }
        }

        /// <summary>
        /// Returns a fully-formed XML document starting at an object. Supports tree layouts only; no shared objects, no loops, no self-referencing objects.
        /// </summary>
        public static string WriteSimple<T>(T target, string rootTag, IUserSettings userSettings = null)
        {
            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterXmlSimple(rootTag, userSettings);

                Serialization.ComposeElement(writerContext.StartData(typeof(T)), target, typeof(T));

                // right now I'm just assuming "always pretty"
                return writerContext.Finish(true);
            }
        }

        /// <summary>
        /// Returns C# validation code starting at an option.
        /// </summary>
        public static string WriteValidation<T>(T target, Recorder.IUserSettings userSettings = null)
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterValidationRecord(userSettings);

                Serialization.ComposeElement(writerContext.StartData(), target, typeof(T));

                return writerContext.Finish();
            }
        }

        /// <summary>
        /// Parses the output of Write, generating an object and all its related serialized data.
        /// </summary>
        public static T Read<T>(string input, string stringName = "input", IUserSettings userSettings = null)
        {
            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                ReaderFileRecorder reader = ReaderFileRecorderXml.Create(input, stringName, userSettings);
                if (reader == null)
                {
                    return default;
                }

                var refs = reader.ParseRefs();

                // First, we need to make the instances for all the references, so they can be crosslinked appropriately
                // We'll be doing a second parse to parse *many* of these, but not all
                var furtherParsing = new List<Action>();
                var refDict = new Dictionary<string, object>();
                var readerContext = new ReaderContext() { allowReflection = false, allowRefs = true };

                foreach (var reference in refs)
                {
                    object refInstance = null;
                    if (Serialization.ConverterFor(reference.type) is Converter converter)
                    {
                        if (converter is ConverterString converterString)
                        {
                            try
                            {
                                refInstance = converterString.ReadObj(reference.node.GetText(), reference.node.GetInputContext());
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(new ConverterReadException(reference.node.GetInputContext(), converter, e));

                                refInstance = Serialization.GenerateResultFallback(refInstance, reference.type);
                            }

                            // this does not need to be queued for parsing
                        }
                        else if (converter is ConverterRecord converterRecord)
                        {
                            // create the basic object
                            refInstance = reference.type.CreateInstanceSafe("object", reference.node);

                            // the next parse step
                            furtherParsing.Add(() =>
                            {
                                var recorderReader = new RecorderReader(reference.node, readerContext, trackUsage: true);
                                try
                                {
                                    converterRecord.RecordObj(refInstance, recorderReader);
                                    recorderReader.ReportUnusedFields();
                                }
                                catch (Exception e)
                                {
                                    Dbg.Ex(new ConverterReadException(reference.node.GetInputContext(), converter, e));
                                }
                            });
                        }
                        else if (converter is ConverterFactory converterFactory)
                        {
                            // create the basic object
                            try
                            {
                                var recorderReader = new RecorderReader(reference.node, readerContext, disallowShared: true, trackUsage: true);
                                refInstance = converterFactory.CreateObj(recorderReader);

                                // the next parse step, if we have one
                                furtherParsing.Add(() =>
                                {
                                    recorderReader.AllowShared(readerContext);
                                    try
                                    {
                                        converterFactory.ReadObj(refInstance, recorderReader);
                                        recorderReader.ReportUnusedFields();
                                    }
                                    catch (Exception e)
                                    {
                                        Dbg.Ex(new ConverterReadException(reference.node.GetInputContext(), converter, e));
                                    }
                                });
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(new ConverterReadException(reference.node.GetInputContext(), converter, e));
                            }
                        }
                        else
                        {
                            Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                        }
                    }
                    else
                    {
                        // Create a stub so other things can reference it later
                        refInstance = reference.type.CreateInstanceSafe("object", reference.node);

                        // Whoops, failed to construct somehow. CreateInstanceSafe() has already made a report
                        if (refInstance != null)
                        {
                            furtherParsing.Add(() =>
                            {
                                // Do our actual parsing
                                // We know this *was* shared or it wouldn't be a ref now, so we tag it again in case it's a List<SomeClass> so we can share its children as well.
                                var refInstanceOutput = Serialization.ParseElement(new List<ReaderNodeParseable>() { reference.node }, refInstance.GetType(), refInstance, readerContext, new Recorder.Context() { shared = Context.Shared.Allow }, hasReferenceId: true);

                                if (refInstance != refInstanceOutput)
                                {
                                    Dbg.Err($"{reference.node.GetInputContext()}: Internal error, got the wrong object back from ParseElement. Things are probably irrevocably broken. Please report this as a bug in Dec.");
                                }
                            });
                        }
                    }

                    refDict[reference.id] = refInstance;
                }

                // link up the ref dict; we do this afterwards so we can verify that the object creation code is not using refs
                // note: I sort of feel like this shouldn't work, because we're stashing readerContext in some inline functions that we're adding to a list
                // it's important that they be updated by this line, so they can link up the refs properly
                // but this is a struct; does it not get copied? how does this work? is it boxed, somehow? is it actually referring to the stack?
                // if I exit this context while leaving those functions around, then change readerContext within one of them, does that change propogate?
                // if not, then when does this behavior switch?
                // if so, then are we filling up the heap with stuff?
                // gotta look into this someday
                // anyway the good news is that the test suite is testing this pretty extensively, so at least it works right now, even if it's not fast
                readerContext.refs = refDict;

                // finish up our second-stage ref parsing
                foreach (var action in furtherParsing)
                {
                    action();
                }

                var parseNode = reader.ParseNode();
                if (parseNode == null)
                {
                    // error has already been reported
                    return default;
                }

                // And now, we can finally parse our actual root element!
                // (which accounts for a tiny percentage of things that need to be parsed)
                return (T)Serialization.ParseElement(new List<ReaderNodeParseable>() { parseNode }, typeof(T), null, readerContext, new Recorder.Context() { shared = Context.Shared.Flexible });
            }
        }

        /// <summary>
        /// Parses the output of WriteSimple, generating an object and all its related serialized data.
        /// </summary>
        public static T ReadSimple<T>(string input, string rootTag, string stringName = "input", IUserSettings userSettings = null)
        {
            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var reader = ReaderFileXmlSimple.Create(input, rootTag, stringName, userSettings);
                if (reader == null)
                {
                    return default;
                }

                var readerContext = new ReaderContext() { allowReflection = false, allowRefs = false };

                // And now, we can finally parse our actual root element!
                // (which accounts for a tiny percentage of things that need to be parsed)
                return (T)Serialization.ParseElement(new List<ReaderNodeParseable>() { reader }, typeof(T), null, readerContext, new Recorder.Context() { shared = Context.Shared.Flexible });
            }
        }

        /// <summary>
        /// Makes a copy of an object.
        /// </summary>
        /// <remarks>
        /// This is logically equivalent to Write(Read(obj)), but much faster (approx 200x in one real-world testcase.)
        ///
        /// Clone() is guaranteed to accept everything that Write(Read(obj)) does, but the reverse is not true; Clone is sometimes more permissive than the disk serialization system. Don't expect to use this as complete validation for Write.
        ///
        /// This is a new feature and should be considered experimental. It is currently undertested, and I cannot stress this enough, Dec is a library full of weird edge cases and extremely carefully chosen behaviors, I *guarantee* many of those are handled wrong with Clone. If performance is not critical I currently strongly recommend using Write(Read(obj)).
        /// </remarks>
        public static T Clone<T>(T obj, IUserSettings userSettings = null)
        {
            Serialization.Initialize();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterClone(userSettings);

                var writerNode = writerContext.StartData(typeof(T));

                Serialization.ComposeElement(writerNode, obj, typeof(T));
                var output = writerNode.GetResult(false);

                writerContext.FinalizePendingWrites();

                return (T)output;
            }
        }
    }
}
