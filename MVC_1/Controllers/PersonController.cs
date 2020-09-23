﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVC_1.Models;
using MVC_1.Models.Exceptions;

namespace MVC_1.Controllers
{
    public class PersonController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        // id is not necessary as it is now AUTO_INCREMENT in the database, and is generated thereby.
        public IActionResult Create(string firstName, string lastName, string phone)
        {
            // When this Action gets called, there are 3 likely states:
            // 1) First page load, no data has been provided (initial state).
            // 2) Partial data has been provided (error state).
            // 3) Complete data has been provided (submit state).

            PersonValidationException exception = new PersonValidationException();
            // A request has come in that has some data stored in the query (GET or POST).
            if (Request.Query.Count > 0)
            {
                // Be a little more specific than "== null" because that doesn't account for whitespace.
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    exception.SubExceptions.Add(new Exception("First name was not provided."));
                }
                if (string.IsNullOrWhiteSpace(lastName))
                {
                    exception.SubExceptions.Add(new Exception("Last name was not provided."));
                }
                if (string.IsNullOrWhiteSpace(phone))
                {
                    exception.SubExceptions.Add(new Exception("Phone number was not provided."));
                }
                else
                {
                    // Check for phone number formatting (feel free to use RegEx or any other method).
                    // Has to be in the else branch to avoid null reference exceptions.
                    int temp;
                    string[] phoneParts = phone.Split('-');
                    if (!(
                        phoneParts[0].Length == 3 &&
                        int.TryParse(phoneParts[0], out temp) &&
                        phoneParts[1].Length == 3 &&
                        int.TryParse(phoneParts[1], out temp) &&
                        phoneParts[2].Length == 4 &&
                        int.TryParse(phoneParts[2], out temp)
                        ))
                    {
                        exception.SubExceptions.Add(new Exception("Phone number was not in a valid format."));
                    }
                }
                // If we haven't generated any exceptions.
                if (exception.SubExceptions.Count == 0)
                {
                    // All expected data provided, so this will be our submit state.
                    // Replace the list add with a context add.
                    // Generate the new model instances to be added to the database.
                    Person newPerson = new Person()
                    {
                        FirstName = firstName,
                        LastName = lastName
                    };
                    PhoneNumber newPhoneNumber = new PhoneNumber()
                    {
                        Number = phone,
                        Person = newPerson
                    };
                    // Add the new model instances to the database.
                    using (PersonContext context = new PersonContext())
                    {
                        context.People.Add(newPerson);
                        context.PhoneNumbers.Add(newPhoneNumber);
                        context.SaveChanges();
                    }
                    ViewBag.Success = "Successfully added the person to the list.";
                }
                else
                {
                    // All expected data not provided, so this will be our error state.
                    ViewBag.Exception = exception;
                    // Store our data to re-add to the form.
                    ViewBag.FirstName = firstName;
                    ViewBag.LastName = lastName;
                    ViewBag.Phone = phone;
                }
            }
            // else
            // No request, so this will be our inital state.
            return View();
        }


        public IActionResult List()
        {
            // Just like with Create() all we have to do is translate our logic from List to Context.
            using (PersonContext context = new PersonContext())
            {
                ViewBag.People = context.People.ToList();
            }

            return View();
        }

        public IActionResult Details(string id, string delete)
        {
            IActionResult result;
            if (delete != null)
            {
                DeletePersonByID(int.Parse(id));
                result = RedirectToAction("List");
            }
            else
            {
                ViewBag.Person = GetPersonByID(int.Parse(id));
                result = View();
            }

            return result;
        }

        public Person GetPersonByID(int id)
        {
            Person target;
            // We have to make a separate query for phone numbers, unless we use something called a DTO (Data Transfer Object) to bind them to.
            // Due to time constraints that may or may not be covered, so this is a workaround.
            List<PhoneNumber> phoneNumbers;
            using (PersonContext context = new PersonContext())
            {
                target = context.People.Where(x => x.ID == id).Single();
                phoneNumbers = context.PhoneNumbers.Where(x => x.PersonID == target.ID).ToList();
            }
            // When we initially query to get the "target", EF will only enumerate (retreive) the records from THAT table (by default).
            // This is a workaround to make sure that the phone numbers are assigned to the object that gets returned.
            // There are better and more efficient ways to do this, but this will serve our purposes and is easier to understand at face value.
            target.PhoneNumbers = phoneNumbers;
            return target;
        }

        public void DeletePersonByID(int id)
        {
           
            using (PersonContext context = new PersonContext())
            {
                context.People.Remove(context.People.Where(x => x.ID == id).Single());
                context.SaveChanges();
            }

        }
    }
}