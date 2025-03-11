# 🚀 TCP Message Relay Gateway

## **Overview**
This project implements a **TCP Message Relay Gateway** that facilitates **bidirectional communication** between a **vehicle** and a **backend server**. The gateway ensures messages from the vehicle are **relayed to the backend** and vice versa while handling **connection management, retries, and error handling**.

## **📌 Features**
✔ **Event-Driven Architecture** → Uses event handlers for **decoupled and scalable processing**.  
✔ **Automatic Connection Handling** → Ensures the **backend connects only if the vehicle is online**.  
✔ **Retry & Error Handling** → Implements **automatic reconnection** and **graceful error handling**.  
✔ **Bidirectional Message Forwarding** → Messages are relayed **both ways** between vehicle & backend.  
✔ **Dependency Injection (DI) Support** → Uses **DI for logging, event handlers, and client management**.  
✔ **Asynchronous Communication** → Ensures **non-blocking** TCP message processing.  

## **🛠️ Technologies Used**
- **.NET 9**
- **C# (Sockets API)**
- **Microsoft.Extensions.Logging**
- **Dependency Injection**

---

## **🏗️ Project Structure**
```
/TcpGatewaySolution
   /src
      /TcpLibrary                    → (Reusable TCP Client Library)
         /Clients
            - ITcpClientService.cs    → (TCP Client Interface)
            - TcpClientService.cs     → (Socket-based TCP Client)
         /Config
            - TcpConfig.cs            → (Configuration Settings)
         /Events
            - IEventHandler.cs        → (Generic Event Handler Interface)
         /Handlers
            - LoggingMessageHandler.cs → (Logs incoming messages)
            - ForwardingMessageHandler.cs → (Forwards messages to backend)
      /TcpMessageRelay
         - TcpMessageRelay.cs          → (Manages relay between vehicle & backend)
   /tests                             → (Unit & Integration Tests) (Coming soon...)
   TcpGatewaySolution.sln             → (Solution file)
```

---

## **⚙️ Configuration**
All configuration settings are managed in `TcpConfig.cs`.

```csharp
public class TcpConfig
{
    public string ServerIp { get; set; }
    public int Port { get; set; }
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayMs { get; set; } = 3000;
}
```

### **🔹 Default Configuration**
| **Setting** | **Description** | **Default Value** |
|------------|----------------|----------------|
| `ServerIp` | IP Address of the TCP server | `127.0.0.1` |
| `Port` | TCP Port Number | `5000` |
| `MaxRetries` | Maximum reconnection attempts | `5` |
| `RetryDelayMs` | Delay before retry (ms) | `3000` |

---

## **🚀 Running the Application**
### **🔹 Step 1: Install Dependencies**
Run the following command to install required dependencies:
```sh
dotnet restore
```

### **🔹 Step 2: Build the Solution**
```sh
dotnet build
```

### **🔹 Step 3: Run the TCP Message Relay**
```sh
dotnet run --project src/TcpMessageRelay/TcpMessageRelay.csproj
```

---

## **📌 How It Works**
1️⃣ The **gateway waits for a vehicle to connect**.
2️⃣ Once connected, **it attempts to connect to the backend**.
3️⃣ **Messages are automatically forwarded** between the vehicle and the backend.
4️⃣ If the **vehicle disconnects**, the backend connection is **automatically closed**.
5️⃣ The system **logs all messages and connection status**.

---

## **✅ Unit & Integration Tests**
Unit tests are provided under `/tests/TcpLibrary.Tests`.
Run tests using:
```sh
dotnet test
```

---

## **📌 Next Steps & Improvements**
🔹 Implement **automatic reconnection** if the vehicle comes back online.  
🔹 Add **TLS/SSL encryption** for secure communication.  

---
