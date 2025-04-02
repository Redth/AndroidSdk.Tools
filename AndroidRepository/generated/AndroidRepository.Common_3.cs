//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by XmlSchemaClassGenerator version 2.1.1179.0 using the following command:
// xscgen --nf=namespace-mappings.txt --output=./generated/ ./xsd/*.xsd --verbose
namespace AndroidRepository.Common_3
{
    
    
    /// <summary>
    /// <para>Abstract subclass of type-details adding elements to specify the android version
    ///                a package corresponds to.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(("Abstract subclass of type-details adding elements to specify the android version " +
        "a package corresponds to."))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("apiDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("apiDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.Addon2_3.AddonDetailsType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.Repository2_3.PlatformDetailsType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.Repository2_3.SourceDetailsType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.SysImg2_3.SysImgDetailsType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.SysImg2_4.SysImgDetailsType))]
    public abstract partial class ApiDetailsType : AndroidRepository.Common_2.TypeDetails
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("api-level", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string ApiLevel { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("codename", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Codename { get; set; }
        
        [System.Xml.Serialization.XmlElementAttribute("extension-level", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int ExtensionLevel { get; set; }
        
        /// <summary>
        /// <para xml:lang="en">Gets or sets a value indicating whether the ExtensionLevel property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExtensionLevelSpecified { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private bool _baseExtension = true;
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("base-extension", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public bool BaseExtension
        {
            get
            {
                return _baseExtension;
            }
            set
            {
                _baseExtension = value;
            }
        }
    }
    
    /// <summary>
    /// <para>A string with both user-friendly and easily-parsed versions.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A string with both user-friendly and easily-parsed versions.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("idDisplayType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("idDisplayType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    public partial class IdDisplayType
    {
        
        /// <summary>
        /// <para>Simple type enforcing restrictions on machine-readable strings.</para>
        /// <para xml:lang="en">Pattern: [a-zA-Z0-9_-]+.</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.RegularExpressionAttribute("[a-zA-Z0-9_-]+")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("id", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Id { get; set; }
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("display", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Display { get; set; }
    }
    
    /// <summary>
    /// <para>a library provided by this addon</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("a library provided by this addon")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("libraryType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("libraryType", Namespace="http://schemas.android.com/sdk/android/repo/common/03")]
    public partial class LibraryType
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("description", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Description { get; set; }
        
        [System.Xml.Serialization.XmlAttributeAttribute("localJarPath")]
        public string LocalJarPath { get; set; }
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private bool _manifestEntryRequired = true;
        
        [System.ComponentModel.DefaultValueAttribute(true)]
        [System.Xml.Serialization.XmlAttributeAttribute("manifestEntryRequired")]
        public bool ManifestEntryRequired
        {
            get
            {
                return _manifestEntryRequired;
            }
            set
            {
                _manifestEntryRequired = value;
            }
        }
    }
}
