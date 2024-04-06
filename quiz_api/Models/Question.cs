using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Question
{
    [Key]
    public int Id { get; set; }
    public string Text { get; set; }
    public string ImageUrl { get; set; }
    public string IncorrectAnswerMessage { get; set; }
    public List<Answer> Answers { get; set; }
}

public class Answer
{
    [Key]
    public int Id { get; set; }
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
}
