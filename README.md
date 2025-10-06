# 📚 RESTWebSocket Demo Solution (C# / .NET 8.0)

This solution demonstrates a simple **microservice-style C# system** consisting of:
- A **Management API** – manages users (with roles: `Admin`, `Member`, etc.).
- A **Library API** – manages books and exposes a **WebSocket endpoint** for event notifications.

Both projects are powered by **MySQL** and use **[EfAutoMigration](https://www.nuget.org/packages/EfAutoMigration)** to automatically:
- Create the database (`library_demo`)
- Create tables
- Apply schema changes
- Seed default data on first startup

> 💡 No manual SQL or migration commands are required — everything runs automatically on startup.

---

## 🧱 Project Structure

```
RESTWebsocket-demo/
│
├── Shared/
│   ├── Entities/          # EF Core entities (Users, Books, Roles, etc.)
│   └── Common/            # Helpers, Enums, and Shared Models
│
└── APIs/
    ├── Management/        # User management REST API (Admin)
    └── Library/           # Book management REST + WebSocket server
```

---

## ⚙️ Requirements

### 🔸 On Windows
- .NET 8.0 SDK  
- MySQL installed (or WSL MySQL connection available)
- Visual Studio or VS Code (optional)

### 🔸 On Linux (Ubuntu / WSL)
Make sure the following are installed:

```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0 mysql-server
```

Then confirm versions:
```bash
dotnet --version
mysql --version
```

---

## 🧩 Database Setup

### 1️⃣ Start MySQL
```bash
sudo service mysql start
```

### 2️⃣ Create a demo user (optional if not already)
Login as root:
```bash
sudo mysql
```

Create the user:
```sql
CREATE USER 'demouser'@'localhost' IDENTIFIED BY '123';
GRANT ALL PRIVILEGES ON *.* TO 'demouser'@'localhost' WITH GRANT OPTION;
FLUSH PRIVILEGES;
EXIT;
```

### 3️⃣ Connection String
Both **Management** and **Library** apps use this connection string:

```json
"ConnectionStrings": {
  "Db": "server=localhost;database=library_demo;user=demouser;password=123;"
}
```

> This can be found and modified inside each app’s `appsettings.json`.

---

## 🚀 Running Locally

### Option A — Run using `dotnet run`
From each API folder:

```bash
cd APIs/Management
dotnet run
```

```bash
cd APIs/Library
dotnet run
```

By default:
- Management runs on **http://localhost:5000**
- Library runs on **http://localhost:5100**

The Library also opens a **WebSocket** endpoint:
```
ws://localhost:5100/ws?userId={id}
```

---

### Option B — Publish to Linux (WSL / Server)

From the project root:

```bash
cd APIs/Management
dotnet publish -c Release -r linux-x64 --self-contained true -o out
```

```bash
cd APIs/Library
dotnet publish -c Release -r linux-x64 --self-contained true -o out
```

Then run each published binary:
```bash
cd /APIs/Management/out
dotnet Management.dll

cd /APIs/Library/out
dotnet Library.dll
```

You can also run them in background:
```bash
nohup ./Management > management.log 2>&1 &
nohup ./Library > library.log 2>&1 &
```

---

## 🧠 RESTful API Testing

Using Postman, import the collection under :

```
RESTWebsocket-demo/
│
└── PostmanCollection/
    └── library-demo.postman_collection.json        # Set this collection on your Postman
```

Then you could try running one of the requests there.

## 🧠 WebSocket Testing

Once the Library API is running, connect to the WebSocket endpoint:

### Using browser console:
```js
const ws = new WebSocket("ws://localhost:5100/ws?userId=1"); // see APIs/Library/out/appsettings.json for kestrel port
ws.onopen = () => console.log("✅ Connected");
ws.onmessage = (msg) => console.log("📘 Event:", msg.data);
ws.onclose = () => console.log("❌ Closed");
```

### Or using wscat:
```bash
npm install -g wscat
wscat -c ws://localhost:5100/ws?userId=1
```

Then trigger an API event (e.g. add a new book via POST request):
```bash
curl -X POST http://localhost:5100/api/books -H "Content-Type: application/json" -d '{"title": "Edensor", "author": "Andrea Hirata"}'
```

✅ You’ll see a live push message via WebSocket:
```
📘 Event: {"Event":"Available","Msg":"New entry "Edensor" is available to be borrowed."}
```

---

## 🛠️ Technical Highlights

- **.NET 8.0** Web APIs with EF Core
- **MySQL** persistence
- **Automatic Migration + Seeding** (via [EfAutoMigration](https://www.nuget.org/packages/EfAutoMigration))
- **WebSocket integration** for server push notifications
- **Microservice-style separation** between Management and Library apps
- **Cross-platform ready** (Windows, Linux, macOS, WSL)

---

## 🧩 Ports Used

| Service | HTTP | HTTPS | Description |
|----------|------|--------|--------------|
| Management | 5000 | 5001 | User REST API |
| Library | 5100 | 5101 | Books REST + WebSocket |

You can adjust ports inside each app’s `appsettings.json`.

---

## ✅ Verification Steps

1. Confirm MySQL is running (`sudo service mysql status`)
2. Run `dotnet run` (or published binaries)
3. Access REST endpoints via browser or Postman:
   - `GET http://localhost:5000/api/users`
   - `GET http://localhost:5100/api/books`
4. Connect WebSocket (`ws://localhost:5100/ws?userId=1`)
5. Trigger book changes → see push events live!

---

## 📦 Notes

- Each app manages its own `DbContext` but shares entity definitions from the **Shared** project.
- No token or JWT is required for this demo — user validation is based on `userId` and role check from DB.
- The system is designed to run **independently** but can communicate logically as part of a microservice ecosystem.
- Always run the Management.dll before running Library.dll. Management.dll would generate admins as well as roles

---

## 🧑‍💻 Author

Developed by **Muhammad Guruh Ajinugroho**  
Demo powered by [.NET 8.0 + EF Core + MySQL + WebSocket + EfAutoMigration]
