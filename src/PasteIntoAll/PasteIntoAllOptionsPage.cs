using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace PasteIntoAll
{
    /// <summary>
    /// Settings available in Tools &gt; Options &gt; Paste into all.
    /// The attribute texts are English fallbacks; <see cref="ICustomTypeDescriptor"/> replaces
    /// them with the strings of the current Visual Studio UI language.
    /// </summary>
    public class PasteIntoAllOptionsPage : DialogPage, ICustomTypeDescriptor
    {
        [Category("Pasting")]
        [DisplayName("Paste delay (ms)")]
        [Description("How long to wait after activating the target window before pressing Ctrl+V.")]
        [DefaultValue(350)]
        public int PasteDelayMs { get; set; } = 350;

        [Category("Pasting")]
        [DisplayName("Start OpenCode when it is not running")]
        [Description("Start the OpenCode desktop application when no window is found.")]
        [DefaultValue(true)]
        public bool LaunchOpenCodeIfNotRunning { get; set; } = true;

        [Category("Pasting")]
        [DisplayName("Startup timeout (seconds)")]
        [Description("How long to wait for the target window after starting the application.")]
        [DefaultValue(30)]
        public int LaunchTimeoutSeconds { get; set; } = 30;

        [Category("Pasting")]
        [DisplayName("OpenCode executable path")]
        [Description("Optional. Leave empty to auto-detect OpenCode.exe.")]
        [DefaultValue("")]
        public string OpenCodeExecutablePath { get; set; } = string.Empty;

        [Category("Pasting")]
        [DisplayName("Target application (process or window title)")]
        [Description("Process name (opencode, chrome, Code, notepad) or a fragment of a window title. The application must already be open. Leave empty to paste into OpenCode.")]
        [DefaultValue("opencode")]
        public string TargetApplication { get; set; } = "opencode";

        [Category("Content")]
        [DisplayName("Send the full file path when the whole file is selected")]
        [Description("When the selection covers the whole document, its absolute path is sent instead of the code.")]
        [DefaultValue(true)]
        public bool IncludeFullPathForWholeFile { get; set; } = true;

        [Category("Content")]
        [DisplayName("Also append the file contents for a whole file")]
        [Description("Off by default: only the path is sent for a whole file.")]
        [DefaultValue(false)]
        public bool IncludeCodeWithWholeFilePath { get; set; } = false;

        [Category("Content")]
        [DisplayName("Treat an empty selection as the whole file")]
        [Description("If nothing is selected, the whole document is treated as selected.")]
        [DefaultValue(true)]
        public bool EmptySelectionMeansWholeFile { get; set; } = true;

        // ---- Localized property descriptions ---------------------------------------

        private sealed class LocalizedPropertyDescriptor : PropertyDescriptor
        {
            private readonly PropertyDescriptor _inner;
            private readonly string _displayName;
            private readonly string _description;
            private readonly string _category;

            public LocalizedPropertyDescriptor(
                PropertyDescriptor inner,
                string displayName,
                string description,
                string category)
                : base(inner, null)
            {
                _inner = inner;
                _displayName = displayName;
                _description = description;
                _category = category;
            }

            public override string DisplayName => _displayName;

            public override string Description => _description;

            public override string Category => _category;

            public override Type ComponentType => _inner.ComponentType;

            public override bool IsReadOnly => _inner.IsReadOnly;

            public override Type PropertyType => _inner.PropertyType;

            public override bool CanResetValue(object component) => _inner.CanResetValue(component);

            public override object GetValue(object component) => _inner.GetValue(component);

            public override void ResetValue(object component) => _inner.ResetValue(component);

            public override void SetValue(object component, object value) => _inner.SetValue(component, value);

            public override bool ShouldSerializeValue(object component) => _inner.ShouldSerializeValue(component);
        }

        private static PropertyDescriptor Localize(PropertyDescriptor property)
        {
            switch (property.Name)
            {
                case nameof(PasteDelayMs):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.PasteDelayName,
                        Localization.PasteDelayDescription,
                        Localization.CategoryPasting);
                case nameof(LaunchOpenCodeIfNotRunning):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.LaunchName,
                        Localization.LaunchDescription,
                        Localization.CategoryPasting);
                case nameof(LaunchTimeoutSeconds):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.LaunchTimeoutName,
                        Localization.LaunchTimeoutDescription,
                        Localization.CategoryPasting);
                case nameof(OpenCodeExecutablePath):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.ExecutableName,
                        Localization.ExecutableDescription,
                        Localization.CategoryPasting);
                case nameof(TargetApplication):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.TargetName,
                        Localization.TargetDescription,
                        Localization.CategoryPasting);
                case nameof(IncludeFullPathForWholeFile):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.WholeFilePathName,
                        Localization.WholeFilePathDescription,
                        Localization.CategoryContent);
                case nameof(IncludeCodeWithWholeFilePath):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.AppendCodeName,
                        Localization.AppendCodeDescription,
                        Localization.CategoryContent);
                case nameof(EmptySelectionMeansWholeFile):
                    return new LocalizedPropertyDescriptor(
                        property,
                        Localization.EmptySelectionName,
                        Localization.EmptySelectionDescription,
                        Localization.CategoryContent);
                default:
                    return property;
            }
        }

        // ---- ICustomTypeDescriptor -------------------------------------------------

        AttributeCollection ICustomTypeDescriptor.GetAttributes() =>
            TypeDescriptor.GetAttributes(GetType());

        string ICustomTypeDescriptor.GetClassName() =>
            TypeDescriptor.GetClassName(GetType());

        string ICustomTypeDescriptor.GetComponentName() =>
            TypeDescriptor.GetComponentName(GetType());

        TypeConverter ICustomTypeDescriptor.GetConverter() =>
            TypeDescriptor.GetConverter(GetType());

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() =>
            TypeDescriptor.GetDefaultEvent(GetType());

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() =>
            TypeDescriptor.GetDefaultProperty(GetType());

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) =>
            TypeDescriptor.GetEditor(GetType(), editorBaseType);

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() =>
            TypeDescriptor.GetEvents(GetType());

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) =>
            TypeDescriptor.GetEvents(GetType(), attributes);

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
            ((ICustomTypeDescriptor)this).GetProperties(null);

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection source = TypeDescriptor.GetProperties(GetType(), attributes);
            var localized = new PropertyDescriptor[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                localized[i] = Localize(source[i]);
            }

            return new PropertyDescriptorCollection(localized);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;
    }
}
