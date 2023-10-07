using Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using VolumeControl.Log;
using VolumeControl.TypeExtensions;

namespace volume_control_audioAPI_test
{
    [HotkeyActionGroup("Test Group 1")]
    public class AddonTestClass1
    {
        [HotkeyAction(Description = "Test 1-1")]
        public void TestMethod1(object? sender, HotkeyActionPressedEventArgs e)
        {

        }
    }
    public readonly struct AttributeWrapper
    {
        public AttributeWrapper(Type attributeType, bool checkInherited = false)
        {
            if (!attributeType.IsDerivedFrom(typeof(Attribute)))
                throw new ArgumentException($"{nameof(AttributeWrapper)} created with invalid attribute type '{attributeType.FullName}'; expected a type derived from '{typeof(Attribute).FullName}'!", nameof(attributeType));

            AttributeType = attributeType;
            CheckInherited = checkInherited;

            if (AttributeType.GetCustomAttribute<AttributeUsageAttribute>() is AttributeUsageAttribute usageAttribute)
            {
                AttributeTargets = usageAttribute.ValidOn;
            }
            else AttributeTargets = AttributeTargets.All;
        }

        #region Properties
        public Type AttributeType { get; }
        public AttributeTargets AttributeTargets { get; }
        /// <summary>
        /// Gets whether or not this attribute may be retrieved from base objects/methods when possible.
        /// </summary>
        public bool CheckInherited { get; }

        private static LogWriter Log => FLog.Log;
        #endregion Properties

        internal Attribute? TryExtractFromType(Type type)
        {
            try
            {
                return type.GetCustomAttribute(AttributeType, CheckInherited);
            }
            catch (TypeLoadException ex)
            {
                Log.Debug($"Addon type \"{type.FullName}\" could not be loaded because it depends on missing type \"{ex.TypeName}\"!", ex);
            }
            catch (Exception ex)
            {
                Log.Debug($"Addon type \"{type.FullName}\" could not be loaded because of an exception!", ex);
            }
            return null;
        }
        internal Attribute? TryExtractFromMember(MemberInfo member)
        {
            try
            {
                return member.GetCustomAttribute(AttributeType, CheckInherited);
            }
            catch (TypeLoadException ex)
            {
                Log.Debug($"Addon member \"{member.Name}\" (Declared in type: {member.DeclaringType?.FullName}) could not be loaded because it depends on missing type \"{ex.TypeName}\"!", ex);
            }
            catch (Exception ex)
            {
                Log.Debug($"Addon member \"{member.Name}\" (Declared in type: {member.DeclaringType?.FullName}) could not be loaded because of an exception!", ex);
            }
            return null;
        }
        internal (MemberInfo?, Attribute)[] ExtractAllFromType(Type type)
        {
            List<(MemberInfo?, Attribute)> l = new();

            switch (AttributeTargets)
            {
            case AttributeTargets.Class:
                {
                    if (!type.IsClass) break;
                    if (TryExtractFromType(type) is Attribute attribute)
                    {
                        l.Add((null, attribute));
                    }
                    break;
                }
            case AttributeTargets.Method:
                {
                    foreach (var methodInfo in type.GetMethods())
                    {
                        if (TryExtractFromMember(methodInfo) is Attribute attribute)
                        {
                            l.Add((methodInfo, attribute));
                        }
                    }
                    break;
                }
            case AttributeTargets.Property:
                {
                    foreach (var propertyInfo in type.GetProperties())
                    {
                        if (TryExtractFromMember(propertyInfo) is Attribute attribute)
                        {
                            l.Add((propertyInfo, attribute));
                        }
                    }
                    break;
                }
            default:
                break;
            }

            return l.ToArray();
        }
    }

    public abstract class AddonLoader
    {
        public AddonLoader(params AttributeWrapper[] targetAttributes)
        {
            TargetAttributes = targetAttributes.ToList();
        }
        public AddonLoader() => TargetAttributes = new();

        public List<AttributeWrapper> TargetAttributes { get; }

        protected static LogWriter Log => FLog.Log;

        internal (MemberInfo?, Attribute)[] GetAllTargetAttributesFromType(Type type)
        {
            List<(MemberInfo?, Attribute)> l = new();
            foreach (var targetAttribute in TargetAttributes)
            {
                l.AddRange(targetAttribute.ExtractAllFromType(type));
            }
            return l.ToArray();
        }
        protected abstract void HandleTypes(Type[] types);

