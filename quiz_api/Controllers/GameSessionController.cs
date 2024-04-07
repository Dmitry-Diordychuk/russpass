using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApi.Data;
using QuizApi.Models;

namespace quiz_api.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors("AllowAnyOrigin")]
public class GameSessionController : ControllerBase
{
    private readonly QuizContext _context;
    private const int QuestionsPerLevel = 2;

    public GameSessionController(QuizContext context)
    {
        _context = context;
    }

    [HttpPost("start")]
    public async Task<ActionResult<GameSession>> StartSession()
    {
        var availableQuestionsCount = await _context.Questions.CountAsync();
        // Проверяем, есть ли достаточно вопросов для старта игры
        if (availableQuestionsCount < QuestionsPerLevel)
        {
            // Вопросов недостаточно для начала игры, автоматическая победа
            return Ok("Congratulations! You've automatically won the game due to a lack of questions.");
        }

        var session = new GameSession();
        session.CurrentQuestionIds = await GetQuestionsForLevel(session.CurrentLevel, session.AnsweredQuestionIds);
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        // Возвращаем детали сессии, включая первый набор вопросов
        return Ok(session);
    }

    [HttpGet("state/{sessionId}")]
    public async Task<ActionResult> GetSessionState(int sessionId)
    {
        var session = await _context.GameSessions.FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound("Session not found.");
        }

        // Создание списка текущих вопросов с детальной информацией для фронтенда
        var currentQuestions = await _context.Questions
                                             .Where(q => session.CurrentQuestionIds.Contains(q.Id))
                                             .Select(q => q.Id)
                                             .ToListAsync();

        // Получаем номера вопросов, на которые уже был дан правильный ответ
        // var answeredQuestions = await _context.Questions
        //                                       .Where(q => session.AnsweredQuestionIds.Contains(q.Id))
        //                                       .Select(q => q.Id)
        //                                       .ToListAsync();

        var response = new
        {
            SessionId = session.Id,
            CurrentLevel = session.CurrentLevel,
            CurrentQuestions = currentQuestions,
            AnsweredQuestionIds = session.AnsweredQuestionIds,
            QuestionsAnswered = session.AnsweredQuestionIds.Count,
            IsCompleted = session.IsCompleted
        };

        return Ok(response);
    }

    [HttpPost("answer")]
    public async Task<ActionResult> AnswerQuestion(int sessionId, int questionId, int answerId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound("Session not found.");
        }

        var question = await _context.Questions
                                     .Include(q => q.Answers)
                                     .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question == null)
        {
            return NotFound("Question not found.");
        }

        if (!session.CurrentQuestionIds.Contains(questionId))
        {
            return BadRequest("Question is not part of the current level.");
        }

        var answer = question.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
        {
            return BadRequest("Answer not found.");
        }

        if (!answer.IsCorrect)
        {
            return Ok(new {
                IsCorrect = false,
                Message = question.IncorrectAnswerMessage
            });
        }

        session.AnsweredQuestionIds.Add(questionId);
        // Проверка завершения уровня
        if (session.AnsweredQuestionIds.Count % QuestionsPerLevel == 0)
        {
            var nextQuestions = await GetQuestionsForLevel(session.CurrentLevel + 1, session.AnsweredQuestionIds);
            if (nextQuestions.Count == 0)
            {
                // Вопросы закончились, игрок победил
                session.IsCompleted = true;
                await _context.SaveChangesAsync();
                return Ok(new {
                    IsCorrect = true,
                    GoNextLevel = true,
                    IsComplete = true,
                });
            }
            else
            {
                // Переход на следующий уровень
                session.CurrentLevel++;
                session.CurrentQuestionIds = nextQuestions;
                await _context.SaveChangesAsync();
                return Ok(new {
                    IsCorrect = true,
                    GoNextLevel = true,
                    IsComplete = false
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new {
            IsCorrect = true,
            GoNextLevel = false,
            IsComplete = false
        });
    }

    // POST: api/GameSession/end/{sessionId}
    [HttpPost("end/{sessionId}")]
    public async Task<ActionResult> EndSession(int sessionId)
    {
        var session = await _context.GameSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
        {
            return NotFound("Session not found.");
        }

        // Проверяем, не завершена ли уже сессия
        if (session.IsCompleted)
        {
            return BadRequest("This session is already completed.");
        }

        // Завершаем сессию
        session.IsCompleted = true;
        await _context.SaveChangesAsync();

        return Ok("Session has been successfully ended.");
    }

    private async Task<List<int>> GetQuestionsForLevel(int level, List<int> answeredQuestionIds)
    {
        var availableQuestionsIds = await _context.Questions
                                                  .Where(q => !answeredQuestionIds.Contains(q.Id))
                                                  .Select(q => q.Id)
                                                  .ToListAsync();

        // Если доступных вопросов недостаточно для нового уровня, возвращаем пустой список
        if (availableQuestionsIds.Count < QuestionsPerLevel)
        {
            return new List<int>();
        }

        var random = new Random();
        var selectedQuestionsIds = availableQuestionsIds
                                  .OrderBy(x => random.Next())
                                  .Take(QuestionsPerLevel)
                                  .ToList();

        return selectedQuestionsIds;
    }
}
