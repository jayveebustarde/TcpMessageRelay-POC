# 🚀 TCP Message Relay Gateway

## **📌 Overview**
The **TCP Message Relay Gateway** facilitates **bidirectional communication** between a **vehicle client** and a **backend server**. It acts as a relay, ensuring messages are forwarded **both ways** while managing **connections, retries, and error handling**.

This solution consists of **two main projects**:
1. **`TcpLibrary`** → A **reusable, generic TCP client library** for managing a single TCP connection.
2. **`TcpMessageRelay`** → Implements **the relay logic** that bridges communication between a **vehicle and backend**.

---

## **✅ Features**
✔ **Reusable TCP Client Library** → Provides **connection management, event-based messaging, and retries**.  
✔ **Bidirectional Message Relay** → Ensures **messages are forwarded between the vehicle & backend**.  
✔ **Automatic Connection Handling** → **Backend connects only when the vehicle is online**.  
✔ **Robust Error Handling** → Manages **disconnects, retries, and custom exceptions**.  
✔ **Dependency Injection Support** → Uses **DI for logging, event handlers, and client services**.  
✔ **Asynchronous & Non-Blocking** → Fully **async TCP messaging** for scalability.  

---

## **🛠️ Technologies Used**
- **.NET 9**
- **C# (Sockets API)**
- **Microsoft.Extensions.Logging**
- **Moq (Unit Testing)**
- **xUnit (Testing Framework)**
- **Dependency Injection (DI)**

---

## **🏗️ Solution Architecture**
```
/TcpGatewaySolution
   /src
      /TcpLibrary                    → (Reusable TCP Client Library)
         /Clients
            - IClientService.cs       → (Generic TCP Client Interface)
            - TcpClientService.cs     → (Socket-based TCP Client)
         /Config
            - TcpConfig.cs            → (Configuration Settings)
         /Events
            - IEventHandler.cs        → (Generic Event Handler Interface)
         /Exceptions
            - TcpConnectionException.cs  → (Handles connection errors)
            - TcpSendException.cs        → (Handles message sending errors)
      /TcpMessageRelay                → (Handles communication relay)
         /Clients
            - IVehicleClient.cs        → (Abstraction for the vehicle connection)
            - IBackendClient.cs        → (Abstraction for the backend connection)
         /Relay
            - ITcpRelayService.cs      → (Interface for relay management)
            - TcpRelayService.cs       → (Manages TCP message forwarding)
         /Handlers
            - ForwardingMessageHandler.cs → (Handles message forwarding)
   /tests                              → (Unit & Integration Tests)
      - TcpLibrary.Tests                → (Tests for reusable TCP client)
      - TcpMessageRelay.Tests           → (Tests for message relay logic)
   TcpGatewaySolution.sln               → (Solution file)
```

---

## **⚙️ Configuration**
All **TCP connection settings** are managed in `TcpConfig.cs`:

```csharp
public class TcpConfig
{
    public string ServerIp { get; init; }
    public int Port { get; init; }
    public int MaxRetries { get; init; } = 5;
    public int RetryDelayMs { get; init; } = 3000;
}
```

### **🔹 Default Configuration**
| **Setting**    | **Description**                           | **Default Value** |
|---------------|------------------------------------------|------------------|
| `ServerIp`    | IP Address of the TCP server            | `127.0.0.1`      |
| `Port`        | TCP Port Number                         | `5000`           |
| `MaxRetries`  | Maximum reconnection attempts          | `5`              |
| `RetryDelayMs` | Delay before retry (ms)               | `3000`           |

---

## **🚀 Running the Application**
### **🔹 Step 1: Install Dependencies**
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
1️⃣ The **gateway waits for a vehicle client to connect**.  
2️⃣ Once connected, it **attempts to connect to the backend**.  
3️⃣ **Messages from the vehicle are forwarded to the backend**, and vice versa.  
4️⃣ If the **vehicle disconnects**, the backend connection **automatically closes**.  
5️⃣ **Custom exception handling** ensures the system recovers from failures.  

---

## **✅ Unit & Integration Tests**
Unit tests are structured under `/tests`:

### **🔹 Running Tests**
```sh
dotnet test
```

### **🔹 Test Coverage**
✔ **Connection Handling Tests** → Ensures **clients connect/disconnect correctly**.  
✔ **Message Forwarding Tests** → Verifies **messages are properly relayed**.  
✔ **Exception Handling Tests** → Ensures **robust error management**.  

---