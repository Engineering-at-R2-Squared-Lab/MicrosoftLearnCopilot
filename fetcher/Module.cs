namespace fetcher
{
    public class Rating
    {
        public int Count { get; set; }
        public double Average { get; set; }
    }

    public class Module
    {
        required public string Summary { get; set; }
        required public List<string> Levels { get; set; }
        required public List<string> Roles { get; set; }
        required public List<string> Products { get; set; }
        required public string Uid { get; set; }
        required public string Type { get; set; }
        required public string Title { get; set; }
        public int DurationInMinutes { get; set; }
        public Rating Rating { get; set; }
        required public double Popularity { get; set; }
        public string IconUrl { get; set; }
        public string SocialImageUrl { get; set; }
        required public string Locale { get; set; }
        public DateTime LastModified { get; set; }
        required public string Url { get; set; }
        required public string FirstUnitUrl { get; set; }
        required public List<string> Units { get; set; }
        public int NumberOfChildren { get; set; }
    }
}