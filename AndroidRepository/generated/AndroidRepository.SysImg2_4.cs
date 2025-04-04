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
namespace AndroidRepository.SysImg2_4
{
    
    
    /// <summary>
    /// <para>type-details subclass including system image-specific information:
    ///                - tag, specifying the device type (tablet, tv, wear, etc.)
    ///                - vendor, the vendor for this system image (android, google, etc.)
    ///                - abi, the architecture for this image (x86, armeabi-v7a, etc.)</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute(@"type-details subclass including system image-specific information: - tag, specifying the device type (tablet, tv, wear, etc.) - vendor, the vendor for this system image (android, google, etc.) - abi, the architecture for this image (x86, armeabi-v7a, etc.)")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("sysImgDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/sys-img2/04")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("sysImgDetailsType", Namespace="http://schemas.android.com/sdk/android/repo/sys-img2/04")]
    public partial class SysImgDetailsType : AndroidRepository.Common_3.ApiDetailsType
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<AndroidRepository.Common_3.IdDisplayType> _tag;
        
        [System.Xml.Serialization.XmlElementAttribute("tag", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public System.Collections.ObjectModel.Collection<AndroidRepository.Common_3.IdDisplayType> Tag
        {
            get
            {
                return _tag;
            }
            private set
            {
                _tag = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Tag collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TagSpecified
        {
            get
            {
                return (this.Tag.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="SysImgDetailsType" /> class.</para>
        /// </summary>
        public SysImgDetailsType()
        {
            this._tag = new System.Collections.ObjectModel.Collection<AndroidRepository.Common_3.IdDisplayType>();
            this._abis = new System.Collections.ObjectModel.Collection<string>();
            this._translatedAbis = new System.Collections.ObjectModel.Collection<string>();
        }
        
        [System.Xml.Serialization.XmlElementAttribute("vendor", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public AndroidRepository.Common_3.IdDisplayType Vendor { get; set; }
        
        /// <summary>
        /// <para>The ABI of a platform's system image.</para>
        /// <para xml:lang="en">Pattern: (armeabi|armeabi-v7a|arm64-v8a|x86|x86_64|mips|mips64|riscv64).</para>
        /// </summary>
        [System.ComponentModel.DataAnnotations.RegularExpressionAttribute("(armeabi|armeabi-v7a|arm64-v8a|x86|x86_64|mips|mips64|riscv64)")]
        [System.Xml.Serialization.XmlElementAttribute("abi", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Abi { get; set; }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<string> _abis;
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("abis", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public System.Collections.ObjectModel.Collection<string> Abis
        {
            get
            {
                return _abis;
            }
            private set
            {
                _abis = value;
            }
        }
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<string> _translatedAbis;
        
        [System.Xml.Serialization.XmlElementAttribute("translatedAbis", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public System.Collections.ObjectModel.Collection<string> TranslatedAbis
        {
            get
            {
                return _translatedAbis;
            }
            private set
            {
                _translatedAbis = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the TranslatedAbis collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TranslatedAbisSpecified
        {
            get
            {
                return (this.TranslatedAbis.Count != 0);
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("sdk-sys-img", Namespace="http://schemas.android.com/sdk/android/repo/sys-img2/04")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("sdk-sys-img", Namespace="http://schemas.android.com/sdk/android/repo/sys-img2/04")]
    public partial class SdkSysImg : AndroidRepository.Common_2.RepositoryType
    {
    }
}
