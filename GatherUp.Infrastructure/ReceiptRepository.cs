using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using GatherUp.Core.DO; // domain objects
using GatherUp.Core; // IReceiptRepository

namespace GatherUp.Infrastructure.Data
{
    public class ReceiptRepository : IReceiptRepository
    {
        private readonly string _xmlFilePath;
        private readonly string _receiptsFolderPath;

        public ReceiptRepository(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("Directory path cannot be empty", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            _xmlFilePath = Path.Combine(directoryPath, "Receipts.xml");
            _receiptsFolderPath = Path.Combine(directoryPath, "ReceiptFiles");

            if (!Directory.Exists(_receiptsFolderPath))
                Directory.CreateDirectory(_receiptsFolderPath);
        }

        public Task AddAsync(ReceiptDetails entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return Task.Run(() =>
            {
                string? newFilePath = null;
                if (!string.IsNullOrEmpty(entity.FilePath) && File.Exists(entity.FilePath))
                {
                    var fileName = Path.GetFileName(entity.FilePath);
                    newFilePath = Path.Combine(_receiptsFolderPath, $"{entity.ReceiptNumber}_{fileName}");
                    File.Copy(entity.FilePath, newFilePath, true);
                }

                var doc = XMLDocManager.LoadDocument(_xmlFilePath);
                if (doc.Root == null)
                    doc = XMLDocManager.CreateDocument("Receipts");

                var receiptElement = new XElement("Receipt",
                    new XAttribute("ReceiptNumber", entity.ReceiptNumber),
                    new XAttribute("Amount", entity.Amount),
                    new XAttribute("IssuedDate", entity.IssuedDate.ToString("O")));

                if (newFilePath != null)
                    receiptElement.Add(new XElement("FilePath", newFilePath));

                XMLDocManager.AddElement(doc, receiptElement);
                XMLDocManager.SaveDocument(doc, _xmlFilePath);
            });
        }

        public Task<ReceiptDetails?> GetByReceiptNumberAsync(string receiptNumber)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(_xmlFilePath))
                    return null as ReceiptDetails;

                var doc = XMLDocManager.LoadDocument(_xmlFilePath);
                var element = doc.Root?.Elements("Receipt")
                    .FirstOrDefault(e => (string?)e.Attribute("ReceiptNumber") == receiptNumber);

                if (element == null)
                    return null as ReceiptDetails;

                var filePath = element.Element("FilePath")?.Value;
                return new ReceiptDetails(
                    receiptNumber: (string?)element.Attribute("ReceiptNumber") ?? "",
                    amount: (decimal?)element.Attribute("Amount") ?? 0,
                    issuedDate: DateTime.Parse((string?)element.Attribute("IssuedDate") ?? DateTime.Now.ToString("O")),
                    filePath: filePath
                );
            });
        }

        public Task<IEnumerable<ReceiptDetails>> GetAllAsync()
        {
            return Task.Run<IEnumerable<ReceiptDetails>>(() =>
            {
                if (!File.Exists(_xmlFilePath))
                    return new List<ReceiptDetails>();

                var doc = XMLDocManager.LoadDocument(_xmlFilePath);
                var receipts = new List<ReceiptDetails>();

                foreach (var element in doc.Root?.Elements("Receipt") ?? Enumerable.Empty<XElement>())
                {
                    var filePath = element.Element("FilePath")?.Value;
                    var receipt = new ReceiptDetails(
                        receiptNumber: (string?)element.Attribute("ReceiptNumber") ?? "",
                        amount: (decimal?)element.Attribute("Amount") ?? 0,
                        issuedDate: DateTime.Parse((string?)element.Attribute("IssuedDate") ?? DateTime.Now.ToString("O")),
                        filePath: filePath
                    );
                    receipts.Add(receipt);
                }

                return receipts;
            });
        }
    }
}