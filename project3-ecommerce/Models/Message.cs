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
    
    public partial class Message
    {
        public long ID { get; set; }
        public string Message1 { get; set; }
        public Nullable<long> SenderID { get; set; }
        public Nullable<long> ReceiverID { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public string GroupName { get; set; }
    }
}