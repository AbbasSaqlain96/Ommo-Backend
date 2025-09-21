using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class SendEmailRepository : ISendEmailRepository
    {
        private readonly AppDbContext _dbContext;
        public SendEmailRepository(AppDbContext dbContext) => _dbContext = dbContext;

        public async Task<int> InsertAsync(SendEmail sendEmail)
        {
            await _dbContext.send_email.AddAsync(sendEmail);
            await _dbContext.SaveChangesAsync();
            return sendEmail.id;
        }

        public async Task MarkSentAsync(int id, DateTime sentAtUtc)
        {
            var row = await _dbContext.send_email.FirstOrDefaultAsync(x => x.id == id);
            if (row == null) return;
            row.status = "sent";
            row.sent_at = sentAtUtc;
            row.error_message ??= string.Empty;
            await _dbContext.SaveChangesAsync();
        }

        public async Task MarkFailedAsync(int id, string errorMessage)
        {
            var row = await _dbContext.send_email.FirstOrDefaultAsync(x => x.id == id);
            if (row == null) return;
            row.status = "failed";
            row.error_message = errorMessage;
            await _dbContext.SaveChangesAsync();
        }
    }
}
