namespace Models
{
    public sealed record Issue
    {
        public List<IssueCustomField> CustomFields { get; set; } = [];
        // Поле не используется
        public string State { get; set; }
        public long? Resolved { get; set; }
        // Тут может быть NRE (null reference exception), надо предусмотреть и перехватить, указав более правильное
        public int StoryPoints => CustomFields.FirstOrDefault().Value;
    }

    public sealed record IssueCustomField
    {
        public int Value { get; set; }
    }

}