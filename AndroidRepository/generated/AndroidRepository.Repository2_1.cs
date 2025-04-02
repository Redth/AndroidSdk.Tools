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
namespace AndroidRepository.Repository2_1
{
    
    
    /// <summary>
    /// <para>type-details subclass for platform components, including information on the
    ///                layoutlib provided.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(("type-details subclass for platform components, including information on the layou" +
        "tlib provided."))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("platformDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("platformDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    public partial class PlatformDetailsType : AndroidRepository.Common_1.ApiDetailsType
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("layoutlib", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public LayoutlibType Layoutlib { get; set; }
    }
    
    /// <summary>
    /// <para>The API level used by the LayoutLib included in a platform to communicate with the IDE.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(("The API level used by the LayoutLib included in a platform to communicate with th" +
        "e IDE."))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("layoutlibType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("layoutlibType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    public partial class LayoutlibType
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlAttributeAttribute("api")]
        public int Api { get; set; }
    }
    
    /// <summary>
    /// <para>trivial type-details subclass for source components.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("trivial type-details subclass for source components.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("sourceDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("sourceDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    public partial class SourceDetailsType : AndroidRepository.Common_1.ApiDetailsType
    {
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("sdk-repository", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("sdk-repository", Namespace="http://schemas.android.com/sdk/android/repo/repository2/01")]
    public partial class SdkRepository : AndroidRepository.Common_1.RepositoryType
    {
    }
}
