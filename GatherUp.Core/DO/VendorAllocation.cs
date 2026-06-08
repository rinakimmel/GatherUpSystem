using System.Collections.Generic;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    public class VendorAllocation
    {
        [XmlAttribute]
        public string VendorName { get; set; } = string.Empty;

        [XmlAttribute]
        public decimal AmountOwed { get; set; }

        [XmlAttribute]
        public bool ReceiptsReceived { get; set; }

        [XmlArray("Receipts")]
        [XmlArrayItem("Receipt")]
        public List<ReceiptDetails> Receipts { get; set; } = new List<ReceiptDetails>();

        public VendorAllocation() { }

        public VendorAllocation(string vendorName)
        {
            VendorName = vendorName;
        }
    }
}