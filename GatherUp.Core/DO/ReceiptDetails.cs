using System;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    public record ReceiptDetails
    {
        [XmlAttribute]
        public string ReceiptNumber { get; init; }

        [XmlAttribute]
        public decimal Amount { get; init; }

        [XmlAttribute]
        public DateTime IssuedDate { get; init; }

        [XmlElement]
        public string? FilePath { get; init; }

        public ReceiptDetails() : this("", 0, DateTime.Now, null) { }

        public ReceiptDetails(string receiptNumber, decimal amount, DateTime issuedDate, string? filePath = null)
        {
            ReceiptNumber = receiptNumber;
            Amount = amount;
            IssuedDate = issuedDate;
            FilePath = filePath;
        }
    }
}