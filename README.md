#### Sample project to show off FluentValidation with Asp.Net Core

#### Fluent Validation Sample

FluentValidation is a .NET library for building strongly-typed validation rules.

FluentValidation uses [Fluent Interface design pattern](https://github.com/edward-teixeira/design-patterns/tree/master/src/FluentInterfacePattern)
.

#### Installation:

```shell
dotnet add package FluentValidation
```

For ASP.NET Core integration:

```shell
dotnet add package FluentValidation.AspNetCore
```

#### Validating Input with FluentValidation

```cs
using FluentValidation;
// 1- Create a Validator that inherits from AbstractValidator
public class CustomerValidator: AbstractValidator<Customer> {
  public CustomerValidator() {
  //2 - Define the validation rules
    RuleFor(x => x.Surname).NotEmpty();
    RuleFor(x => x.Forename).NotEmpty().WithMessage("Please specify a first name");
    RuleFor(x => x.Discount).NotEqual(0).When(x => x.HasDiscount);
    RuleFor(x => x.Address).Length(20, 250);
    RuleFor(x => x.Postcode).Must(BeAValidPostcode).WithMessage("Please specify a valid postcode");
  }

  private bool BeAValidPostcode(string postcode) {
    // custom postcode validating logic goes here
  }
}

var customer = new Customer();
var validator = new CustomerValidator();

// 3 - Execute the validator
ValidationResult results = validator.Validate(customer);

// 4 - Inspect any validation failures.
bool success = results.IsValid;
List<ValidationFailure> failures = results.Errors;
```

#### Validating Simple Properties

- Should never use FluentValidation to validate domain classes;
- Validates request data, not the domain class;

Example:

```cs
public class StudentValidator : AbstractValidator<StudentDto>  
{  
    public StudentValidator()  
    {  
        RuleSet("Register", () =>  
        {  
            RuleFor(x => x.Email).NotEmpty().Length(0, 150).EmailAddress();  
        });  
        RuleSet("EditPersonalInfo", () =>  
        {  
            // No separate rules for EditPersonalInfo API yet  
        });  
        RuleFor(x => x.Name).NotEmpty().Length(0, 200);  
        RuleFor(x => x.Addresses).NotNull().SetValidator(new AddressesValidator());  
    }  
}
```

#### Validating Complex Properties

- Avoid using inline nested rules in favor of separate validator for code reuse and conciseness:

```cs
// Inline Validation
// Bad
```cs
RuleFor(x => x.Address).NotNull();

RuleFor(x => x.Address.Street)
.NotEmpty()
.Length(0, 100)
.When(x => x.Address != null);

RuleFor(x => x.Address.City)
.NotEmpty()
.Length(0, 40)
.When(x => x.Address != null);

RuleFor(x => x.Address.State)
.NotEmpty()
.Length(0, 2)
.When(x => x.Address != null);

RuleFor(x => x.Address.ZipCode)
.NotEmpty()
.Length(0, 5)
.When(x => x.Address != null);

// Separate validator
//Good
RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
```

- Always keep the domain model separate from data contracts;

- Validate each field in the structure separately;

- Fluent validation doesn't automatically check for non-nullability of the object container when you set up rules for
  its properties. Use When() to check explicitly:

```cs
// Instead of using a child validator, you can define child rules inline, eg:
RuleFor(customer => customer.Address.Postcode).NotNull()

// Use When() condition
RuleFor(x => x.Address.Street)
.NotEmpty()
.Length(0, 100)
.When(x => x.Address != null);
```

- Avoid code duplication by creating a separate validator:

```cs
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>  
{  
    public RegisterRequestValidator()  
    {  
        RuleFor(x => x.Email).NotEmpty().Length(0, 150).EmailAddress();  
        RuleFor(x => x.Name).NotEmpty().Length(0, 200);
        // If Addresses is not null, it's own validator will be called
        RuleFor(x => x.Addresses)
        .NotNull()
        .SetValidator(new AddressesValidator());  
    }  
}

// We can reuse this anywhere
public class AddressesValidator : AbstractValidator<AddressDto[]>  
{  
    public AddressesValidator()  
    {  
		RuleFor(x => x.Street).NotEmpty().Length(0, 100);  
		RuleFor(x => x.City).NotEmpty().Length(0, 40);  
		RuleFor(x => x.State).NotEmpty().Length(0, 2);  
		RuleFor(x => x.ZipCode).NotEmpty().Length(0, 5);
    }  
}
```

#### Validating Collections

- There are two ways of setting up validation rules for collection items:

```cs
// Inline Rules:
RuleForEach(x => x.Addresses).ChildRules(address => {
	address.RuleFor(x => x.Street).NotEmpty().Length(0, 100);  
	address.RuleFor(x => x.City).NotEmpty().Length(0, 40);  
	address.RuleFor(x => x.State).NotEmpty().Length(0, 2);  
	address.RuleFor(x => x.ZipCode).NotEmpty().Length(0, 5);
});

// Separate Validator
RuleFor(x => x.Addresses).NotNull().SetValidator(new AddressesValidator());
```

- Validating the collection and items:

```cs
public class AddressesValidator : AbstractValidator<AddressDto[]>  
{  
    public AddressesValidator()  
    {  
	    // Validate the collection
        RuleFor(x => x)  
            .Must(x => x?.Length >= 1 && x.Length <= 3)  
            .WithMessage("The number of addresses must be between 1 and 3")
            //Validate collections items  
            .ForEach(x =>  
            {  
                x.NotNull();
                x.SetValidator(new AddressValidator());  
            });  
    }  
}
```

- Use  `RuleForEach` method to apply the same rule to multiple items in a collection:

```cs
public class PersonValidator : AbstractValidator<Person> 
{
  public PersonValidator() 
  {
  // NotNull check against each item in the `AddressLines` collection.
    RuleForEach(x => x.AddressLines).NotNull();
  }
}
```

- Access the index of the collection element that caused the validation failure with `{CollectionIndex}` placeholder -
  version 8.5+:

```cs
public class PersonValidator : AbstractValidator<Person> 
{
  public PersonValidator() 
  {
    RuleForEach(x => x.AddressLines)
    .NotNull()
    .WithMessage("Address {CollectionIndex} is required.");
  }
}
```

- Combine `RuleForEach` with `SetValidator` when the collection is of another complex objects:

```cs
public class Customer 
{
  public List<Order> Orders { get; set; } = new List<Order>();
}

public class Order 
{
  public double Total { get; set; }
}

public class OrderValidator : AbstractValidator<Order> 
{
  public OrderValidator() 
  {
    RuleFor(x => x.Total).GreaterThan(0);
  }
}

public class CustomerValidator : AbstractValidator<Customer> 
{
  public CustomerValidator() 
  {
    RuleForEach(x => x.Orders).SetValidator(new OrderValidator());
  }
}
```

- Include or exclude certain items in the collection from being validated:

```cs
// This must come directly after the call to RuleForEach():
RuleForEach(x => x.Orders)
  .Where(x => x.Cost != null)
  .SetValidator(new OrderValidator());
```

- Combine rules that act upon the entire collection with rules which act upon individual elements within the collection
  - 8.2+:

```cs
// This rule acts on the whole collection (using RuleFor)
RuleFor(x => x.Orders)
  .Must(x => x.Count <= 10).WithMessage("No more than 10 orders are allowed");
// This rule acts on each individual element (using RuleForEach)
RuleForEach(x => x.Orders)
  .Must(order => order.Total > 0).WithMessage("Orders must have a total of more than 0")


// The above 2 rules could be re-written as:
RuleFor(x => x.Orders)
  .Must(x => x.Count <= 10).WithMessage("No more than 10 orders are allowed")
  .ForEach(orderRule => 
  {
    orderRule.Must(order => order.Total > 0).WithMessage("Orders must have a total of more than 0")
  });
```

#### Inheritance Validation

- Allows you to set up validation rules polymorphically - only applicable to domain classes;
- If your object contains a property which is a base class or interface, you can set up
  specific [child validators](https://docs.fluentvalidation.net/en/latest/start.html#complex-properties) for individual
  subclasses/implementors:

```cs
// We have an interface that represents a 'contact',
// for example in a CRM system. All contacts must have a name and email.
public interface IContact 
{
  string Name { get; set; }
  string Email { get; set; }
}

// A Person is a type of contact, with a name and a DOB.
public class Person : IContact 
{
  public string Name { get; set; }
  public string Email { get; set; }

  public DateTime DateOfBirth { get; set; }
}

// An organisation is another type of contact,
// with a name and the address of their HQ.
public class Organisation : IContact {
  public string Name { get; set; }
  public string Email { get; set; }

  public Address Headquarters { get; set; }
}

// Our model class that we'll be validating.
// This might be a request to send a message to a contact.
public class ContactRequest 
{
  public IContact Contact { get; set; }

  public string MessageToSend { get; set; }
}

// Next we create validators for Person and Organisatio
public class PersonValidator : AbstractValidator<Person> 
{
  public PersonValidator() 
  {
    RuleFor(x => x.Name).NotNull();
    RuleFor(x => x.Email).NotNull();
    RuleFor(x => x.DateOfBirth).GreaterThan(DateTime.MinValue);
  }
}

public class OrganisationValidator : AbstractValidator<Organisation> 
{
  public OrganisationValidator() 
  {
    RuleFor(x => x.Name).NotNull();
    RuleFor(x => x.Email).NotNull();
    RuleFor(x => x.HeadQuarters).SetValidator(new AddressValidator());
  }
}

/* Now we create a validator for our `ContactRequest`. We can define specific validators for the `Contact` property, depending on its runtime type. This is done by calling `SetInheritanceValidator`, passing in a function that can be used to define specific child validators: */
public class ContactRequestValidator : AbstractValidator<ContactRequest>
{
  public ContactRequestValidator()
  {

    RuleFor(x => x.Contact).SetInheritanceValidator(v => 
    {
      v.Add<Organisation>(new OrganisationValidator());
      v.Add<Person>(new PersonValidator());
    });

}
```

#### RuleSets

- RuleSets allow you to group validation rules together which can be executed together as a group whilst ignoring other
  rules:

```cs
/*
For example, let’s imagine we have 3 properties on a Person object (Id, Surname and Forename) and have a validation rule for each. We could group the Surname and Forename rules together in a “Names” RuleSet:
*/
 public class PersonValidator : AbstractValidator<Person> 
 {
  public PersonValidator() 
  {
     RuleSet("Names", () => 
     {
        RuleFor(x => x.Surname).NotNull();
        RuleFor(x => x.Forename).NotNull();
     });

     RuleFor(x => x.Id).NotEqual(0);
  }
}
```

#### Throwing Exceptions

- FluentValidation can throw exceptions instead of ValidationResult:

```cs
Customer customer = new Customer();
CustomerValidator validator = new CustomerValidator();
// This throws a `ValidationException` which contains the error messages in the Errors property.
validator.ValidateAndThrow(customer);

// Alternatively
validator.Validate(customer, options => options.ThrowOnFailures());
```

- Don't use exceptions for validation;

#### Conditional Validation

- Run validation only with some predicate:

```cs
RuleFor(x => x.Addresses).NotNull().SetValidator(new AddressesValidator());  
When(x => x.Email == null, () =>  
{  
    RuleFor(x => x.Phone).NotEmpty();  
});  
When(x => x.Phone == null, () =>  
{  
    RuleFor(x => x.Email).NotEmpty();  
});

// or alternatively
RuleFor(x => x.Phone)  
    .NotEmpty()  
    .Matches("^[2-9][0-9]{9}$")  
    .When(x => x.Phone != null);
```

- Validating multiple properties:

```cs
/*
* Conditions that group multiple rule chains*
*/
// If the email is null, the phone should not be empty
When(x => x.Email == null, () =>  
{  
    RuleFor(x => x.Phone).NotEmpty();  
});
// if the phone is null, email should not be empty
When(x => x.Phone == null, () =>  
{  
    RuleFor(x => x.Email).NotEmpty();  
});
```

- Conditions within the rule chain:

```cs
// The condition in the When() applies to all preceding checks
RuleFor(x => x.Email)  
    .NotEmpty()  
    .Length(0, 150)  
    .EmailAddress()  
    .When(x => x.Email != null);
    
// Applies only the immediate previous check
RuleFor(x => x.Email)  
    .NotEmpty()  
    .Length(0, 150)  
    .EmailAddress()  
    .When(x => x.Email != null, ApplyConditionTo.CurrentValidator);
```

#### Cascade Mode

- Cascade mode controls validation flow;
- Configure how rules should cascade when one fails:

```cs
// Will stop in the first fail
RuleFor(x => x.Email)  
    .Cascade(CascadeMode.Stop)  
    .NotEmpty()  
    .Length(0, 150)  
    .EmailAddress()  
    .When(x => x.Email != null);
    
// Will continue the validation chain - default;
RuleFor(x => x.Email)  
    .Cascade(CascadeMode.Continue)  
    .NotEmpty()  
    .Length(0, 150)  
    .EmailAddress()  
    .When(x => x.Email != null);

// Setting it Globally
ValidatorOptions.Global.CascadeMode = CascadeMode.Stop;
```

#### Asp.Net Core Integration

1. Install ASP.NET Core package:

```shell
dotnet add package FluentValidation.AspNetCore
```

2. Register FluentValidation service:

```cs
// Program.cs or Startup.cs
services
	.AddControllers()
	.AddFluentValidation();
// Now all errors generated by the library will show up in the ModelState.
```

3. Register the validators:

```cs
// Program.cs or Startup.cs
// Register RegisterRequestValidator.cs
services
	.AddControllers(options => 
	RegisterValidatorsFromAssemblyContaining<RegisterRequestValidator>)
	.AddFluentValidation();
```

4. Call the ModelState for validation:

```cs
// In the controller
if (!ModelState.IsValid)  
{  
    string[] errors = ModelState  
        .Where(x => x.Value.Errors.Any())  
        .Select(x => x.Value.Errors.First().ErrorMessage)  
        .ToArray();  
    return BadRequest(string.Join(", ", errors));  
}

// or you can skip this manual validation with controller ApiController annotation and Controllerbase:
[Route("api/students")]
[ApiController]
public class StudentController : ControllerBase 
{ }
// The model state is checked automatically
// No need for the FromBody attribute
```

5. Done!

**Obs: You can only have one validator per data contract**

#### Custom Validation Rules

- Reusing a single rule in a chain;

```cs
public static class CustomValidators  
{  
    public static IRuleBuilderOptionsConditions<T, IList<TElement>> ListMustContainNumberOfItems<T, TElement>(
    this IRuleBuilder<T,IList<TElement>> ruleBuilder,
    int? min = null,
    int? max = null)  
    {  
        return ruleBuilder.Custom((list, context) =>  
        {  
            if (min.HasValue && list.Count < min.Value)  
            {  
                context.AddFailure($"The list must contain {min.Value} items or more. It contains {list.Count} items.");  
            }  
  
            if (max.HasValue && list.Count > max.Value)  
            {  
                context.AddFailure($"The list must contain {max.Value} items or fewer. It contains {list.Count} items.");  
            }  
        });  
    }  
}

// CustomValidators usage
public class AddressesValidator : AbstractValidator<AddressDto[]>  
{  
    public AddressesValidator()  
    {  
        RuleFor(x => x)  
            .ListMustContainNumberOfItems(1, 3)  
            .ForEach(x =>  
            {  
                x.NotNull();  
                x.SetValidator(new AddressValidator());  
            });  
    }  
}
```

#### Transforming Values

- You can apply a transformation to a property value prior to validation being performed against it.For example, if you
  have property of type `string` that actually contains numeric input, you could apply a transformation to convert the
  string value to a number:

```cs
/* This rule transforms the value from a `string` to a nullable `int` (returning `null` if the value couldn’t be converted). A greater-than check is then performed on the resulting value.
*/
Transform(from: x => x.SomeStringProperty, to: value => int.TryParse(value, out int val) ? (int?) val : null)
    .GreaterThan(10);
/*
Syntactically this is not particularly nice to read, so the logic for the transformation can optionally be moved into a separate method:
*/
Transform(x => x.SomeStringProperty, StringToNullableInt)
    .GreaterThan(10);

int? StringToNullableInt(string value)
  => int.TryParse(value, out int val) ? (int?) val : null;

```

- There is also a `TransformForEach` method available, which performs the transformation against each item in a
  collection;

### Validating Input the DDD way

##### What is validation?

- Validation is the process of mapping a set onto its subset;
- Mapping always goes from the larger set to the smaller one;
- Mapping involves a decision;
- Mapping is filtration;

##### Always-valid Domain Model

- Always-valid domain model is a guideline advocating for domain classes to always remain in a valid state.
- Not-always-valid domain model allows to categorize validations. Example:

```cs
// Must put the domain class into an invalid state
public class Student : Entity  
{  
    public Email Email { get; }  
    public StudentName Name { get; private set; }  
    public Address[] Addresses { get; private set; }  
  
	public ValidationResult Validate() 
	{
		// Validation goes here
	}
}
```

- Why potentially invalid domain classes is a problem?
	- You never know if domain classes are validated or not.

###### Always-valid or Not-always-valid domain model?

- Not-always-valid domain model:
	- Must be extra diligent not to miss required checks;
	- Vastly increases maintenance costs;

- Always-valid domain model:
	- Impossible to miss required checks;
	- Significantly reduces maintenance costs;

- Validate request data, not the domain classes;
- Domain classes != Data Contracts;

###### Not-always-valid Domain Model and Primitive Obsession

Primitive obsession is when you use primitive types to model your domain. Example:

```cs
public class Customer {
	// Email != string
	public string Email {get; set; }
	// Discount != decimal
	public decimal CurrentDiscount {get; set; }
}
```

- Primitive types are a very crude wat to model your domain.
- Requires extra prudency.
- To fix the issue with primitive obession we need to introduce wrappers on top of the primitive types that would more
  accurately represent the underlying domain concepts. These wrappers are called ValueObjects. For example, for the
  field Email of the Customer class, we could create a type called Email ou EmailAddress:

```cs
// Email and Discount are ValueObjects
public class Customer  {
	public Email Email { get; set; }
	public Discount CurrentDiscount {get; set; }
}  
```

###### Value Objects

- Value object is a concept with no inherent identity;
- Instances of such a class are interchangeable, as long as theit contents are the same;
- Value objects are immutable, they cannot be persisted on their own;
- Always attached to an entity;

##### Validation vs Invariants

- Invariant is a condition that your domain model must uphold at all times;
- Invariatns are the same as input validation;
- Invariants define the domain model;
- Invariants are the reason validation exists;
- Invariants are what differentiates valid and invalid domain models;
- Validation rules = Invariantes;
- All validation rules belong to the domain layer;
- No differente between simple and complex validations;
- Data validation is the same as business rules validations;
- Domain model is a walled garden;
- 1 invariant = primitive type and 1 invariants = value object;

##### Combining FluentValidation with Value Objects

- Keep validation rules in value objects amd use value objects from FluentValidation;
- Validation is done in the fluent validator;
- Good use of exceptions;
- Exceptions is a fail-safe;
- Not catiching suck exceptions;
- Fail fast principle;

##### Validation is Parsing

- Can't separate Creation from Validation. The two can't be separated;
- Separation leads to code duplication;
- Parsing = Validation + Transformation;
- Parsers preserve information about transformations;
- All operations that involve transformation and validation should be treated as parsers. Such operations should be
  implemented as one method;

##### Validating Complex Data

- The use of primitive types should be a conscious choice;
- Having all validations in the domain layer isn't practical;
- Software development is all about strategically chosen concessions and trade-offs;
- Primitive types make it impossible to implement validation as parsing. Muster either forgo transformation or duplicate
  it;

#### Diving deeper into the concept of validation

##### Defining Explicit Errors

- Strings are not reliable errros;
- Error messages should not be handled by the domain layer;
- Define each error explicitly;
- All error codes must be unique
- Check the uniqueness with a unit test;

Example:

```cs
public sealed class Error : ValueObject {
	
	// Parte of thje contract with clients
	public string Code { get; }
	
	// For debugging Purposes only
	public string Message { get; }
	
	// Enabels mapping to different HTTP response codes
	public string HttpCode { get; } 
	
	internal Error(string code, string message) {
		Code = code;
		Message = message;
	}
	
	// Only Code participate in equality comparison
	protected override IEnumerable<object> GetEqualityComponents() {
		yield return Code
	}
}
```

---
Links:

[Fluent Validation Fundamentals](https://app.pluralsight.com/library/courses/fluentvalidation-fundamentals/table-of-contents)

[Fluent Validation](https://fluentvalidation.net/)

[Fluent Validation SourceCode](https://github.com/FluentValidation/FluentValidation)

[Fluent Pattern](https://martinfowler.com/bliki/FluentInterface.html)

[Fluent Validation Source Code](https://github.com/FluentValidation/FluentValidation)

[Always-Valid Domain Model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/)

[Always valid vs not always valid domain model](https://enterprisecraftsmanship.com/posts/always-valid-vs-not-always-valid-domain-model/)

[ValueObjects ](https://martinfowler.com/bliki/ValueObject.html)

[Advanced error handling techniques](https://enterprisecraftsmanship.com/posts/advanced-error-handling-techniques/)

---
