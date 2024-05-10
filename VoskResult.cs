namespace TelegramBot
{
    public class VoskResult
    {
        public List<Result>? Result { get; set; }
        public string? Text { get; set; }
    }

    public class Result
    {
        public double Conf { get; set; }
        public double End { get; set; }
        public double Start { get; set; }
        public string? Word { get; set; }
    }
}
