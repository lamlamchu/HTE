public class EmailSimulator
{
    private readonly Random _random = new();

    public EmailModel GetRandomEmail()
    {
        int index = _random.Next(EmailData.Templates.Count);
        return EmailData.Templates[index];
    }
}
