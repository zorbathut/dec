using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Dec;

namespace DecBenchmark
{
    // ───────────────────────────────────────────────
    // Data types
    // ───────────────────────────────────────────────

    public enum SimpleEnum
    {
        Alpha,
        Beta,
        Gamma,
        Delta,
        Epsilon,
    }

    public class PrimitivesRecordable : IRecordable
    {
        public int intValue;
        public float floatValue;
        public double doubleValue;
        public bool boolValue;
        public string stringValue;
        public SimpleEnum enumValue;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref intValue, "intValue");
            recorder.Record(ref floatValue, "floatValue");
            recorder.Record(ref doubleValue, "doubleValue");
            recorder.Record(ref boolValue, "boolValue");
            recorder.Record(ref stringValue, "stringValue");
            recorder.Record(ref enumValue, "enumValue");
        }

        public static PrimitivesRecordable Create(int seed)
        {
            return new PrimitivesRecordable
            {
                intValue = seed * 17,
                floatValue = seed * 1.5f,
                doubleValue = seed * 2.718281828,
                boolValue = seed % 2 == 0,
                stringValue = $"string_value_{seed}",
                enumValue = (SimpleEnum)(seed % 5),
            };
        }
    }

    public class NestedRecordable : IRecordable
    {
        public PrimitivesRecordable child;
        public int[] intArray;
        public List<string> stringList;
        public Dictionary<string, int> stringIntDict;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref child, "child");
            recorder.Record(ref intArray, "intArray");
            recorder.Record(ref stringList, "stringList");
            recorder.Record(ref stringIntDict, "stringIntDict");
        }

        public static NestedRecordable Create(int seed, int collectionSize)
        {
            var result = new NestedRecordable
            {
                child = PrimitivesRecordable.Create(seed),
                intArray = Enumerable.Range(seed, collectionSize).ToArray(),
                stringList = Enumerable.Range(seed, collectionSize).Select(i => $"item_{i}").ToList(),
                stringIntDict = new Dictionary<string, int>(),
            };
            for (int i = 0; i < collectionSize; i++)
            {
                result.stringIntDict[$"key_{seed}_{i}"] = seed + i;
            }
            return result;
        }
    }

    public class DeeplyNestedRecordable : IRecordable
    {
        public int value;
        public DeeplyNestedRecordable inner;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref value, "value");
            recorder.Record(ref inner, "inner");
        }

        public static DeeplyNestedRecordable Create(int depth, int seed = 0)
        {
            var result = new DeeplyNestedRecordable { value = seed };
            if (depth > 1)
            {
                result.inner = Create(depth - 1, seed + 1);
            }
            return result;
        }
    }

    public class ManyFieldsRecordable : IRecordable
    {
        public int f0, f1, f2, f3, f4, f5, f6, f7, f8, f9;
        public int f10, f11, f12, f13, f14, f15, f16, f17, f18, f19;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref f0, "f0");
            recorder.Record(ref f1, "f1");
            recorder.Record(ref f2, "f2");
            recorder.Record(ref f3, "f3");
            recorder.Record(ref f4, "f4");
            recorder.Record(ref f5, "f5");
            recorder.Record(ref f6, "f6");
            recorder.Record(ref f7, "f7");
            recorder.Record(ref f8, "f8");
            recorder.Record(ref f9, "f9");
            recorder.Record(ref f10, "f10");
            recorder.Record(ref f11, "f11");
            recorder.Record(ref f12, "f12");
            recorder.Record(ref f13, "f13");
            recorder.Record(ref f14, "f14");
            recorder.Record(ref f15, "f15");
            recorder.Record(ref f16, "f16");
            recorder.Record(ref f17, "f17");
            recorder.Record(ref f18, "f18");
            recorder.Record(ref f19, "f19");
        }

        public static ManyFieldsRecordable Create()
        {
            return new ManyFieldsRecordable
            {
                f0 = 100, f1 = 101, f2 = 102, f3 = 103, f4 = 104,
                f5 = 105, f6 = 106, f7 = 107, f8 = 108, f9 = 109,
                f10 = 110, f11 = 111, f12 = 112, f13 = 113, f14 = 114,
                f15 = 115, f16 = 116, f17 = 117, f18 = 118, f19 = 119,
            };
        }
    }

    // Plain class with no IRecordable — exercises the reflection fallback path during Dec parsing
    public class ReflectionData
    {
        public int intVal = 0;
        public float floatVal = 0;
        public double doubleVal = 0;
        public bool boolVal = false;
        public string strVal = "";
        public SimpleEnum enumVal = SimpleEnum.Alpha;
        public int extra1 = 0;
        public int extra2 = 0;
        public int extra3 = 0;
        public string extra4 = "";
    }

    public class ReflectionDec : Dec.Dec
    {
        public ReflectionData data;
        public List<ReflectionData> dataList;
    }

    public class SharedRefHolder : IRecordable
    {
        public PrimitivesRecordable refA;
        public PrimitivesRecordable refB;

        public void Record(Recorder recorder)
        {
            recorder.Shared().Record(ref refA, "refA");
            recorder.Shared().Record(ref refB, "refB");
        }
    }

    // Converter types

    public struct ConverterStringTarget
    {
        public int x;
        public int y;
    }

    public class ConverterStringImpl : ConverterString<ConverterStringTarget>
    {
        public override string Write(ConverterStringTarget input)
        {
            return $"{input.x},{input.y}";
        }

        public override ConverterStringTarget Read(string input, Dec.Context context)
        {
            var parts = input.Split(',');
            return new ConverterStringTarget
            {
                x = int.Parse(parts[0]),
                y = int.Parse(parts[1]),
            };
        }
    }

    public struct ConverterRecordTarget
    {
        public int value;
        public string name;
    }

    public class ConverterRecordImpl : ConverterRecord<ConverterRecordTarget>
    {
        public override void Record(ref ConverterRecordTarget input, Recorder recorder)
        {
            recorder.Record(ref input.value, "value");
            recorder.Record(ref input.name, "name");
        }
    }

    public class ConverterFactoryTarget
    {
        public int id;
        public string label;
    }

    public class ConverterFactoryImpl : ConverterFactory<ConverterFactoryTarget>
    {
        public override void Write(ConverterFactoryTarget input, Recorder recorder)
        {
            recorder.Record(ref input.id, "id");
            recorder.Record(ref input.label, "label");
        }

        public override ConverterFactoryTarget Create(Recorder recorder)
        {
            var result = new ConverterFactoryTarget();
            recorder.Record(ref result.id, "id");
            return result;
        }

        public override void Read(ref ConverterFactoryTarget input, Recorder recorder)
        {
            recorder.Record(ref input.label, "label");
        }
    }

    public class ConverterHolder : IRecordable
    {
        public ConverterStringTarget stringTarget;
        public ConverterRecordTarget recordTarget;
        public ConverterFactoryTarget factoryTarget;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref stringTarget, "stringTarget");
            recorder.Record(ref recordTarget, "recordTarget");
            recorder.Record(ref factoryTarget, "factoryTarget");
        }
    }

    public class SampleDec : Dec.Dec
    {
        public int hitPoints = 100;
        public string displayName = "Sample";
    }

    public class ComplexGraph : IRecordable
    {
        public List<NestedRecordable> nestedList;
        public HashSet<int> intHashSet;
        public Queue<int> intQueue;
        public Stack<int> intStack;
        public PrimitivesRecordable sharedA;
        public PrimitivesRecordable sharedB; // same instance as sharedA
        public Tuple<int, string> tuple;
        public (int, string, float) valueTuple;
        public int? nullableWithValue;
        public int? nullableNull;
        public Type typeRef;
        public byte[] byteArray;
        public SampleDec decRef;

        public void Record(Recorder recorder)
        {
            recorder.Record(ref nestedList, "nestedList");
            recorder.Record(ref intHashSet, "intHashSet");
            recorder.Record(ref intQueue, "intQueue");
            recorder.Record(ref intStack, "intStack");
            recorder.Shared().Record(ref sharedA, "sharedA");
            recorder.Shared().Record(ref sharedB, "sharedB");
            recorder.Record(ref tuple, "tuple");
            recorder.Record(ref valueTuple, "valueTuple");
            recorder.Record(ref nullableWithValue, "nullableWithValue");
            recorder.Record(ref nullableNull, "nullableNull");
            recorder.Record(ref typeRef, "typeRef");
            recorder.Record(ref byteArray, "byteArray");
            recorder.Record(ref decRef, "decRef");
        }

        public static ComplexGraph Create(SampleDec dec)
        {
            var shared = PrimitivesRecordable.Create(42);
            var result = new ComplexGraph
            {
                nestedList = Enumerable.Range(0, 50).Select(i => NestedRecordable.Create(i, 20)).ToList(),
                intHashSet = new HashSet<int>(Enumerable.Range(0, 500)),
                intQueue = new Queue<int>(Enumerable.Range(0, 200)),
                intStack = new Stack<int>(Enumerable.Range(0, 200)),
                sharedA = shared,
                sharedB = shared,
                tuple = Tuple.Create(42, "hello"),
                valueTuple = (99, "world", 3.14f),
                nullableWithValue = 123,
                nullableNull = null,
                typeRef = typeof(string),
                byteArray = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray(),
                decRef = dec,
            };
            return result;
        }
    }

    // ───────────────────────────────────────────────
    // Base class
    // ───────────────────────────────────────────────

    public abstract class BenchmarkBase
    {
        protected void SetupDec(Type[] explicitTypes = null, Type[] explicitConverters = null)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters
            {
                explicitTypes = explicitTypes ?? Type.EmptyTypes,
                explicitConverters = explicitConverters ?? Type.EmptyTypes,
            };

            // Silence info/warnings but throw on errors so broken benchmarks fail loudly
            Dec.Config.InfoHandler = _ => { };
            Dec.Config.WarningHandler = _ => { };
            Dec.Config.ErrorHandler = str => throw new Exception($"Dec error: {str}");
            Dec.Config.ExceptionHandler = ex => throw ex;
        }

        protected void FinishParser(string xml = null)
        {
            var parser = new Dec.Parser();
            if (xml != null)
            {
                parser.AddString(Dec.Parser.FileType.Xml, xml);
            }
            parser.Finish();
        }

        protected void CleanupDec()
        {
            Dec.Database.Clear();
            Dec.Config.TestParameters = null;
        }
    }

    // ───────────────────────────────────────────────
    // Benchmark classes
    // ───────────────────────────────────────────────

    [MemoryDiagnoser]
    public class PrimitiveBenchmarks : BenchmarkBase
    {
        private PrimitivesRecordable data;
        private string serialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec();
            FinishParser();
            data = PrimitivesRecordable.Create(42);
            serialized = Recorder.Write(data);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark]
        public string Write() => Recorder.Write(data);

        [Benchmark]
        public PrimitivesRecordable Read() => Recorder.Read<PrimitivesRecordable>(serialized);

        [Benchmark]
        public PrimitivesRecordable Clone() => Recorder.Clone(data);
    }

    [MemoryDiagnoser]
    public class ComplexGraphBenchmarks : BenchmarkBase
    {
        private ComplexGraph data;
        private string serialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec(
                explicitTypes: new[] { typeof(SampleDec) }
            );
            FinishParser(@"
                <Decs>
                    <SampleDec decName=""TestDec"">
                        <hitPoints>250</hitPoints>
                        <displayName>Benchmark Dec</displayName>
                    </SampleDec>
                </Decs>");

            data = ComplexGraph.Create(Dec.Database<SampleDec>.Get("TestDec"));
            serialized = Recorder.Write(data);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark]
        public string Write() => Recorder.Write(data);

        [Benchmark]
        public ComplexGraph Read() => Recorder.Read<ComplexGraph>(serialized);

        [Benchmark]
        public ComplexGraph Clone() => Recorder.Clone(data);
    }

    [MemoryDiagnoser]
    public class LargeArrayBenchmarks : BenchmarkBase
    {
        [Params(100, 1000, 10000)]
        public int Size;

        private int[] intArray;
        private string intArraySerialized;

        private string[] stringArray;
        private string stringArraySerialized;

        private byte[] byteArray;
        private string byteArraySerialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec();
            FinishParser();

            intArray = Enumerable.Range(0, Size).ToArray();
            intArraySerialized = Recorder.Write(intArray);

            stringArray = Enumerable.Range(0, Size).Select(i => $"element_{i}").ToArray();
            stringArraySerialized = Recorder.Write(stringArray);

            byteArray = Enumerable.Range(0, Size).Select(i => (byte)(i % 256)).ToArray();
            byteArraySerialized = Recorder.Write(byteArray);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark] public string WriteIntArray() => Recorder.Write(intArray);
        [Benchmark] public int[] ReadIntArray() => Recorder.Read<int[]>(intArraySerialized);
        [Benchmark] public int[] CloneIntArray() => Recorder.Clone(intArray);

        [Benchmark] public string WriteStringArray() => Recorder.Write(stringArray);
        [Benchmark] public string[] ReadStringArray() => Recorder.Read<string[]>(stringArraySerialized);
        [Benchmark] public string[] CloneStringArray() => Recorder.Clone(stringArray);

        [Benchmark] public string WriteByteArray() => Recorder.Write(byteArray);
        [Benchmark] public byte[] ReadByteArray() => Recorder.Read<byte[]>(byteArraySerialized);
        [Benchmark] public byte[] CloneByteArray() => Recorder.Clone(byteArray);
    }

    [MemoryDiagnoser]
    public class LargeDictionaryBenchmarks : BenchmarkBase
    {
        [Params(100, 1000, 10000)]
        public int Size;

        private Dictionary<string, int> flatDict;
        private string flatDictSerialized;

        private Dictionary<int, List<int>> nestedDict;
        private string nestedDictSerialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec();
            FinishParser();

            flatDict = new Dictionary<string, int>();
            for (int i = 0; i < Size; i++)
            {
                flatDict[$"key_{i}"] = i;
            }
            flatDictSerialized = Recorder.Write(flatDict);

            nestedDict = new Dictionary<int, List<int>>();
            for (int i = 0; i < Size; i++)
            {
                nestedDict[i] = Enumerable.Range(i, 5).ToList();
            }
            nestedDictSerialized = Recorder.Write(nestedDict);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark] public string WriteFlatDict() => Recorder.Write(flatDict);
        [Benchmark] public Dictionary<string, int> ReadFlatDict() => Recorder.Read<Dictionary<string, int>>(flatDictSerialized);
        [Benchmark] public Dictionary<string, int> CloneFlatDict() => Recorder.Clone(flatDict);

        [Benchmark] public string WriteNestedDict() => Recorder.Write(nestedDict);
        [Benchmark] public Dictionary<int, List<int>> ReadNestedDict() => Recorder.Read<Dictionary<int, List<int>>>(nestedDictSerialized);
        [Benchmark] public Dictionary<int, List<int>> CloneNestedDict() => Recorder.Clone(nestedDict);
    }

    [MemoryDiagnoser]
    public class LargeListBenchmarks : BenchmarkBase
    {
        [Params(100, 1000, 10000)]
        public int Size;

        private List<NestedRecordable> data;
        private string serialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec();
            FinishParser();

            data = Enumerable.Range(0, Size).Select(i => NestedRecordable.Create(i, 10)).ToList();
            serialized = Recorder.Write(data);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark] public string Write() => Recorder.Write(data);
        [Benchmark] public List<NestedRecordable> Read() => Recorder.Read<List<NestedRecordable>>(serialized);
        [Benchmark] public List<NestedRecordable> Clone() => Recorder.Clone(data);
    }

    [MemoryDiagnoser]
    public class RecordableBenchmarks : BenchmarkBase
    {
        private NestedRecordable nested;
        private string nestedSerialized;

        private DeeplyNestedRecordable deep;
        private string deepSerialized;

        private ManyFieldsRecordable manyFields;
        private string manyFieldsSerialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec();
            FinishParser();

            nested = NestedRecordable.Create(0, 100);
            nestedSerialized = Recorder.Write(nested);

            deep = DeeplyNestedRecordable.Create(50);
            deepSerialized = Recorder.Write(deep);

            manyFields = ManyFieldsRecordable.Create();
            manyFieldsSerialized = Recorder.Write(manyFields);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark] public string WriteNested() => Recorder.Write(nested);
        [Benchmark] public NestedRecordable ReadNested() => Recorder.Read<NestedRecordable>(nestedSerialized);
        [Benchmark] public NestedRecordable CloneNested() => Recorder.Clone(nested);

        [Benchmark] public string WriteDeep() => Recorder.Write(deep);
        [Benchmark] public DeeplyNestedRecordable ReadDeep() => Recorder.Read<DeeplyNestedRecordable>(deepSerialized);
        [Benchmark] public DeeplyNestedRecordable CloneDeep() => Recorder.Clone(deep);

        [Benchmark] public string WriteManyFields() => Recorder.Write(manyFields);
        [Benchmark] public ManyFieldsRecordable ReadManyFields() => Recorder.Read<ManyFieldsRecordable>(manyFieldsSerialized);
        [Benchmark] public ManyFieldsRecordable CloneManyFields() => Recorder.Clone(manyFields);
    }

    [MemoryDiagnoser]
    public class ReflectionBenchmarks : BenchmarkBase
    {
        [Params(10, 100)]
        public int Count;

        private string xml;

        private static string GenerateXml(int count)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<Decs>");
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"  <ReflectionDec decName=\"Dec{i}\">");
                sb.AppendLine("    <data>");
                sb.AppendLine($"      <intVal>{i * 17}</intVal>");
                sb.AppendLine($"      <floatVal>{i * 1.5f}</floatVal>");
                sb.AppendLine($"      <doubleVal>{i * 2.718}</doubleVal>");
                sb.AppendLine($"      <boolVal>{(i % 2 == 0).ToString().ToLower()}</boolVal>");
                sb.AppendLine($"      <strVal>string_{i}</strVal>");
                sb.AppendLine($"      <enumVal>{(SimpleEnum)(i % 5)}</enumVal>");
                sb.AppendLine($"      <extra1>{i * 10}</extra1>");
                sb.AppendLine($"      <extra2>{i * 20}</extra2>");
                sb.AppendLine($"      <extra3>{i * 30}</extra3>");
                sb.AppendLine($"      <extra4>extra_{i}</extra4>");
                sb.AppendLine("    </data>");
                sb.AppendLine("    <dataList>");
                for (int j = 0; j < 5; j++)
                {
                    sb.AppendLine("      <li>");
                    sb.AppendLine($"        <intVal>{i * 100 + j}</intVal>");
                    sb.AppendLine($"        <strVal>list_{i}_{j}</strVal>");
                    sb.AppendLine("      </li>");
                }
                sb.AppendLine("    </dataList>");
                sb.AppendLine("  </ReflectionDec>");
            }
            sb.AppendLine("</Decs>");
            return sb.ToString();
        }

        [GlobalSetup]
        public void Setup()
        {
            xml = GenerateXml(Count);

            // Do an initial parse so Compose benchmark has data
            SetupDec(explicitTypes: new[] { typeof(ReflectionDec) });
            FinishParser(xml);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark]
        public void Parse()
        {
            CleanupDec();
            SetupDec(explicitTypes: new[] { typeof(ReflectionDec) });
            FinishParser(xml);
        }

        [Benchmark]
        public string Compose() => new Composer().ComposeXml(false);
    }

    [MemoryDiagnoser]
    public class ConverterBenchmarks : BenchmarkBase
    {
        private ConverterHolder data;
        private string serialized;

        [GlobalSetup]
        public void Setup()
        {
            SetupDec(
                explicitConverters: new[] { typeof(ConverterStringImpl), typeof(ConverterRecordImpl), typeof(ConverterFactoryImpl) }
            );
            FinishParser();

            data = new ConverterHolder
            {
                stringTarget = new ConverterStringTarget { x = 10, y = 20 },
                recordTarget = new ConverterRecordTarget { value = 42, name = "test" },
                factoryTarget = new ConverterFactoryTarget { id = 7, label = "factory" },
            };
            serialized = Recorder.Write(data);
        }

        [GlobalCleanup]
        public void Cleanup() => CleanupDec();

        [Benchmark] public string Write() => Recorder.Write(data);
        [Benchmark] public ConverterHolder Read() => Recorder.Read<ConverterHolder>(serialized);
        [Benchmark] public ConverterHolder Clone() => Recorder.Clone(data);
    }
}