        public void LoadFromTypes(params Type[] types)
            => HandleTypes(types);
        public void LoadFromAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                var exportedTypes = assembly.GetExportedTypes();

                if (exportedTypes.Length == 0)
                    continue;

                LoadFromTypes(exportedTypes);
            }
        }
        public void LoadFromAssemblyPaths(IEnumerable<string> assemblyPaths)
        {
            List<Assembly> assemblies = new();
            foreach (var path in assemblyPaths)
            {
                try
                {
                    assemblies.Add(Assembly.LoadFile(path));
                }
                catch (FileNotFoundException)
                {
                    Log.Debug($"{nameof(Assembly)} not found at \"{path}\"; file does not exist!");
                }
                catch (Exception ex)
                {
                    Log.Debug($"{nameof(Assembly)} at \"{path}\" could not be loaded because of an exception!", ex);
                }
            }
            LoadFromAssemblies(assemblies.ToArray());
        }
        public void LoadFromDirectory(string path, bool recursive = true)
        {
            LoadFromAssemblyPaths(Directory.EnumerateFiles(path, "*.dll", new EnumerationOptions() { RecurseSubdirectories = recursive }));
        }
    }
    public class HotkeyActionAddonLoader : AddonLoader
    {
        public HotkeyActionAddonLoader() : base(new(typeof(HotkeyActionGroupAttribute)), new(typeof(HotkeyActionAttribute))) { }

        public List<HotkeyAction> LoadedHotkeyActions { get; } = new();

        protected override void HandleTypes(Type[] types)
        {
            List<HotkeyAction> hotkeyActions = new();
            foreach (var type in types)
            {
                var attributes = GetAllTargetAttributesFromType(type);
                if (attributes.Length > 0)
                {
                    var inst = Activator.CreateInstance(type); //< create an instance of this type

                    var actionGroupAttr = attributes.FirstOrDefault(pr => pr.Item2 is HotkeyActionGroupAttribute).Item2 as HotkeyActionGroupAttribute;

                    string? defaultGroupName = actionGroupAttr?.GroupName;
                    Brush? defaultGroupBrush = actionGroupAttr?.GroupColor is not null ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(actionGroupAttr.GroupColor)) : null;

                    foreach (var (memberInfo, attribute) in attributes)
                    {
                        if (attribute.Equals(actionGroupAttr)) continue;


                        if (attribute is HotkeyActionAttribute hAttr && memberInfo is MethodInfo methodInfo)
                        {
                            var data = hAttr.GetActionData();
                            if (data.ActionGroup is null && defaultGroupName is not null)
                                data.ActionGroup = defaultGroupName;
                            if (data.ActionGroupBrush is null && defaultGroupBrush is not null)
                                data.ActionGroupBrush = defaultGroupBrush;

                            // validate parameters & return type:
                            if (!methodInfo.ReturnType.Equals(typeof(void)))
                            {
                                Log.Debug($"Addon method return value is discarded");
                            }

                            var parameters = methodInfo.GetParameters();
                            if (parameters.Length < 2)
                            {
                                Log.Debug(
                                    $"Addon method is invalid: '{methodInfo.Name}' (Invalid Function Declaration; Missing Parameters)",
                                    $"Hotkey action methods must accept a first parameter of type `{typeof(object).FullName}`, and a second parameter of type `{typeof(System.ComponentModel.HandledEventArgs).FullName}` or `{typeof(HotkeyActionPressedEventArgs).FullName}`!");
                                continue;
                            }
                            else if (!parameters[0].ParameterType.Equals(typeof(object)) || (!parameters[1].ParameterType.Equals(typeof(System.ComponentModel.HandledEventArgs)) && !parameters[1].ParameterType.Equals(typeof(HotkeyActionPressedEventArgs))))
                            {
                                Log.Debug(
                                    $"Addon method is invalid: '{methodInfo.Name}' (Invalid Function Declaration; First Parameter)",
                                    $"Hotkey action methods must accept a first parameter of type `{typeof(object).FullName}`, and a second parameter of type `{typeof(System.ComponentModel.HandledEventArgs).FullName}` or `{typeof(HotkeyActionPressedEventArgs).FullName}`!");
                                continue;
                            }

                            hotkeyActions.Add(new HotkeyAction(inst, methodInfo, data));
                        }
                        else
                        {
                            Log.Error($"Addon method is invalid: {memberInfo?.Name}");
                        }
                    }
                }
            }
        }
    }
}
