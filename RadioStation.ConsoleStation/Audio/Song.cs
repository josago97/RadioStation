namespace RadioStation.ConsoleStation.Audio
{
    public class Song
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Category { get; private set; }  

        public Song(string id, string name, string category)
        {
            Id = id;
            Name = name;
            Category = category;
        }

        public override string ToString()
        {
            return $"{Name} ({Category})";
        }
    }
}
