using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace PRFactory.Infrastructure.Jira;

/// <summary>
/// Parses Jira comments to extract meaningful information like @claude mentions,
/// answers to questions, and approval/rejection signals.
/// </summary>
public interface IJiraCommentParser
{
    /// <summary>
    /// Checks if a comment mentions @claude.
    /// </summary>
    /// <param name="commentBody">The comment text to check.</param>
    /// <returns>True if the comment contains @claude; otherwise, false.</returns>
    bool IsMentioningClaude(string commentBody);

    /// <summary>
    /// Parses answers from a comment based on the questions asked.
    /// </summary>
    /// <param name="commentBody">The comment text containing answers.</param>
    /// <param name="questions">The list of questions that were asked.</param>
    /// <returns>A dictionary mapping question IDs to answers, or null if no valid answers found.</returns>
    Dictionary<string, string>? ParseAnswers(string commentBody, List<Question> questions);

    /// <summary>
    /// Parses a comment to determine if it contains approval or rejection signals.
    /// </summary>
    /// <param name="commentBody">The comment text to parse.</param>
    /// <returns>The approval status detected in the comment.</returns>
    ApprovalStatus ParseApproval(string commentBody);

    /// <summary>
    /// Extracts a specific instruction or request from a comment mentioning @claude.
    /// </summary>
    /// <param name="commentBody">The comment text.</param>
    /// <returns>The instruction text, or null if no clear instruction found.</returns>
    string? ExtractInstruction(string commentBody);
}

/// <summary>
/// Implementation of Jira comment parser with support for various comment formats.
/// </summary>
public class JiraCommentParser : IJiraCommentParser
{
    private readonly ILogger<JiraCommentParser> _logger;

    // Patterns for detecting approval
    private static readonly string[] ApprovalKeywords = new[]
    {
        "approve",
        "approved",
        "looks good",
        "lgtm",
        "ship it",
        "merge it",
        "go ahead",
        "proceed",
        "accepted",
        "accept"
    };

    // Patterns for detecting rejection
    private static readonly string[] RejectionKeywords = new[]
    {
        "reject",
        "rejected",
        "needs changes",
        "need changes",
        "needs work",
        "not approved",
        "don't approve",
        "do not approve",
        "decline",
        "declined"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraCommentParser"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public JiraCommentParser(ILogger<JiraCommentParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsMentioningClaude(string commentBody)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
            return false;

        // Check for @claude (case-insensitive)
        return commentBody.Contains("@claude", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Dictionary<string, string>? ParseAnswers(string commentBody, List<Question> questions)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            _logger.LogDebug("Comment body is empty, no answers to parse");
            return null;
        }

        if (questions == null || questions.Count == 0)
        {
            _logger.LogDebug("No questions provided to match answers against");
            return null;
        }

        _logger.LogDebug("Attempting to parse answers from comment for {QuestionCount} questions", questions.Count);

        // Try different parsing strategies
        var answers = TryParseNumberedAnswers(commentBody, questions)
                   ?? TryParseQuestionAnswerPairs(commentBody, questions)
                   ?? TryParseInlineAnswers(commentBody, questions);

        if (answers != null && answers.Count > 0)
        {
            _logger.LogInformation("Successfully parsed {AnswerCount} answers from comment", answers.Count);
        }
        else
        {
            _logger.LogDebug("No valid answers could be parsed from comment");
        }

        return answers;
    }

    /// <inheritdoc />
    public ApprovalStatus ParseApproval(string commentBody)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
            return ApprovalStatus.None;

        var lowerBody = commentBody.ToLowerInvariant();

        // Check for rejection keywords first (they're more specific)
        if (RejectionKeywords.Any(keyword => lowerBody.Contains(keyword)))
        {
            _logger.LogDebug("Detected rejection in comment");
            return ApprovalStatus.Rejected;
        }

        // Check for approval keywords
        if (ApprovalKeywords.Any(keyword => lowerBody.Contains(keyword)))
        {
            _logger.LogDebug("Detected approval in comment");
            return ApprovalStatus.Approved;
        }

