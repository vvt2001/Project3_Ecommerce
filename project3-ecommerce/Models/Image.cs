//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace project3_ecommerce.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Image
    {
        public long ID { get; set; }
        public Nullable<long> ProductID { get; set; }
        public byte[] Content { get; set; }
        public string ImagePath { get; set; }
        public string FileName { get; set; }
    }
}
