using System;
using Microsoft.VisualStudio.Shell;
using VSLangProj80;

namespace ProtoBufGenerator
{
    /// <summary>
    /// The CustomToolRegistrationAttribute allows you to test and 
    /// debug your Custom Tool in the Visual Studio experiemental hive.
    /// It is applied to your Custom Tool in the template and you need
    /// only change a few values to have it working.
    /// 
    /// When you run your Custom Template project, the Visual Studio 2008
    /// experiemental hive opens and uses this attrbute to temporarily
    /// register your custom tool in that environment.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class CustomToolRegistrationAttribute : RegistrationAttribute
    {
        public CustomToolRegistrationAttribute(string name, Type customToolType)
        {
            Name = name;
            CustomToolType = customToolType;
        }

        /// <summary>
        /// The type that implements the custom tool.  This starts 
        /// as MyCustomTool by default in the template.
        /// </summary>
        public Type CustomToolType { get; set; }

        /// <summary>
        /// The name of your custom tool.  Typically this will be
        /// the same as your type name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string FileExtension { get; set; }

        #region RegistrationAttribute abstract member implementations
        public override void Register(RegistrationContext context)
        {
            using (Key shellRegistrationKey = context.CreateKey(
                string.Format(@"Generators\{0}\{1}",
                    vsContextGuids.vsContextGuidVCSProject,
                    CustomToolType.Name)))
            {
                shellRegistrationKey.SetValue(string.Empty, Name);
                shellRegistrationKey.SetValue("CLSID", CustomToolType.GUID.ToString("B"));
                shellRegistrationKey.SetValue("GeneratesDesignTimeSource", 1);
                shellRegistrationKey.SetValue("GeneratesSharedDesignTimeSource", 1);
            }

            if (!string.IsNullOrEmpty(FileExtension))
            {
                using (Key shellRegistryFileKey = context.CreateKey(
                string.Format(@"Generators\{0}\{1}",
                    vsContextGuids.vsContextGuidVCSProject,
                    FileExtension)))
                {
                    if (FileExtension.StartsWith("."))
                    {
                        shellRegistryFileKey.SetValue(string.Empty,
                            CustomToolType.Name);
                    }
                    else
                    {
                        shellRegistryFileKey.SetValue(string.Empty,
                            "." + CustomToolType.Name);
                    }
                }
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(string.Format(@"Generators\{0}\{1}",
                    vsContextGuids.vsContextGuidVCSProject,
                    CustomToolType.Name));
        }
        #endregion
    }
}
