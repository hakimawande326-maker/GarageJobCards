GarageJobCards — Phila's Auto Spare Management System

A Garage Job Management Web Application built for Phila's Auto Spare, an auto repair shop in Umlazi, as part of the Applications Development (APDA201) module.

The Problem

Phila's Auto Spare currently tracks vehicle repairs using paper job cards and verbal communication between mechanics and the front-desk receptionist. This causes lost/misplaced job cards, delayed repairs, and customers who have no way to check on their vehicle's status without repeatedly calling the shop.

The Solution

This system replaces the paper-based process with a digital one:

Receptionists register customers and vehicles, generating a digital job card for each repair.
Managers assign job cards to specific mechanics.
Mechanics update repair status in real time: Received → Diagnosing → Repairing → Ready for Collection.
Customers can log in to a self-service portal to check their vehicle's live repair status, without needing to call in.
Tech Stack
ASP.NET MVC 5 (.NET Framework, classic System.Web.Mvc — not .NET Core)
C# for all application/business logic
SQL Server (developed and managed via SQL Server Management Studio)
Entity Framework (Database First)
Razor views (.cshtml), HTML/CSS/JavaScript for the front end
Visual Studio 2022
User Roles
Role	Access
Manager	Assigns job cards to mechanics, oversees all active jobs
Receptionist	Registers customers/vehicles, creates new job cards
Mechanic	Views assigned jobs, updates repair status
Customer	Tracks their own vehicle's repair status

Roles are stored and checked against the database (Users/Roles tables) rather than hardcoded, so role assignments can change without a code deployment.

Getting Started
Clone the repository.
Open GarageJobCards.sln in Visual Studio 2022.
Restore NuGet packages — right-click the solution in Solution Explorer → Restore NuGet Packages. This step is required after every fresh clone, since package files aren't committed to the repo.
Update the connection string in Web.config to point to your local SQL Server instance.
Run the database setup scripts (see /Database if present) in SQL Server Management Studio to create the schema.
Press F5 to build and run.
Team

Built by AppStatic 10 for Phila's Auto Spare, Umlazi.

Status

Work in progress — student coursework project for APDA201.
