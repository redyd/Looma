# Looma

Looma is a cross-platform application designed for individuals who enjoy knitting, crocheting, and other fiber arts. It
helps users manage their **stock (yarn, threads, etc.)**, **projects**, **patterns**, and **documents** in a structured
and organized way.

---

## 🧶 Features

- **Stock Management**: Track your yarn, threads, and other materials with details like brand, color, weight, and
  length-to-weight ratio.
- **Project Tracking**: Organize your knitting or crocheting projects, including associated patterns and documents.
- **Pattern Management**: Store and search for patterns, with support for notes, URLs, and date tracking.
- **Document Handling**: Upload, manage, and open documents related to your projects or patterns.
- **Search & Filter**: Easily search and filter through stocks, patterns, and projects using powerful query
  capabilities.
- **Cross-Platform**: Built with .NET and Avalonia, ensuring a consistent experience across Windows, macOS, and Linux.

---

## 🧱 Technologies Used

- **.NET MAUI / Avalonia**: For cross-platform UI development.
- **Entity Framework Core**: For data persistence and database interactions.
- **MVVM Pattern**: Clean separation of concerns with `ViewModelBase`, `INavigationService`, and data templates.
- **Dependency Injection**: Configured via `DependencyInjection.cs` and `IServiceCollection`.
- **Avalonia UI Components**: Custom user controls and data templates for rich UI interactions.

---

## 📁 Project Structure

Looma/
├── Looma.App/ # Application entry point and UI
├── Looma.Domain/ # Core business logic and data models
├── Looma.Infrastructure/ # Database mappings, repositories, and migrations
├── Looma.Presentation/ # ViewModels and UI logic
├── Looma.Views/ # XAML UI components and user controls
└── src/ # Source code organization
                                                                                                                                                                                                                                    
---                                                                                                                                                                                                                                 

## 🛠 Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Avalonia UI](https://avaloniaui.net/) (if not already installed)

### Setup

1. Clone the repository:
   ```bash                                                                                                                                                                                                                           
   git clone https://github.com/yourusername/looma
   
2 Navigate to the project directory:

cd looma

3 Restore NuGet packages:

dotnet restore

4 Apply database migrations:

dotnet ef database update

5 Run the application:

dotnet run