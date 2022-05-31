using System.Collections.Generic;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace DomainModel
{
    public class Email : ValueObject
    {
        private Email(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<Email> Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result.Failure<Email>("Value is required");

            var email = input.Trim();

            if (email.Length > 150)
                return Result.Failure<Email>("Value is too long");

            if (Regex.IsMatch(email, @"^(.+)@(.+)$") == false)
                return Result.Failure<Email>("Value is invalid");

            return Result.Success(new Email(email));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
