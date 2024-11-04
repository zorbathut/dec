using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Reflection : Base
    {
        // I'm kind of surprised this works, frankly
        public class AssemblyFaked : Assembly
        {
            private readonly string fullName;

            public AssemblyFaked(string name)
            {
                fullName = name;
            }

            public override string FullName
            {
                get
                {
                    return fullName;
                }
            }
        }

        public string[] ApplyUserAssemblyFilter(string[] inp)
        {
            var IsUserAssemblyHandle = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilReflection").GetMethod("IsUserAssembly", BindingFlags.Static | BindingFlags.NonPublic);
            bool IsUserAssembly(Assembly asm)
            {
                return (bool)IsUserAssemblyHandle.Invoke(null, new object[] { asm });
            }

            var results = new List<string>();
            foreach (var entry in inp)
            {
                if (IsUserAssembly(new AssemblyFaked(entry)))
                {
                    results.Add(entry);
                }
            }

            return results.ToArray();
        }

        [Test]
        public void UserAssemblyUnity()
        {
            // Unity defaults (2019.3.9)
            var input = new string[]
            {
                "Assembly - CSharp, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "ExCSS.Unity, Version = 2.0.6.0, Culture = neutral, PublicKeyToken = null",
                "Mono.Security, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = 0738eb9f132ed756",
                "System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089",
                "System.Configuration, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a",
                "System.Core, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089",
                "System.Xml, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089",
                "System.Xml.Linq, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089",
                "Tests, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.Cecil, Version = 0.10.0.0, Culture = neutral, PublicKeyToken = fc15b93552389f74",
                "Unity.CollabProxy.Editor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.CompilationPipeline.Common, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.Legacy.NRefactory, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.Rider.Editor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.SerializationLogic, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.TextMeshPro, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.TextMeshPro.Editor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.Timeline, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.Timeline.Editor, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Unity.VSCode.Editor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor.Graphs, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor.TestRunner, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor.UI, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor.VR, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEditor.WindowsStandalone.Extensions, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AIModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ARModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AccessibilityModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AndroidJNIModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AnimationModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AssetBundleModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.AudioModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ClothModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ClusterInputModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ClusterRendererModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.CoreModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.CrashReportingModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.DSPGraphModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.DirectorModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.GameCenterModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.GridModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.HotReloadModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.IMGUIModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ImageConversionModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.InputLegacyModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.InputModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.JSONSerializeModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.LocalizationModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ParticleSystemModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.PerformanceReportingModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.Physics2DModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.PhysicsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ProfilerModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.ScreenCaptureModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.SharedInternalsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.SpriteMaskModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.SpriteShapeModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.StreamingModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.SubstanceModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.SubsystemsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TLSModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TerrainModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TerrainPhysicsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TestRunner, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TextCoreModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TextRenderingModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.TilemapModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UI, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UIElementsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UIModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UNETModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UmbraModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityAnalyticsModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityConnectModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityTestProtocolModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityWebRequestAssetBundleModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityWebRequestAudioModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityWebRequestModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityWebRequestTextureModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.UnityWebRequestWWWModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.VFXModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.VRModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.VehiclesModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.VideoModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.WindModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "UnityEngine.XRModule, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "mscorlib, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089",
                "netstandard, Version = 2.0.0.0, Culture = neutral, PublicKeyToken = cc7b13ffcd2ddd51",
                "nunit.framework, Version = 3.5.0.0, Culture = neutral, PublicKeyToken = 2638cd05610744eb",
            };

            Assert.AreEqual(new string[] {
                "Assembly - CSharp, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
                "Tests, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null",
            }, ApplyUserAssemblyFilter(input));
        }

        [Test]
        public void UserAssemblyConsole()
        {
            // Loaf results
            var input = new string[]
            {
                "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                "System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Runtime.Extensions, Version= 4.2.1.0, Culture= neutral, PublicKeyToken= b03f5f7f11d50a3a",
                "dec, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "loaf, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
                "netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
            };

            Assert.AreEqual(new string[] {
                "loaf, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null",
            }, ApplyUserAssemblyFilter(input));
        }

        [Test]
        public void CreatedNull()
        {
            var CreateInstanceSafeHandle = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilReflection").GetMethod("CreateInstanceSafe", BindingFlags.Static | BindingFlags.NonPublic);
            object CreateInstanceSafe(Type type, string errorType, object readerNode)
            {
                return CreateInstanceSafeHandle.Invoke(null, new object[] { type, errorType, readerNode });
            }

            ExpectErrors(() => Assert.AreEqual(null, CreateInstanceSafe(typeof(int?), "object", null)));
        }
    }
}
