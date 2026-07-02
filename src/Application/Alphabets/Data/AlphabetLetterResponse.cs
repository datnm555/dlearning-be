namespace Application.Alphabets.Data;

public sealed record AlphabetLetterResponse(
    Guid Id,
    string UpperCase,
    string LowerCase,
    string Name,
    string Sound,
    string ExampleWord,
    string ExampleEmoji,
    int DisplayOrder);
