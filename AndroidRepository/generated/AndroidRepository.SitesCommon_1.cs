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
namespace AndroidRepository.SitesCommon_1
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("siteListType", Namespace="http://schemas.android.com/repository/android/sites-common/1")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_3.SdkAddonsList))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_4.SdkAddonsList))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_5.SdkAddonsList))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_6.SdkAddonsList))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(SiteList))]
    public partial class SiteListType
    {
        
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        private System.Collections.ObjectModel.Collection<SiteType> _site;
        
        [System.Xml.Serialization.XmlElementAttribute("site", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public System.Collections.ObjectModel.Collection<SiteType> Site
        {
            get
            {
                return _site;
            }
            private set
            {
                _site = value;
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Gets a value indicating whether the Site collection is empty.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SiteSpecified
        {
            get
            {
                return (this.Site.Count != 0);
            }
        }
        
        /// <summary>
        /// <para xml:lang="en">Initializes a new instance of the <see cref="SiteListType" /> class.</para>
        /// </summary>
        public SiteListType()
        {
            this._site = new System.Collections.ObjectModel.Collection<SiteType>();
        }
    }
    
    /// <summary>
    /// <para>An abstract Site, containing a user-friendly name and URL.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("An abstract Site, containing a user-friendly name and URL.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("siteType", Namespace="http://schemas.android.com/repository/android/sites-common/1")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_3.AddonSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_4.AddonSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_5.AddonSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_6.AddonSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(GenericSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_3.SysImgSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_4.SysImgSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_5.SysImgSiteType))]
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AndroidRepository.AddonsList_6.SysImgSiteType))]
    public abstract partial class SiteType
    {
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("url", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Url { get; set; }
        
        [System.ComponentModel.DataAnnotations.RequiredAttribute(AllowEmptyStrings=true)]
        [System.Xml.Serialization.XmlElementAttribute("displayName", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string DisplayName { get; set; }
    }
    
    /// <summary>
    /// <para>A trivial implementation of siteType.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A trivial implementation of siteType.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("genericSiteType", Namespace="http://schemas.android.com/repository/android/sites-common/1")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GenericSiteType : SiteType
    {
    }
    
    /// <summary>
    /// <para>A simple list of add-ons site.</para>
    /// </summary>
    [System.ComponentModel.DescriptionAttribute("A simple list of add-ons site.")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XmlSchemaClassGenerator", "2.1.1179.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute("site-list", Namespace="http://schemas.android.com/repository/android/sites-common/1")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("site-list", Namespace="http://schemas.android.com/repository/android/sites-common/1")]
    public partial class SiteList : SiteListType
    {
    }
}
