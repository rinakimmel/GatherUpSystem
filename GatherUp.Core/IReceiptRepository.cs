using System.Collections.Generic;
using System.Threading.Tasks;
using GatherUp.Core.DO;

namespace GatherUp.Core
{
    public interface IReceiptRepository
    {
        Task AddAsync(ReceiptDetails receipt);
        Task<ReceiptDetails?> GetByReceiptNumberAsync(string receiptNumber);
        Task<IEnumerable<ReceiptDetails>> GetAllAsync();
    }
}
