using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;

namespace DomainModel
{
    public class Student : Entity
    {
        private readonly List<Enrollment> _enrollments = new();

        protected Student()
        {
        }

        public Student(Email email, string name, Address[] addresses)
            : this()
        {
            Email = email;
            EditPersonalInfo(name, addresses);
        }

        public Email Email { get; }
        public string Name { get; private set; }
        public Address[] Addresses { get; private set; }
        public virtual IReadOnlyList<Enrollment> Enrollments => _enrollments.ToList();

        public void EditPersonalInfo(string name, Address[] addresses)
        {
            Name = name;
            Addresses = addresses;
        }

        public virtual void Enroll(Course course, Grade grade)
        {
            if (_enrollments.Count >= 2)
                throw new Exception("Cannot have more than 2 enrollments");

            if (_enrollments.Any(x => x.Course == course))
                throw new Exception($"Student '{Name}' already enrolled into course '{course.Name}'");

            var enrollment = new Enrollment(this, course, grade);
            _enrollments.Add(enrollment);
        }
    }

    public class Address : Entity
    {
        private Address(string street, string city, State state, string zipCode)
        {
            Street = street;
            City = city;
            State = state;
            ZipCode = zipCode;
        }

        public string Street { get; }
        public string City { get; }
        public State State { get; }
        public string ZipCode { get; }

        public static Result<Address> Create(
            string street, string city, string state, string zipCode, string[] allStates)
        {
            var stateObject = State.Create(state, allStates).Value;

            street = (street ?? "").Trim();
            city = (city ?? "").Trim();
            zipCode = (zipCode ?? "").Trim();

            if (street.Length < 1 || street.Length > 100)
                return Result.Failure<Address>("Invalid street length");

            if (city.Length < 1 || city.Length > 40)
                return Result.Failure<Address>("Invalid city length");

            if (zipCode.Length < 1 || zipCode.Length > 5)
                return Result.Failure<Address>("Invalid zip code length");

            return new Address(street, city, stateObject, zipCode);
        }
    }

    public class State : ValueObject
    {
        private State(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<State> Create(string input, string[] allStates)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result.Failure<State>("Value is required");

            var name = input.Trim().ToUpper();

            if (name.Length > 2)
                return Result.Failure<State>("Value is too long");

            if (allStates.Any(x => x == name) == false)
                return Result.Failure<State>("State is invalid");

            return Result.Success(new State(name));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
