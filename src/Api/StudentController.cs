﻿using System;
using System.Linq;
using DomainModel;
using Microsoft.AspNetCore.Mvc;

namespace Api
{
    [Route("api/students")]
    public class StudentController : ApplicationController
    {
        private readonly CourseRepository _courseRepository;
        private readonly StateRepository _stateRepository;
        private readonly StudentRepository _studentRepository;

        public StudentController(
            StudentRepository studentRepository, CourseRepository courseRepository, StateRepository stateRepository)
        {
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _stateRepository = stateRepository;
        }

        [HttpPost]
        public IActionResult Register(RegisterRequest request)
        {
            //if (request == null)
            //    return BadRequest("Request cannot be empty");
            // Email should be unique
            // Error codes
            // Return a list of errors, not just the first one

            var addresses = request.Addresses
                .Select(x => Address.Create(x.Street, x.City, x.State, x.ZipCode, _stateRepository.GetAll()).Value)
                .ToArray();

            var email = Email.Create(request.Email).Value;
            var name = request.Name.Trim();

            var student = new Student(email, name, addresses);
            _studentRepository.Save(student);

            var response = new RegisterResponse
            {
                Id = student.Id
            };
            return Ok(response);
        }

        [HttpPut("{id}")]
        public IActionResult EditPersonalInfo(long id, EditPersonalInfoRequest request)
        {
            // Check that the student exists
            var student = _studentRepository.GetById(id);

            //var validator = new EditPersonalInfoRequestValidator();
            //ValidationResult result = validator.Validate(request);

            //if (result.IsValid == false)
            //{
            //    return BadRequest(result.Errors[0].ErrorMessage);
            //}

            //Address[] addresses = request.Addresses
            //    .Select(x => new Address(x.Street, x.City, x.State, x.ZipCode))
            //    .ToArray();
            //student.EditPersonalInfo(request.Name, addresses);
            _studentRepository.Save(student);

            return Ok();
        }

        [HttpPost("{id}/enrollments")]
        public IActionResult Enroll(long id, EnrollRequest request)
        {
            // Check that the student exists
            // Check that the courses exist
            // Grade is correctly parsed
            // Business rules in the Enroll method
            var student = _studentRepository.GetById(id);

            foreach (var enrollmentDto in request.Enrollments)
            {
                var course = _courseRepository.GetByName(enrollmentDto.Course);
                var grade = Enum.Parse<Grade>(enrollmentDto.Grade);

                student.Enroll(course, grade);
            }

            return Ok();
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var student = _studentRepository.GetById(id);

            var resonse = new GetResonse
            {
                Addresses = student.Addresses.Select(x =>
                        new AddressDto
                        {
                            Street = x.Street,
                            City = x.City,
                            State = x.State.Value,
                            ZipCode = x.ZipCode
                        })
                    .ToArray(),
                Email = student.Email.Value,
                Name = student.Name,
                Enrollments = student.Enrollments.Select(x => new CourseEnrollmentDto
                {
                    Course = x.Course.Name,
                    Grade = x.Grade.ToString()
                }).ToArray()
            };
            return Ok(resonse);
        }
    }
}
