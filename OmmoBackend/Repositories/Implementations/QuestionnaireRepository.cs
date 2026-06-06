using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class QuestionnaireRepository : IQuestionnaireRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<QuestionnaireRepository> _logger;
        public QuestionnaireRepository(AppDbContext dbContext, ILogger<QuestionnaireRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task UpsertAnswersAsync(int companyId, List<QuestionnaireAnswerRequest> answers)
        {
            try
            {
                foreach (var answer in answers)
                {
                    var existing = await _dbContext.questionnaire
                        .FirstOrDefaultAsync(x =>
                            x.company_id == companyId &&
                            x.questionnaire_number == answer.QuestionNumber);

                    if (existing == null)
                    {
                        await _dbContext.questionnaire.AddAsync(new Questionnaire
                        {
                            company_id = companyId,
                            questionnaire_number = answer.QuestionNumber,
                            answer = answer.AnswerText,
                            updated_at = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.answer = answer.AnswerText;
                        existing.updated_at = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to upsert questionnaire answers for CompanyId {CompanyId}",
                    companyId);

                throw;
            }
        }
    }
}
