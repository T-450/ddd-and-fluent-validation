namespace DomainModel
{
    public class Course : Entity
    {
        public Course(long id, string name, int credits)
        {
            Id = id;
            Name = name;
            Credits = credits;
        }

        public string Name { get; }
        public int Credits { get; }
    }
}
