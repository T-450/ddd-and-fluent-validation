using System.Linq;
using DomainModel;

namespace Api
{
    public sealed class CourseRepository
    {
        private static readonly Course[] _allCourses =
        {
            new(1, "Calculus", 5),
            new(2, "History", 4),
            new(3, "Literature", 4)
        };

        public Course GetByName(string name)
        {
            return _allCourses.SingleOrDefault(x => x.Name == name);
        }
    }
}
