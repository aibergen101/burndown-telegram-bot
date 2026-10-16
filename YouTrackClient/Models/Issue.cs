namespace Models
{
    public sealed record Issue
    {
        public List<IssueCustomField> CustomFields { get; set; } = [];
        public string State { get; set; }
        public long? Resolved { get; set; }
        public int StoryPoints => CustomFields.FirstOrDefault().Value;
    }

    public sealed record IssueCustomField
    {
        public int Value { get; set; }
    }

}