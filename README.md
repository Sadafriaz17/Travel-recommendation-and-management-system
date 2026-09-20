# NEXVOY

**AI-Powered Travel Planning & Booking Platform**

NEXVOY is a full-stack travel web application that combines AI-assisted trip planning with a vendor marketplace, VR destination previews, and an end-to-end booking system. Built on ASP.NET MVC, it delivers both a rich traveler-facing experience and a comprehensive administrative backend.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Admin Portal](#admin-portal)

---

## Overview

NEXVOY reimagines the travel planning experience by pairing AI-driven recommendations with an interactive, visually rich interface. Rather than static listings, users receive personalized itineraries, immersive destination previews, and access to a marketplace where vendors can offer travel-related products and services.

## Features

### For Travelers
- AI-based trip planning tailored to user preferences
- VR destination previews for an immersive pre-booking experience
- Vendor marketplace for travel products and services
- End-to-end booking and travel e-commerce flow
- Interactive package browsing with detailed itineraries

### For Administrators
A comprehensive admin dashboard with full CRUD management across:

| Module | Capabilities |
|---|---|
| Blogs | Create, edit, and publish travel content |
| Bookings | View, manage, and update customer bookings |
| Destinations | Add and edit destination listings |
| Gallery | Manage image galleries with self-healing fallback handling |
| Hotels | Manage hotel listings and hotel images |
| Packages | Manage packages, package types, and itineraries |
| Page Content | Edit site-wide content blocks |
| Payments | Manage payments and payment types |
| Products | Manage marketplace products and product types |
| Users | Manage users and user roles |

## Tech Stack

- **Backend:** ASP.NET MVC (C#)
- **Views:** Razor (CSHTML)
- **Frontend Effects:** Three.js for the interactive 3D globe
- **Database:** SQL Server
- **Architecture:** Area-based MVC structure separating the public site from the admin portal

## Project Structure

```
TourFYP/
├── Areas/
│   └── AdminArea/
│       ├── Controllers/     # Admin CRUD controllers
│       └── Views/           # Admin dashboard views
├── Controllers/              # Public-facing site controllers
├── Models/                   # Data models
├── Views/                    # Public-facing site views
├── Scripts/                  # JS assets, including Three.js globe
└── Content/                  # CSS and static assets
```

## Getting Started

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework / .NET Core (matching your project configuration)
- SQL Server

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/your-username/your-repo-name.git
   ```
2. Open the solution in Visual Studio.
3. Restore NuGet packages.
4. Update the connection string in `Web.config` (or `appsettings.json`) to point to your database.
5. Run database migrations or execute the provided SQL scripts.
6. Build and run the project.

## Admin Portal

The admin portal is fully separated under `Areas/AdminArea`, providing administrators a dedicated dashboard to manage every part of the platform — from travel packages to payments — independent of the public-facing site code.
