namespace QuizApi.Models;

public class GameSession
{
    public int Id { get; set; }
    public int CurrentLevel { get; set; } = 1;
    public List<int> CurrentQuestionIds { get; set; } = new List<int>(); // IDs вопросов текущего уровня
    public List<int> AnsweredQuestionIds { get; set; } = new List<int>(); // IDs вопросов с правильными ответами
    public bool IsCompleted { get; set; } = false;
}
