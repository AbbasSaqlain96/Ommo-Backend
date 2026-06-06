using OmmoBackend.Dtos;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IQuestionnaireRepository
    {
        Task UpsertAnswersAsync(int companyId, List<QuestionnaireAnswerRequest> answers);
    }
}