        _logger.LogDebug("No approval or rejection signal detected in comment");
        return ApprovalStatus.None;
    }

    /// <inheritdoc />
    public string? ExtractInstruction(string commentBody)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
            return null;

        // Remove @claude mention and extract the rest as instruction
        var instruction = Regex.Replace(commentBody, @"@claude\s*", "", RegexOptions.IgnoreCase).Trim();

        if (string.IsNullOrWhiteSpace(instruction))
            return null;

        _logger.LogDebug("Extracted instruction from comment: {Instruction}", instruction);
        return instruction;
    }

    /// <summary>
    /// Attempts to parse answers in numbered format:
    /// 1. Answer text here
    /// 2. Another answer here
    /// </summary>
    private Dictionary<string, string>? TryParseNumberedAnswers(string commentBody, List<Question> questions)
    {
        var answers = new Dictionary<string, string>();

        // Pattern matches: "1. " or "1) " followed by answer text
        var numberPattern = @"^\s*(\d+)[\.\)]\s*(.+?)(?=^\s*\d+[\.\)]|$)";
        var matches = Regex.Matches(commentBody, numberPattern,
            RegexOptions.Multiline | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

        if (matches.Count == 0)
            return null;

        _logger.LogDebug("Found {MatchCount} numbered answers", matches.Count);

        foreach (Match match in matches.Cast<Match>().Where(m => m.Success && m.Groups.Count >= 3))
        {
            var questionNumber = int.Parse(match.Groups[1].Value);
            var answerText = match.Groups[2].Value.Trim();

            // Map to question by 1-based index
            if (questionNumber > 0 && questionNumber <= questions.Count)
            {
                var question = questions[questionNumber - 1];
                answers[question.Id] = answerText;
                _logger.LogDebug("Mapped answer {Number} to question {QuestionId}", questionNumber, question.Id);
            }
        }

        return answers.Count > 0 ? answers : null;
    }

    /// <summary>
    /// Attempts to parse answers in Q&A format:
    /// Q: What is your name?
    /// A: John Doe
    /// </summary>
    private Dictionary<string, string>? TryParseQuestionAnswerPairs(string commentBody, List<Question> questions)
    {
        var answers = new Dictionary<string, string>();

        // Pattern matches Q: question text A: answer text
        var qaPattern = @"Q:\s*(.+?)\s*A:\s*(.+?)(?=Q:|$)";
        var matches = Regex.Matches(commentBody, qaPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        if (matches.Count == 0)
            return null;

        _logger.LogDebug("Found {MatchCount} Q&A pairs", matches.Count);

        foreach (Match match in matches.Cast<Match>().Where(m => m.Success && m.Groups.Count >= 3))
        {
            var questionSnippet = match.Groups[1].Value.Trim();
            var answerText = match.Groups[2].Value.Trim();

            // Try to match question snippet to one of our questions
            var matchedQuestion = FindMatchingQuestion(questionSnippet, questions);

            if (matchedQuestion != null)
            {
                answers[matchedQuestion.Id] = answerText;
                _logger.LogDebug("Matched Q&A pair to question {QuestionId}", matchedQuestion.Id);
            }
        }

        return answers.Count > 0 ? answers : null;
    }

    /// <summary>
    /// Attempts to parse answers that are inline with question references:
    /// "For the repository URL question, it's https://github.com/..."
    /// "The target branch is: main"
    /// </summary>
    private Dictionary<string, string>? TryParseInlineAnswers(string commentBody, List<Question> questions)
    {
        var answers = new Dictionary<string, string>();

        // Split comment into sentences
        var sentences = commentBody.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();

            if (string.IsNullOrWhiteSpace(trimmedSentence))
                continue;

            // Try to match each sentence to a question
            foreach (var question in questions)
            {
                if (answers.ContainsKey(question.Id))
                    continue; // Already found answer for this question

                // Check if sentence references this question
                var questionKeywords = ExtractKeywords(question.Text);

                if (questionKeywords.Any(keyword =>
                    trimmedSentence.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    // Extract the answer part (typically after a colon or "is")
                    var answerMatch = Regex.Match(trimmedSentence, @"(?:is|:|are)\s*(.+)$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

                    if (answerMatch.Success)
                    {
                        var answerText = answerMatch.Groups[1].Value.Trim();
                        answers[question.Id] = answerText;
                        _logger.LogDebug("Matched inline answer to question {QuestionId}", question.Id);
                    }
                }
            }
        }

        return answers.Count > 0 ? answers : null;
    }

    /// <summary>
    /// Finds a question that best matches the given text snippet.
    /// </summary>
    private Question? FindMatchingQuestion(string snippet, List<Question> questions)
    {
        var snippetLower = snippet.ToLowerInvariant();

        // Try exact substring match first
        var exactMatch = questions.FirstOrDefault(q =>
            q.Text.Contains(snippet, StringComparison.OrdinalIgnoreCase) ||
            snippetLower.Contains(q.Text.ToLowerInvariant()));

        if (exactMatch != null)
            return exactMatch;

        // Try keyword-based matching
        var snippetKeywords = ExtractKeywords(snippet);

        var bestMatch = questions
            .Select(q => new
            {
                Question = q,
                Score = CountMatchingKeywords(ExtractKeywords(q.Text), snippetKeywords)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch?.Question;
    }

    /// <summary>
    /// Extracts important keywords from a text string.
    /// </summary>
    private List<string> ExtractKeywords(string text)
    {
        // Remove common stop words and extract significant words
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "should",
            "could", "may", "might", "can", "to", "of", "in", "on", "at", "for",
            "with", "from", "by", "about", "what", "which", "who", "when", "where",
            "why", "how", "your", "my", "our", "this", "that", "these", "those"
        };

        return Regex.Matches(text, @"\b\w+\b", RegexOptions.None, TimeSpan.FromSeconds(1))
            .Cast<Match>()
            .Select(m => m.Value.ToLowerInvariant())
            .Where(word => word.Length > 2 && !stopWords.Contains(word))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Counts how many keywords from list A appear in list B.
    /// </summary>
    private int CountMatchingKeywords(List<string> keywordsA, List<string> keywordsB)
    {
        return keywordsA.Intersect(keywordsB, StringComparer.OrdinalIgnoreCase).Count();
    }
}

/// <summary>
/// Represents a question that was asked to the user.
/// </summary>
public class Question
{
    /// <summary>
    /// Gets or sets the unique identifier for the question.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the question is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the question type (e.g., "text", "choice", "boolean").
    /// </summary>
    public string Type { get; set; } = "text";
}

/// <summary>
/// Represents the approval status detected in a comment.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// No approval or rejection signal detected.
    /// </summary>
    None,

    /// <summary>
    /// The comment indicates approval.
    /// </summary>
    Approved,

    /// <summary>
    /// The comment indicates rejection.
    /// </summary>
    Rejected
}
