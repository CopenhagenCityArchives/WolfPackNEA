﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 
namespace NEA.ArchiveModel.BKG1007 {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.sa.dk/xmlns/diark/1.0")]
    [System.Xml.Serialization.XmlRootAttribute("docIndex", Namespace="http://www.sa.dk/xmlns/diark/1.0", IsNullable=false)]
    public partial class docIndexType {
        
        private documentType[] docField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("doc")]
        public documentType[] doc {
            get {
                return this.docField;
            }
            set {
                this.docField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.sa.dk/xmlns/diark/1.0")]
    public partial class documentType {
        
        private string dIDField;
        
        private string pIDField;
        
        private string mIDField;
        
        private string dCfField;
        
        private string oFnField;
        
        private string aFtField;
        
        private string gmlXsdField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="positiveInteger")]
        public string dID {
            get {
                return this.dIDField;
            }
            set {
                this.dIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="positiveInteger", IsNullable=true)]
        public string pID {
            get {
                return this.pIDField;
            }
            set {
                this.pIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="positiveInteger")]
        public string mID {
            get {
                return this.mIDField;
            }
            set {
                this.mIDField = value;
            }
        }
        
        /// <remarks/>
        public string dCf {
            get {
                return this.dCfField;
            }
            set {
                this.dCfField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="normalizedString")]
        public string oFn {
            get {
                return this.oFnField;
            }
            set {
                this.oFnField = value;
            }
        }
        
        /// <remarks/>
        public string aFt {
            get {
                return this.aFtField;
            }
            set {
                this.aFtField = value;
            }
        }
        
        /// <remarks/>
        public string gmlXsd {
            get {
                return this.gmlXsdField;
            }
            set {
                this.gmlXsdField = value;
            }
        }
    }
}
