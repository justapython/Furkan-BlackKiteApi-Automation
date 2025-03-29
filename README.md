# BlackKite API Automation Project

## 🔍 Table of Contents
- [About the Project](#about-the-project)
- [Technologies Used](#technologies-used)
- [Installation](#installation)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Features](#features)
- [References](#references)

---

## ℹ️ About the Project
This project provides end-to-end API test automation for the Black Kite platform. It uses SpecFlow for BDD-style testing, and RestSharp for HTTP communication. The goal is to test key business flows such as ecosystem and company creation, finding retrieval and update, logging, and deletion within the platform.

---

## 🛠️ Technologies Used
- [.NET 8.0](https://dotnet.microsoft.com/en-us/)
- [C#](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [RestSharp](https://restsharp.dev/)
- [SpecFlow (Gherkin Syntax)](https://specflow.org/)
- [NUnit](https://nunit.org/)
- [SpecFlow.Tools.MsBuild.Generation](https://www.nuget.org/packages/SpecFlow.Tools.MsBuild.Generation/)

---

## ⚙️ Installation
1. Clone the repository:

```bash
git clone https://github.com/yourusername/BlackKiteApiAutomation.git
cd BlackKiteApiAutomation
```

2. Restore the dependencies:

```bash
dotnet restore
```

> Ensure you have .NET SDK 8.0+ and Git installed.

---

## ▶️ Usage
To execute the tests via terminal:

```bash
dotnet clean

dotnet build

dotnet test
```

This will run all defined SpecFlow scenarios and output the test results to the terminal.

---

## 📁 Project Structure
```
BlackKiteApiTestsFurkan/
├── BlackKiteApiLib/
│   └── Clients/
│       └── BlackKiteClient.cs
├── BlackKiteApiTests/
│   ├── StepDefinitions/
│   │   └── BlackKiteSteps.cs
│   └── Features/
│       └── BlackKite.feature
```

---

## ✨ Features
- ✅ Authentication using client credentials
- ✅ Ecosystem creation & verification
- ✅ Company creation, scan status polling
- ✅ Notification listing and finding analysis
- ✅ Dynamic module-based finding detail requests
- ✅ Patch finding status with validation
- ✅ Verify status change logs
- ✅ Delete company from specific ecosystem
- ✅ Delete ecosystem and confirm deletion

---

## 📊 Dependencies (NuGet)
```xml
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="NUnit" Version="3.14.0" />
<PackageReference Include="NUnit.Analyzers" Version="3.9.0" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
<PackageReference Include="RestSharp" Version="112.1.0" />
<PackageReference Include="SpecFlow" Version="3.9.74" />
<PackageReference Include="SpecFlow.NUnit" Version="3.9.74" />
<PackageReference Include="SpecFlow.Tools.MsBuild.Generation" Version="3.9.74" />
```

These are included in the `.csproj` file under the test project folder.

---

## 📖 References
- [Black Kite Official API Documentation](https://google.com)
- [SpecFlow Documentation](https://docs.specflow.org/projects/specflow/en/latest/)
- [RestSharp Guide](https://restsharp.dev/)
- [NUnit Docs](https://docs.nunit.org/)

---


