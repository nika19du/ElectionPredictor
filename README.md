# ElectionPredictor

# 🗳️ Election Predictor

A full-stack data-driven application for analyzing and predicting election results based on historical data and polling information.

---

## 🚀 Features

- 📥 Import election data from Wikipedia (MediaWiki API)
- 🔎 Custom HTML parsing of election results
- 🗃️ Store structured data in PostgreSQL
- 📊 View election results in an interactive UI (Blazor)
- 🔄 Automatic refresh after data import
- ↕️ Sorting by year, party, vote percentage and seats
- 🧠 Ready for machine learning predictions (ML.NET)

---

## 🏗️ Architecture

### 🔹 Layers:
- **API Layer** – Fetch data from Wikipedia
- **Parser Layer** – Extract structured election results from HTML
- **Service Layer** – Business logic and import pipelines
- **Data Layer** – EF Core + PostgreSQL
- **UI Layer** – Blazor Server

---

## 🛠️ Tech Stack

- ASP.NET Core
- Blazor Server
- Entity Framework Core
- PostgreSQL (pgAdmin)
- ML.NET (planned)
- MediaWiki API

---

<img width="725" height="731" alt="image" src="https://github.com/user-attachments/assets/c59e5f89-5973-496e-a2d9-3acf7662268e" />


## 📊 Data Sources

- Wikipedia election pages (via MediaWiki API)
- Parsed HTML tables for election results
