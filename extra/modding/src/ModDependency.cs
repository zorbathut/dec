using System;

namespace Dec.Modding
{
    /// <summary>
    /// How one mod relates to another.
    /// </summary>
    /// <remarks>
    /// Required and Incompatible affect loading. The others only change what the player is told.
    /// </remarks>
    public enum ModDependencyKind
    {
        /// <summary>
        /// The target must be installed, enabled, and in range, or this mod is disabled.
        /// </summary>
        Required,

        /// <summary>
        /// The target makes this mod work better; mod screens may suggest installing it.
        /// </summary>
        Recommended,

        /// <summary>
        /// This mod actively integrates with the target.
        /// </summary>
        Supported,

        /// <summary>
        /// This mod made some accommodation for the target without promising support. Also used for entries that exist only to set load order.
        /// </summary>
        Accommodated,

        /// <summary>
        /// The target may misbehave alongside this mod; both still load, with a warning.
        /// </summary>
        Discouraged,

        /// <summary>
        /// This mod can't be used with the target; if both are present, this mod is disabled.
        /// </summary>
        Incompatible,
    }

    /// <summary>
    /// Where the declaring mod loads relative to the target, when the target is present.
    /// </summary>
    /// <remarks>
    /// An entry that doesn't set an order loads After by default, as a preference: if default orders form a cycle, they're ignored with a warning. An order written explicitly is enforced, and mods whose explicit orders form a cycle are disabled.
    /// </remarks>
    public enum ModDependencyOrder
    {
        /// <summary>
        /// The declaring mod loads after the target.
        /// </summary>
        After,

        /// <summary>
        /// The declaring mod loads before the target.
        /// </summary>
        Before,

        /// <summary>
        /// No ordering constraint.
        /// </summary>
        Unordered,
    }

    /// <summary>
    /// One entry in a mod's dependency list.
    /// </summary>
    /// <remarks>
    /// An entry applies only when the target's installed version is in its range.
    /// </remarks>
    public class ModDependency : IRecordable
    {
        private string id;
        private string kind;
        private string version;
        private string order;
        private string text;

        private ModDependencyKind kindParsed = ModDependencyKind.Required;
        private ModDependencyOrder? orderParsed;

        private SemanticVersioning.Range range;
        internal bool invalid;
        internal Context context;

        internal ModDependency() { }

        internal static ModDependency CreateImplicitBase(string baseId)
        {
            return new ModDependency() { id = baseId };
        }

        /// <summary>
        /// The id of the mod this entry refers to.
        /// </summary>
        public string Id
        {
            get
            {
                return id;
            }
        }

        public ModDependencyKind Kind
        {
            get
            {
                return kindParsed;
            }
        }

        /// <summary>
        /// The versions of the target this entry applies to, or null for every version.
        /// </summary>
        public SemanticVersioning.Range Version
        {
            get
            {
                return range;
            }
        }

        public ModDependencyOrder Order
        {
            get
            {
                if (orderParsed.HasValue)
                {
                    return orderParsed.Value;
                }

                return kindParsed == ModDependencyKind.Incompatible ? ModDependencyOrder.Unordered : ModDependencyOrder.After;
            }
        }

        internal bool HasExplicitOrder
        {
            get
            {
                return orderParsed.HasValue;
            }
        }

        /// <summary>
        /// Optional author-supplied text explaining this entry to the player.
        /// </summary>
        public string Text
        {
            get
            {
                return text;
            }
        }

        void IRecordable.Record(Recorder recorder)
        {
            recorder.Record(ref id, "id");
            recorder.Record(ref kind, "kind");
            recorder.Record(ref version, "version");
            recorder.Record(ref order, "order");
            recorder.Record(ref text, "text");

            if (recorder.Mode != Recorder.Direction.Read)
            {
                return;
            }

            context = recorder.Context;
            id = id?.Trim();

            if (id == null)
            {
                ModManifest.Error(recorder, "Dependency has no `id`");
                invalid = true;
            }
            else if (!ModValidation.IsIdValid(id))
            {
                ModManifest.Error(recorder, $"Dependency id `{id}` is invalid; {ModValidation.IdRules}");
                invalid = true;
            }

            if (version != null)
            {
                range = ModValidation.ParseRange(version);
                if (range == null)
                {
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        ModManifest.Error(recorder, "Dependency has an empty `version`; leave the element out to accept every version");
                    }
                    else
                    {
                        ModManifest.Error(recorder, $"Dependency version `{version}` is not a valid version range");
                    }
                    invalid = true;
                }
            }

            // Parsed here rather than by Dec, so an unknown name is a problem with this mod rather than a log message.
            if (kind != null)
            {
                if (ModValidation.TryParseName(kind, out ModDependencyKind parsed))
                {
                    kindParsed = parsed;
                }
                else
                {
                    ModManifest.Error(recorder, $"Dependency kind `{kind}` is unknown; it must be one of {string.Join(", ", Enum.GetNames(typeof(ModDependencyKind)))}");
                    invalid = true;
                }
            }

            if (order != null)
            {
                if (ModValidation.TryParseName(order, out ModDependencyOrder parsed))
                {
                    orderParsed = parsed;
                }
                else
                {
                    ModManifest.Error(recorder, $"Dependency order `{order}` is unknown; it must be one of {string.Join(", ", Enum.GetNames(typeof(ModDependencyOrder)))}");
                    invalid = true;
                }
            }

            if (recorder.UserSettings is ModManifestSettings settings && id == settings.baseId && orderParsed.HasValue && orderParsed != ModDependencyOrder.After)
            {
                ModManifest.Error(recorder, $"Dependency on the base game `{id}` can't set `order` to {orderParsed}; mods always load after the base game");
                invalid = true;
            }

            if (kindParsed == ModDependencyKind.Incompatible && orderParsed.HasValue)
            {
                ModManifest.Error(recorder, "Incompatible dependencies can't set `order`, since the two mods never load together");
                invalid = true;
            }
        }
    }
}
